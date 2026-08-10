namespace eShop.Shared.Tests;

/// <summary>Sets environment variables for the duration of a test and restores them afterwards.</summary>
internal sealed class ScopedEnvironmentVariables : IDisposable
{
    private readonly Dictionary<string, string?> _originalValues = new(StringComparer.Ordinal);

    public ScopedEnvironmentVariables(params (string Name, string? Value)[] variables)
    {
        foreach (var (name, value) in variables)
        {
            _originalValues[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    public void Dispose()
    {
        foreach (var (name, value) in _originalValues)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
