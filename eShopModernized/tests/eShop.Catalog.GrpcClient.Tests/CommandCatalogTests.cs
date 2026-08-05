using eShop.Catalog.GrpcClient;

namespace eShop.Catalog.GrpcClient.Tests;

public class CommandCatalogTests
{
    /// <summary>Every operation of the legacy WCF <c>eShopWCFService.ICatalogService</c>.</summary>
    private static readonly string[] LegacyOperations =
    [
        "FindCatalogItem",
        "GetCatalogBrands",
        "GetCatalogItems",
        "GetCatalogTypes",
        "GetAvailableStock",
        "CreateAvailableStock",
        "CreateCatalogItem",
        "UpdateCatalogItem",
        "RemoveCatalogItem",
        "GetDiscount",
    ];

    /// <summary>The operations the retired WinForms client actually called (contradiction C-14).</summary>
    private static readonly string[] WinFormsOperations =
    [
        "GetCatalogBrands",
        "GetCatalogTypes",
        "GetCatalogItems",
        "GetAvailableStock",
        "CreateAvailableStock",
        "GetDiscount",
    ];

    [Theory]
    [MemberData(nameof(LegacyOperationNames))]
    public void EveryLegacyOperationHasAVerb(string operation)
        => Assert.Contains(CommandCatalog.All, command => command.LegacyOperation.Contains(operation, StringComparison.Ordinal));

    [Theory]
    [MemberData(nameof(WinFormsOperationNames))]
    public void EveryOperationTheWinFormsClientCalledHasADedicatedVerb(string operation)
        => Assert.Single(CommandCatalog.All, command => string.Equals(command.LegacyOperation, operation, StringComparison.Ordinal));

    [Fact]
    public void VerbsAreUnique()
        => Assert.Equal(
            CommandCatalog.All.Count,
            CommandCatalog.All.Select(command => command.Verb).Distinct(StringComparer.Ordinal).Count());

    [Fact]
    public void UsageTextDocumentsEveryVerb()
    {
        foreach (var command in CommandCatalog.All)
        {
            Assert.Contains(command.Verb, CommandCatalog.UsageText, StringComparison.Ordinal);
            Assert.Contains(command.LegacyOperation, CommandCatalog.UsageText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void UsageTextStatesTheDefaultAddress()
        => Assert.Contains(CommandLine.DefaultAddress, CommandCatalog.UsageText, StringComparison.Ordinal);

    [Theory]
    [InlineData("brands")]
    [InlineData("add-stock")]
    [InlineData("demo")]
    [InlineData("get-discount")]
    public void EveryAliasResolvesToAKnownVerb(string alias)
        => Assert.True(CommandCatalog.IsKnown(CommandLine.Canonicalize(alias)));

    [Fact]
    public void AnUnknownVerbIsNotAccepted()
        => Assert.False(CommandCatalog.IsKnown("frobnicate"));

    public static TheoryData<string> LegacyOperationNames()
    {
        var data = new TheoryData<string>();
        foreach (var operation in LegacyOperations)
        {
            data.Add(operation);
        }

        return data;
    }

    public static TheoryData<string> WinFormsOperationNames()
    {
        var data = new TheoryData<string>();
        foreach (var operation in WinFormsOperations)
        {
            data.Add(operation);
        }

        return data;
    }
}
