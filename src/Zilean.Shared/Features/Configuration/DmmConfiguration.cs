namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Configuration for DebridMediaManager (DMM) scraping and endpoints.
/// Bound from the <c>Zilean__Dmm</c> env var section.
/// </summary>
public class DmmConfiguration
{
    /// <summary>
    /// Enables the DMM metadata scraping scheduler.
    /// Set via the <c>Zilean__Dmm__EnableScraping</c> env var.
    /// </summary>
    public bool EnableScraping { get; set; } = true;

    /// <summary>
    /// Enables the DMM query endpoint.
    /// Set via the <c>Zilean__Dmm__EnableEndpoint</c> env var.
    /// </summary>
    public bool EnableEndpoint { get; set; } = true;

    /// <summary>
    /// Cron expression controlling the DMM scrape schedule.
    /// Set via the <c>Zilean__Dmm__ScrapeSchedule</c> env var.
    /// </summary>
    public string ScrapeSchedule { get; set; } = "0 * * * *";

    /// <summary>
    /// Minimum minutes between re-downloading the same DMM page to avoid redundant fetches.
    /// Set via the <c>Zilean__Dmm__MinimumReDownloadIntervalMinutes</c> env var.
    /// </summary>
    public int MinimumReDownloadIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum number of filtered results returned per DMM scrape query.
    /// Set via the <c>Zilean__Dmm__MaxFilteredResults</c> env var.
    /// </summary>
    public int MaxFilteredResults { get; set; } = 500;

    /// <summary>
    /// Minimum fuzzy-match score (0–1) required when deduplicating DMM entries.
    /// Set via the <c>Zilean__Dmm__MinimumScoreMatch</c> env var.
    /// </summary>
    public double MinimumScoreMatch { get; set; } = 0.85;
}