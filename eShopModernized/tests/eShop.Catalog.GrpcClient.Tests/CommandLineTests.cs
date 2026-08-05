using eShop.Catalog.GrpcClient;

namespace eShop.Catalog.GrpcClient.Tests;

public class CommandLineTests
{
    [Fact]
    public void Parse_WithoutAddress_UsesTheDefaultEndpoint()
    {
        var commandLine = CommandLine.Parse(["get-brands"]);

        Assert.Equal(CommandLine.DefaultAddress, commandLine.Address);
        Assert.Equal("get-brands", commandLine.Verb);
        Assert.Empty(commandLine.Arguments);
        Assert.False(commandLine.IsEmpty);
    }

    public static TheoryData<string[]> AddressPositions() => new()
    {
        new[] { "--address", "http://catalog:8080", "get-stock", "3", "2026-08-05" },
        new[] { "get-stock", "--address", "http://catalog:8080", "3", "2026-08-05" },
        new[] { "get-stock", "3", "2026-08-05", "--address", "http://catalog:8080" },
    };

    [Theory]
    [MemberData(nameof(AddressPositions))]
    public void Parse_TakesTheAddressOptionFromAnyPosition(string[] args)
    {
        var commandLine = CommandLine.Parse(args);

        Assert.Equal("http://catalog:8080", commandLine.Address);
        Assert.Equal("get-stock", commandLine.Verb);
        Assert.Equal(["3", "2026-08-05"], commandLine.Arguments);
    }

    [Fact]
    public void Parse_WithAddressButNoValue_Fails()
    {
        var exception = Assert.Throws<CommandLineException>(() => CommandLine.Parse(["get-brands", "--address"]));

        Assert.Contains("--address", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_WithoutArguments_IsAnEmptyHelpRequest()
    {
        var commandLine = CommandLine.Parse([]);

        Assert.True(commandLine.IsEmpty);
        Assert.Equal(CommandLine.HelpVerb, commandLine.Verb);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("-?")]
    [InlineData("help")]
    public void Parse_WithAHelpFlag_IsANonEmptyHelpRequest(string flag)
    {
        var commandLine = CommandLine.Parse([flag]);

        Assert.False(commandLine.IsEmpty);
        Assert.Equal(CommandLine.HelpVerb, commandLine.Verb);
    }

    [Theory]
    [InlineData("brands", "get-brands")]
    [InlineData("types", "get-types")]
    [InlineData("items", "get-items")]
    [InlineData("find", "find-item")]
    [InlineData("create", "create-item")]
    [InlineData("update", "update-item")]
    [InlineData("remove", "remove-item")]
    [InlineData("stock", "get-stock")]
    [InlineData("add-stock", "create-stock")]
    [InlineData("discount", "get-discount")]
    [InlineData("demo", "catalog")]
    public void Canonicalize_ResolvesTheShortFormsShippedWithTheFirstSample(string alias, string expected)
        => Assert.Equal(expected, CommandLine.Canonicalize(alias));

    [Fact]
    public void Canonicalize_LeavesAnUnknownVerbAlone()
        => Assert.Equal("frobnicate", CommandLine.Canonicalize("frobnicate"));

    [Fact]
    public void Canonicalize_IsCaseSensitiveSoTyposAreReportedRatherThanGuessed()
        => Assert.Equal("Get-Brands", CommandLine.Canonicalize("Get-Brands"));

    [Fact]
    public void Required_ReportsTheNameOfTheMissingArgument()
    {
        var commandLine = CommandLine.Parse(["create-stock", "3"]);

        Assert.Equal("3", commandLine.Required(0, "itemId"));
        Assert.Null(commandLine.Optional(1));

        var exception = Assert.Throws<CommandLineException>(() => commandLine.Required(1, "date"));
        Assert.Contains("date", exception.Message, StringComparison.Ordinal);
    }
}
