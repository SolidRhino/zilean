namespace Zilean.ApiService.Features.Sync;

/// <summary>
/// Holds shared state tracking whether an on-demand scrape is currently running, used to prevent concurrent invocations.
/// </summary>
public class SyncOnDemandState
{
    /// <summary>
    /// Gets or sets a value indicating whether an on-demand scrape job is currently running.
    /// </summary>
    public bool IsRunning { get; set; }
}
