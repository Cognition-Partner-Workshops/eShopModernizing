using System.Globalization;
using System.Text;

namespace eShop.Catalog.GrpcClient;

/// <summary>One CLI verb: its syntax, the legacy WCF operation it calls and what it replaces.</summary>
/// <param name="Verb">The canonical verb.</param>
/// <param name="Arguments">Argument syntax shown in the usage text.</param>
/// <param name="LegacyOperation">The <c>ICatalogService</c> operation invoked.</param>
/// <param name="Replaces">The retired WinForms interaction the verb reproduces.</param>
public sealed record CommandDescriptor(string Verb, string Arguments, string LegacyOperation, string Replaces);

/// <summary>
/// The verbs the client understands. The list is the contract between the usage text, the
/// dispatcher in <see cref="Program" /> and <c>docs/winforms-retirement.md</c>: every operation of
/// the legacy WCF <c>ICatalogService</c> has exactly one verb.
/// </summary>
public static class CommandCatalog
{
    public static IReadOnlyList<CommandDescriptor> All { get; } = new[]
    {
        new CommandDescriptor("get-brands", string.Empty, "GetCatalogBrands", "Main Catalog tab: the Brand filter drop-down"),
        new CommandDescriptor("get-types", string.Empty, "GetCatalogTypes", "Main Catalog tab: the Type filter drop-down"),
        new CommandDescriptor("get-items", "[brandId] [typeId]", "GetCatalogItems", "Main Catalog tab: the product grid (0 = no filter)"),
        new CommandDescriptor("find-item", "<id>", "FindCatalogItem", "no WinForms screen; completes the contract"),
        new CommandDescriptor("get-stock", "<itemId> <yyyy-MM-dd|today>", "GetAvailableStock", "Inventory tab: \"Search\" stock availability"),
        new CommandDescriptor("create-stock", "<itemId> <yyyy-MM-dd|today> <quantity>", "CreateAvailableStock", "Inventory tab: \"Add Shipment\""),
        new CommandDescriptor("get-discount", "[yyyy-MM-dd|today]", "GetDiscount", "Main Catalog tab: the discount banner and discounted prices"),
        new CommandDescriptor("create-item", "<name> <price> <brandId> <typeId> [description] [picture]", "CreateCatalogItem", "no WinForms screen; completes the contract"),
        new CommandDescriptor("update-item", "<id> <name> <price> <brandId> <typeId> [description] [picture]", "UpdateCatalogItem", "no WinForms screen; completes the contract"),
        new CommandDescriptor("remove-item", "<id>", "RemoveCatalogItem", "no WinForms screen; completes the contract"),
        new CommandDescriptor("catalog", "[brandId] [typeId]", "GetCatalogBrands + GetCatalogTypes + GetCatalogItems + GetDiscount", "the whole Main Catalog tab as the WinForms client loaded it"),
        new CommandDescriptor("inventory", "<itemId> <yyyy-MM-dd|today> <quantity>", "CreateAvailableStock + GetAvailableStock", "the whole Inventory tab: add a shipment, then read it back"),
    };

    public static string UsageText { get; } = BuildUsageText();

    /// <summary>True when <paramref name="verb" /> is a canonical verb of this client.</summary>
    public static bool IsKnown(string verb) => All.Any(command => string.Equals(command.Verb, verb, StringComparison.Ordinal));

    private static string BuildUsageText()
    {
        var width = All.Max(command => command.Verb.Length + command.Arguments.Length) + 3;

        var builder = new StringBuilder()
            .AppendLine("eShop catalog gRPC client — the cross-platform replacement for the retired WinForms")
            .AppendLine("desktop client (decision D-07). Every operation of the legacy WCF ICatalogService has a verb.")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Usage: eShop.Catalog.GrpcClient [--address <url>] <command> [arguments]   (default {CommandLine.DefaultAddress})")
            .AppendLine()
            .AppendLine("Commands:");

        foreach (var command in All)
        {
            var syntax = command.Arguments.Length == 0 ? command.Verb : $"{command.Verb} {command.Arguments}";
            builder.AppendLine(CultureInfo.InvariantCulture, $"  {syntax.PadRight(width)}{command.LegacyOperation}");
        }

        return builder
            .AppendLine()
            .AppendLine("Dates are yyyy-MM-dd (or 'today') and prices are invariant-culture decimals, on every host locale.")
            .ToString();
    }
}
