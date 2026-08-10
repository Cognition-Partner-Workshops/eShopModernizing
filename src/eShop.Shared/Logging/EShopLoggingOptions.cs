using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace eShop.Shared.Logging;

/// <summary>Logging settings, bound from the <c>EShopLogging</c> configuration section.</summary>
public sealed class EShopLoggingOptions
{
    /// <summary>Legacy rolling appender path (<c>logFiles\myapp.log</c>). Empty disables the file sink.</summary>
    public const string DefaultFilePath = "logFiles/myapp.log";

    /// <summary>Legacy <c>maximumFileSize</c> of 10 MB.</summary>
    public const long DefaultFileSizeLimitBytes = 10L * 1024 * 1024;

    /// <summary>Legacy <c>maxSizeRollBackups</c> of 5.</summary>
    public const int DefaultRetainedFileCountLimit = 5;

    public LogEventLevel MinimumLevel { get; init; } = LogEventLevel.Information;

    /// <summary>Level for the noisy framework namespaces (<c>Microsoft.*</c>, <c>System.*</c>).</summary>
    public LogEventLevel MicrosoftMinimumLevel { get; init; } = LogEventLevel.Warning;

    public string FilePath { get; init; } = DefaultFilePath;

    public long FileSizeLimitBytes { get; init; } = DefaultFileSizeLimitBytes;

    public int RetainedFileCountLimit { get; init; } = DefaultRetainedFileCountLimit;

    public static EShopLoggingOptions FromConfiguration(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new EShopLoggingOptions
        {
            MinimumLevel = ParseLevel(section["MinimumLevel"], LogEventLevel.Information),
            MicrosoftMinimumLevel = ParseLevel(section["MicrosoftMinimumLevel"], LogEventLevel.Warning),
            FilePath = section["FilePath"] ?? DefaultFilePath,
            FileSizeLimitBytes = long.TryParse(section["FileSizeLimitBytes"], out var size) ? size : DefaultFileSizeLimitBytes,
            RetainedFileCountLimit = int.TryParse(section["RetainedFileCountLimit"], out var count) ? count : DefaultRetainedFileCountLimit,
        };
    }

    private static LogEventLevel ParseLevel(string? value, LogEventLevel fallback) =>
        // "ALL" is the legacy log4net root level and has no Serilog equivalent name.
        string.Equals(value, "ALL", StringComparison.OrdinalIgnoreCase)
            ? LogEventLevel.Verbose
            : Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var level) ? level : fallback;
}
