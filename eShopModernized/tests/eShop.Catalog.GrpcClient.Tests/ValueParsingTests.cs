using System.Globalization;
using eShop.Catalog.GrpcClient;

namespace eShop.Catalog.GrpcClient.Tests;

public class ValueParsingTests
{
    [Fact]
    public void ParseInt_ReadsAnInvariantInteger()
        => Assert.Equal(-42, ValueParsing.ParseInt("-42", "quantity"));

    [Theory]
    [InlineData("")]
    [InlineData("three")]
    [InlineData("3.0")]
    [InlineData("1 000")]
    public void ParseInt_RejectsAnythingElse(string value)
    {
        var exception = Assert.Throws<CommandLineException>(() => ValueParsing.ParseInt(value, "quantity"));

        Assert.Contains("quantity", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void ParsePositiveInt_RejectsIdsThatCannotExist(string value)
        => Assert.Throws<CommandLineException>(() => ValueParsing.ParsePositiveInt(value, "itemId"));

    [Fact]
    public void ParseNonNegativeInt_AcceptsZeroBecauseAShipmentCanEmptyTheStock()
    {
        Assert.Equal(0, ValueParsing.ParseNonNegativeInt("0", "quantity"));
        Assert.Throws<CommandLineException>(() => ValueParsing.ParseNonNegativeInt("-1", "quantity"));
    }

    [Fact]
    public void ParseDecimal_KeepsTheExactBase10Value()
        => Assert.Equal(19.505m, ValueParsing.ParseDecimal("19.505", "price"));

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public void ParseDecimal_ReadsTheSameValueOnEveryHostCulture(string culture)
        => WithCulture(culture, () =>
        {
            Assert.Equal(12.50m, ValueParsing.ParseDecimal("12.50", "price"));

            // '12,50' is a valid price on a German desktop; the CLI takes one format everywhere.
            Assert.Throws<CommandLineException>(() => ValueParsing.ParseDecimal("12,50", "price"));
        });

    [Fact]
    public void ParseDate_ReadsAnIsoDateAsMidnightUtc()
    {
        var parsed = ValueParsing.ParseDate("2026-08-05", "date");

        Assert.Equal(new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc), parsed);
        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    public void ParseDate_ReadsTheSameDayOnEveryHostCulture(string culture)
        => WithCulture(culture, () =>
        {
            Assert.Equal(new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc), ValueParsing.ParseDate("2026-03-04", "date"));

            // '04/03/2026' is 4 March in de-DE and 3 April in en-US — the ambiguity the WinForms
            // client had with Convert.ToDateTime.
            Assert.Throws<CommandLineException>(() => ValueParsing.ParseDate("04/03/2026", "date"));
        });

    [Theory]
    [InlineData("today")]
    [InlineData("TODAY")]
    public void ParseDate_AcceptsTheTodayKeyword(string value)
        => Assert.Equal(DateTime.UtcNow.Date, ValueParsing.ParseDate(value, "date"));

    [Theory]
    [InlineData("2026-13-01")]
    [InlineData("2026-02-30")]
    [InlineData("tomorrow")]
    [InlineData("")]
    public void ParseDate_RejectsAnythingElse(string value)
    {
        var exception = Assert.Throws<CommandLineException>(() => ValueParsing.ParseDate(value, "date"));

        Assert.Contains(ValueParsing.DateFormat, exception.Message, StringComparison.Ordinal);
    }

    private static void WithCulture(string culture, Action assertions)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);

        try
        {
            assertions();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
