using Xunit;

namespace eShop.Shared.Tests;

/// <summary>
/// Environment variables are process-wide, so the test classes that mutate them must not run in
/// parallel with each other: one class restoring a variable while another reads it produced
/// intermittent CI failures.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection
{
    public const string Name = "environment-variables";
}
