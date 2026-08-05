using System.Globalization;

namespace eShop.Catalog.GrpcClient;

/// <summary>
/// Scalar parsing for the command line. Everything is invariant-culture on purpose: the retired
/// WinForms client parsed quantities with <c>int.Parse</c> and shipment dates with
/// <c>Convert.ToDateTime</c> under the operator's current culture, so the same keystrokes meant
/// different dates on a machine set to <c>en-US</c> and one set to <c>en-GB</c>. This client reads
/// one format regardless of the host locale.
/// </summary>
public static class ValueParsing
{
    /// <summary>The only accepted date format.</summary>
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>Accepted in place of a date, resolving to the current UTC day.</summary>
    public const string TodayKeyword = "today";

    public static int ParseInt(string value, string name)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new CommandLineException($"'{name}' must be an integer, but was '{value}'.");

    public static int ParsePositiveInt(string value, string name)
    {
        var parsed = ParseInt(value, name);

        return parsed > 0
            ? parsed
            : throw new CommandLineException($"'{name}' must be greater than zero, but was '{value}'.");
    }

    public static int ParseNonNegativeInt(string value, string name)
    {
        var parsed = ParseInt(value, name);

        return parsed >= 0
            ? parsed
            : throw new CommandLineException($"'{name}' must not be negative, but was '{value}'.");
    }

    /// <summary>
    /// Parses a decimal in the invariant culture. The wire type is a string
    /// (<c>DecimalValue</c>), so the value is round-tripped through <see cref="decimal" /> to
    /// reject garbage and to normalize the separator, without going through <c>double</c>.
    /// </summary>
    public static decimal ParseDecimal(string value, string name)
        => decimal.TryParse(
            value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : throw new CommandLineException(
                $"'{name}' must be an invariant-culture decimal such as 12.50, but was '{value}'.");

    /// <summary>
    /// Parses a <c>yyyy-MM-dd</c> date (or <c>today</c>) as midnight UTC. The backing columns are
    /// SQL <c>date</c>, so only the date component is significant.
    /// </summary>
    public static DateTime ParseDate(string value, string name)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.Equals(value, TodayKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Date;
        }

        return DateTime.TryParseExact(
            value,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : throw new CommandLineException(
                $"'{name}' must be a {DateFormat} date or '{TodayKeyword}', but was '{value}'.");
    }
}
