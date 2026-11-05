using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace eShopWcf.Parity;

internal sealed class ParityRun(SoapClient legacy, ModernClient modern, string directory)
{
    private readonly List<Step> _steps = [];
    private RunState _state = new();
    private string? _error;

    internal async Task<int> ExecuteAsync(string phase)
    {
        Directory.CreateDirectory(directory);
        try
        {
            if (phase == "reads-after-restart")
            {
                _state = JsonSerializer.Deserialize<RunState>(await File.ReadAllTextAsync(Path.Combine(directory, "state.json")).ConfigureAwait(false))
                    ?? throw new InvalidDataException("Missing full-phase state.");
            }

            await ReadsAsync().ConfigureAwait(false);
            if (phase == "full") await WritesAsync().ConfigureAwait(false);
            else if (phase == "reads-after-restart") await PersistenceAsync().ConfigureAwait(false);
            else throw new ArgumentException("Phase must be full or reads-after-restart.", nameof(phase));
        }
        catch (Exception error)
        {
            _error = error.GetType().FullName + ": " + error.Message;
            Console.Error.WriteLine(_error);
        }

        string prefix = phase == "full" ? "" : "restart-";
        var normalized = new
        {
            phase,
            error = _error,
            steps = _steps.Select(x => new { op = x.Op, input = x.Input, legacy = x.Legacy, modern = x.Modern, verdict = x.Verdict, mapping = x.Mapping }),
        };
        string json = JsonSerializer.Serialize(normalized, Canonical.JsonOptions);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var counts = new
        {
            MATCH = _steps.Count(x => x.Verdict == "MATCH"),
            MAPPED = _steps.Count(x => x.Verdict == "MAPPED" && x.Mapping?.StartsWith("KNOWN_LEGACY_DEFECT", StringComparison.Ordinal) != true),
            KNOWN_LEGACY_DEFECT = _steps.Count(x => x.Mapping?.StartsWith("KNOWN_LEGACY_DEFECT", StringComparison.Ordinal) == true),
            MISMATCH = _steps.Count(x => x.Verdict == "MISMATCH"),
        };
        await File.WriteAllTextAsync(Path.Combine(directory, prefix + "normalized-results.json"), json).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(directory, prefix + "results.json"), JsonSerializer.Serialize(new
        {
            phase,
            counts,
            error = _error,
            normalizedSha256 = hash,
            steps = _steps.Select(x => new { op = x.Op, input = x.Input, legacy = x.Legacy, modern = x.Modern, verdict = x.Verdict, mapping = x.Mapping, legacyRaw = x.LegacyRaw, modernRaw = x.ModernRaw }),
        }, Canonical.JsonOptions)).ConfigureAwait(false);
        var report = new StringBuilder($"# SOAP 1.1 versus gRPC parity: {phase}\n\nNormalized SHA256: `{hash}`\n\nCounts: `{JsonSerializer.Serialize(counts)}`\n\n");
        if (_error is not null) report.AppendLine("Infrastructure/scenario error: " + _error);
        foreach (var step in _steps)
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"## {step.Op}: {step.Verdict}\n\n{step.Mapping}\n\nInput:\n```json\n{step.Input}\n```\nLegacy normalized:\n```json\n{step.Legacy?.ToJsonString(Canonical.JsonOptions) ?? "null"}\n```\nModern normalized:\n```json\n{step.Modern?.ToJsonString(Canonical.JsonOptions) ?? "null"}\n```\nLegacy raw (original decimal text preserved):\n```xml\n{step.LegacyRaw}\n```\nModern raw (exact decimal units/nanos preserved):\n```json\n{step.ModernRaw}\n```\n");
        }

        await File.WriteAllTextAsync(Path.Combine(directory, prefix + "report.md"), report.ToString()).ConfigureAwait(false);
        if (phase == "full" && _error is null)
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "state.json"), JsonSerializer.Serialize(_state, Canonical.JsonOptions)).ConfigureAwait(false);
        }

        Console.WriteLine(JsonSerializer.Serialize(counts));
        Console.WriteLine("NORMALIZED_SHA256=" + hash);
        return _error is not null ? 2 : counts.MISMATCH > 0 ? 1 : 0;
    }

    private async Task ReadsAsync()
    {
        await CallAsync("GetCatalogBrands", new()).ConfigureAwait(false);
        await CallAsync("GetCatalogTypes", new()).ConfigureAwait(false);
        foreach (var (brand, type) in new[] { (0, 0), (2, 0), (0, 2), (5, 1), (-1, 0), (99, 99) })
        {
            await CallAsync("GetCatalogItems", Input(new { brandIdFilter = brand, typeIdFilter = type })).ConfigureAwait(false);
        }

        foreach (int id in new[] { 1, 12, 0, 9999 })
        {
            await CallAsync("FindCatalogItem", Input(new { id })).ConfigureAwait(false);
        }

        foreach (var (date, itemId) in new[] { ("2017-09-20T00:00:00", 1), ("2017-09-21T00:00:00", 1), ("2017-09-20T00:00:00", 3), ("2017-09-22T18:30:00", 1) })
        {
            await CallAsync("GetAvailableStock", Input(new { date, catalogItemId = itemId })).ConfigureAwait(false);
        }

        foreach (string day in new[] { "2017-09-18", "2017-09-21", "2017-09-22", "2017-10-01", "2017-12-25", "2018-01-01" })
        {
            await CallAsync("GetDiscount", Input(new { day })).ConfigureAwait(false);
        }
    }

    private async Task WritesAsync()
    {
        var initial = await CallAsync("GetCatalogItems", Input(new { brandIdFilter = 0, typeIdFilter = 0 })).ConfigureAwait(false);
        var created = new WrittenItem("created", Max(initial.Legacy) + 1, Max(initial.Modern) + 1);
        await CallAsync("CreateCatalogItem", Input(new { item = Item(999, "Parity Mug", "12.3456") })).ConfigureAwait(false);
        _state.Written.Add(created);
        await FindAsync(created).ConfigureAwait(false);
        await CallAsync("GetCatalogItems", Input(new { brandIdFilter = 2, typeIdFilter = 1 })).ConfigureAwait(false);
        var updateLegacy = Item(created.LegacyId, "Parity changed", "0.0001", 5, 3);
        var updateModern = Item(created.ModernId, "Parity changed", "0.0001", 5, 3);
        await CallAsync("UpdateCatalogItem", Input(new { item = updateLegacy }), Input(new { item = updateModern })).ConfigureAwait(false);
        var updated = await FindAsync(created).ConfigureAwait(false);
        await CallAsync("UpdateCatalogItem", Input(new { item = Item(9999, "Unknown", "12.3456") })).ConfigureAwait(false);
        foreach (var (date, available) in new[] { ("2017-11-01", 7), ("2017-09-20", 101) })
        {
            await CallAsync("CreateAvailableStock", Input(new { stock = new { StockId = 999, Date = date, CatalogItemId = 1, AvailableStock = available } })).ConfigureAwait(false);
            await CallAsync("GetAvailableStock", Input(new { date, catalogItemId = 1 })).ConfigureAwait(false);
        }

        int removalIndex = _steps.Count;
        var removal = await CallAsync("RemoveCatalogItem", Input(new { item = Item(created.LegacyId, "", "0") }), Input(new { item = Item(created.ModernId, "", "0") })).ConfigureAwait(false);
        int findIndex = _steps.Count;
        var after = await FindAsync(created).ConfigureAwait(false);
        if (Has(removal.Legacy, "fault") && IsOk(removal.Modern) && JsonNode.DeepEquals(after.Legacy, updated.Legacy) && IsNotFound(after.Modern))
        {
            _state.RetainedLegacyId = created.LegacyId;
            _state.RetainedLegacyItem = after.Legacy?.DeepClone();
            _steps[removalIndex] = removal with { Verdict = "MAPPED", Mapping = "KNOWN_LEGACY_DEFECT: detached EF6 Remove faults; gRPC deletes. Retained legacy row verified by the next Find. Exception type established independently by the committed legacy baseline harness; SOAP hides exception details." };
            _steps[findIndex] = after with { Verdict = "MAPPED", Mapping = "KNOWN_LEGACY_DEFECT: verified retained legacy row versus gRPC NOT_FOUND after deletion." };
        }

        await CallAsync("RemoveCatalogItem", Input(new { item = Item(9999, "", "0") })).ConfigureAwait(false);
        await ValidationAsync("empty-name", "", "0").ConfigureAwait(false);
        await ValidationAsync("negative-price", "Negative Price", "-1.25").ConfigureAwait(false);
        await ValidationAsync("null-name", null, "0").ConfigureAwait(false);
        var final = await CallAsync("GetCatalogItems", Input(new { brandIdFilter = 0, typeIdFilter = 0 })).ConfigureAwait(false);
        _state.LegacyItems = final.Legacy?.DeepClone().AsArray();
        _state.ModernItems = final.Modern?.DeepClone().AsArray();
    }

    private async Task ValidationAsync(string label, string? name, string price)
    {
        var before = await CallAsync("GetCatalogItems", Input(new { brandIdFilter = 0, typeIdFilter = 0 })).ConfigureAwait(false);
        var written = new WrittenItem(label, Max(before.Legacy) + 1, Max(before.Modern) + 1);
        await CallAsync("CreateCatalogItem", Input(new { item = Item(999, name, price) })).ConfigureAwait(false);
        _state.Written.Add(written);
        await FindAsync(written).ConfigureAwait(false);
    }

    private async Task PersistenceAsync()
    {
        foreach (var written in _state.Written) await FindAsync(written).ConfigureAwait(false);
        foreach (var (date, expectedStock) in new[] { ("2017-09-20", 101), ("2017-11-01", 7) })
        {
            var stock = await CallAsync("GetAvailableStock", Input(new { date, catalogItemId = 1 })).ConfigureAwait(false);
            bool persistedStock = JsonNode.DeepEquals(stock.Legacy, JsonValue.Create(expectedStock)) && JsonNode.DeepEquals(stock.Modern, JsonValue.Create(expectedStock));
            _steps.Add(new Step("PersistenceStockCheckpoint", Input(new { date, catalogItemId = 1, expectedStock }), stock.Legacy?.DeepClone(), stock.Modern?.DeepClone(), persistedStock ? "MATCH" : "MISMATCH", "Both restarted servers must retain the written stock value.", stock.LegacyRaw, stock.ModernRaw));
        }

        var current = await CallAsync("GetCatalogItems", Input(new { brandIdFilter = 0, typeIdFilter = 0 })).ConfigureAwait(false);
        var expected = Input(new { legacy = _state.LegacyItems, modern = _state.ModernItems });
        bool persisted = JsonNode.DeepEquals(current.Legacy, _state.LegacyItems) && JsonNode.DeepEquals(current.Modern, _state.ModernItems);
        _steps.Add(new Step("PersistenceCheckpoint", expected, current.Legacy?.DeepClone(), current.Modern?.DeepClone(), persisted ? "MATCH" : "MISMATCH",
            "Each side must independently equal its own persisted full-phase snapshot; cross-side defects do not excuse lost writes.", "full-phase snapshot versus restarted SOAP list", "full-phase snapshot versus restarted gRPC list"));
    }

    private Task<Step> FindAsync(WrittenItem item) => CallAsync("FindCatalogItem", Input(new { id = item.LegacyId }), Input(new { id = item.ModernId }));

    private async Task<Step> CallAsync(string op, JsonObject input, JsonObject? modernInput = null)
    {
        modernInput ??= input;
        var pendingLegacy = CaptureAsync(() => legacy.CallAsync(op, input));
        var pendingModern = CaptureAsync(() => modern.CallAsync(op, modernInput));
        await Task.WhenAll(pendingLegacy, pendingModern).ConfigureAwait(false);
        var left = await pendingLegacy.ConfigureAwait(false);
        var right = await pendingModern.ConfigureAwait(false);
        string verdict = "MISMATCH";
        string? mapping = null;
        bool transport = Has(left.Value, "transportError") || Has(right.Value, "transportError") || Status(right.Value) is "Unavailable" or "DeadlineExceeded";
        if (!transport && JsonNode.DeepEquals(left.Value, right.Value)) verdict = "MATCH";
        else if (!transport && left.Value is null && IsNotFound(right.Value)) { verdict = "MAPPED"; mapping = "legacy null ↔ gRPC NOT_FOUND"; }
        else if (!transport && Has(left.Value, "fault") && Has(right.Value, "status")) { verdict = "MAPPED"; mapping = "legacy SOAP fault ↔ gRPC " + right.Value!["status"]; }
        else if (!transport && IsKnownDeleteConsequence(op, input, modernInput, left.Value, right.Value))
        {
            verdict = "MAPPED";
            mapping = "KNOWN_LEGACY_DEFECT: retained deleted row / max-plus-one ID reuse consequence. Only the verified retained row is removed and recorded generated IDs remapped; all other fields must match exactly.";
        }

        var step = new Step(op, Input(new { legacy = input, modern = modernInput }), left.Value, right.Value, verdict, mapping, left.Raw, right.Raw);
        _steps.Add(step);
        Console.WriteLine($"{op}: {verdict} {mapping}\nLegacy raw: {left.Raw}\nModern raw: {right.Raw}");
        return step;
    }

    private bool IsKnownDeleteConsequence(string op, JsonObject input, JsonObject modernInput, JsonNode? left, JsonNode? right)
    {
        if (_state.RetainedLegacyId is not { } retained) return false;
        if (op == "FindCatalogItem" && input["id"]!.GetValue<int>() == retained)
        {
            var expectedModern = _state.ModernItems?.SingleOrDefault(x => x!["Id"]!.GetValue<int>() == modernInput["id"]!.GetValue<int>());
            return JsonNode.DeepEquals(left, _state.RetainedLegacyItem) && (expectedModern is null ? IsNotFound(right) :
                JsonNode.DeepEquals(WithoutNavigation(right), expectedModern));
        }

        if (op is not ("GetCatalogItems" or "FindCatalogItem")) return false;
        var transformed = left?.DeepClone();
        if (transformed is JsonArray rows)
        {
            var retainedRow = rows.SingleOrDefault(x => x!["Id"]!.GetValue<int>() == retained);
            if (retainedRow is not null)
            {
                if (!JsonNode.DeepEquals(retainedRow, WithoutNavigation(_state.RetainedLegacyItem))) return false;
                rows.Remove(retainedRow);
            }

            foreach (var row in rows) RemapId(row);
        }
        else if (transformed is JsonObject item && item.ContainsKey("Id")) RemapId(item);
        else return false;
        return JsonNode.DeepEquals(transformed, right);
    }

    private void RemapId(JsonNode? item)
    {
        var written = _state.Written.SingleOrDefault(x => x.Label != "created" && x.LegacyId == item!["Id"]!.GetValue<int>());
        if (written is not null) item!["Id"] = written.ModernId;
    }

    private static JsonNode? WithoutNavigation(JsonNode? item)
    {
        var copy = item?.DeepClone();
        if (copy is JsonObject value) { value["CatalogBrand"] = null; value["CatalogType"] = null; }
        return copy;
    }

    private static async Task<Outcome> CaptureAsync(Func<Task<Outcome>> call)
    {
        try { return await call().ConfigureAwait(false); }
        catch (Exception error) { return new Outcome(Canonical.Node(new { transportError = error.GetType().FullName, message = error.Message }), error.ToString()); }
    }

    private static JsonObject Item(int id, string? name, string price, int brand = 2, int type = 1) => Input(new
    {
        Id = id,
        Name = name,
        Description = "Parity Ünicode <&>",
        Price = price,
        Picturefilename = "parity.png",
        CatalogBrandId = brand,
        CatalogTypeId = type,
    });
    private static JsonObject Input(object value) => Canonical.Node(value)!.AsObject();
    private static int Max(JsonNode? value) => value is JsonArray rows ? rows.Max(x => x!["Id"]!.GetValue<int>()) : throw new InvalidDataException("Cannot derive generated IDs: GetCatalogItems did not return a list. Inspect the preceding SOAP/gRPC results.");
    private static bool Has(JsonNode? value, string key) => value is JsonObject obj && obj.ContainsKey(key);
    private static string? Status(JsonNode? value) => value is JsonObject obj ? obj["status"]?.ToString() : null;
    private static bool IsNotFound(JsonNode? value) => value is JsonObject obj && obj["status"]?.ToString() == "NotFound";
    private static bool IsOk(JsonNode? value) => value is JsonObject obj && obj["ok"]?.GetValue<bool>() == true;
}
