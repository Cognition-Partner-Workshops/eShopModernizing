using System.Diagnostics;

namespace eShop.Shared.Tests;

/// <summary>
/// Covers the NET-63 guard rail: the grep fallback script under <c>eShopModernized/build/</c> and the
/// banned-symbol list consumed by Microsoft.CodeAnalysis.BannedApiAnalyzers (RS0030).
/// The banned token is assembled at runtime so these test sources do not themselves trip the guard.
/// </summary>
public class BannedSerializerGuardTests
{
    private const string BannedType = "Binary" + "Formatter";
    private const string BannedNamespace = "System.Runtime.Serialization.Formatters." + "Binary";

    [Fact]
    public void BannedSymbolsFile_ListsTheBannedSerializer()
    {
        var banned = File.ReadAllText(Path.Combine(SolutionRoot(), "BannedSymbols.txt"));

        Assert.Contains($"T:{BannedNamespace}.{BannedType};", banned, StringComparison.Ordinal);
    }

    [Fact]
    public void GuardScript_PassesOnTheModernizedSolution()
    {
        if (!GuardScriptRunnable)
        {
            return;
        }

        var (exitCode, output) = RunGuard(SolutionRoot());

        Assert.True(exitCode == 0, output);
    }

    [Fact]
    public void GuardScript_FailsOnASampleUsage()
    {
        if (!GuardScriptRunnable)
        {
            return;
        }

        var sampleRoot = Directory.CreateTempSubdirectory("banned-api-guard-").FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(sampleRoot, "Sample.cs"),
                $"using {BannedNamespace};\n\npublic class Sample\n{{\n    public void Go() => _ = new {BannedType}();\n}}\n");

            var (exitCode, output) = RunGuard(sampleRoot);

            Assert.Equal(1, exitCode);
            Assert.Contains("Sample.cs", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(sampleRoot, recursive: true);
        }
    }

    [Fact]
    public void GuardScript_PassesOnACleanTree()
    {
        if (!GuardScriptRunnable)
        {
            return;
        }

        var cleanRoot = Directory.CreateTempSubdirectory("banned-api-guard-clean-").FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(cleanRoot, "Sample.cs"),
                "public class Sample\n{\n    public string Go() => \"ok\";\n}\n");

            var (exitCode, output) = RunGuard(cleanRoot);

            Assert.True(exitCode == 0, output);
        }
        finally
        {
            Directory.Delete(cleanRoot, recursive: true);
        }
    }

    // The fallback script is bash; CI and the container images are Linux.
    private static bool GuardScriptRunnable => !OperatingSystem.IsWindows();

    private static (int ExitCode, string Output) RunGuard(string scanRoot)
    {
        var startInfo = new ProcessStartInfo("bash")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(Path.Combine(SolutionRoot(), "build", "check-no-binaryformatter.sh"));
        startInfo.ArgumentList.Add(scanRoot);

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output);
    }

    private static string SolutionRoot()
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
