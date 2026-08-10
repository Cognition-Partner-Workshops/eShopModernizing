namespace eShop.Shared;

/// <summary>
/// Marker type for assembly scanning. eShop.Shared is the cross-cutting foundation library:
/// NET-61 adds the options/configuration helpers, NET-62 the logging/telemetry helpers and
/// NET-63 the System.Text.Json serialization helpers that replace BinaryFormatter.
/// </summary>
public static class SharedAssemblyMarker
{
    public static readonly System.Reflection.Assembly Assembly = typeof(SharedAssemblyMarker).Assembly;
}
