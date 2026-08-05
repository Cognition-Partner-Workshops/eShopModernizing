using System.Text.RegularExpressions;

namespace eShop.Catalog.Api.Tests;

/// <summary>
/// Acceptance criterion of NET-61: no static <c>System.Configuration</c> access survives in the
/// modernized solution — all settings must arrive through <c>IOptions&lt;T&gt;</c>.
/// </summary>
public class ConfigurationManagerUsageTests
{
    // Assembled at runtime so this test file does not match its own assertions.
    private static readonly string[] ForbiddenPatterns =
    [
        @"System\.Configuration",
        "Configuration" + @"Manager\.AppSettings",
        "Configuration" + @"Manager\.ConnectionStrings",
        "WebConfiguration" + "Manager",
        @"HostingEnvironment\.ApplicationPhysicalPath",
    ];

    [Fact]
    public void ModernizedSources_DoNotUseStaticConfigurationAccess()
    {
        var solutionRoot = FindSolutionRoot();

        var offenders = Directory
            .EnumerateFiles(solutionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrIntermediate(solutionRoot, path))
            .Where(path => !string.Equals(
                Path.GetFileName(path), Path.GetFileName(SourceFilePath()), StringComparison.Ordinal))
            .SelectMany(path => ForbiddenPatterns
                .Where(pattern => Regex.IsMatch(File.ReadAllText(path), pattern))
                .Select(pattern => $"{Path.GetRelativePath(solutionRoot, path)}: {pattern}"))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void ModernizedProjects_DoNotReferenceSystemConfiguration()
    {
        var solutionRoot = FindSolutionRoot();

        var offenders = Directory
            .EnumerateFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("System.Configuration", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(solutionRoot, path))
            .ToList();

        Assert.Empty(offenders);
    }

    private static bool IsGeneratedOrIntermediate(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Contains("bin") || segments.Contains("obj");
    }

    private static string SourceFilePath([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

    /// <summary>Walks up from the test binaries to the folder holding <c>eShop.sln</c>.</summary>
    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "eShop.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
