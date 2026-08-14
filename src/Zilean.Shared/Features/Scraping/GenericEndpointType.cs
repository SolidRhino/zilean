namespace Zilean.Shared.Features.Scraping;

/// <summary>
/// Specifies the kind of generic ingestion endpoint being scraped.
/// </summary>
public enum GenericEndpointType
{
    /// <summary>Another Zilean instance exposing its catalog for scraping.</summary>
    Zilean = 0,

    /// <summary>A Zurg Real-Debrid streaming server.</summary>
    Zurg = 1,

    /// <summary>An untyped/generic endpoint with no special handling.</summary>
    Generic = 2
}