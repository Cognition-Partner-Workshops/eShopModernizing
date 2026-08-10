namespace eShop.Web.Infrastructure;

/// <summary>
/// Replacement for the legacy InProc session state: <c>Session["MachineName"]</c> and
/// <c>Session["SessionStartTime"]</c> were only ever rendered in the footer, so the same two values
/// are now process-wide and served from this singleton — no session, no sticky sessions needed.
/// </summary>
public sealed class HostInfo
{
    public string MachineName { get; } = Environment.MachineName;

    /// <summary>When this process started serving, the analogue of the legacy session start time.</summary>
    public DateTime ApplicationStartTime { get; } = DateTime.Now;

    public override string ToString() => $"{MachineName}, {ApplicationStartTime}";
}
