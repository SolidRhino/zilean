namespace Zilean.Shared.Features.Statistics;

/// <summary>
/// Represents the status of a metadata import operation.
/// </summary>
public enum ImportStatus
{
    /// <summary>
    /// The import operation is currently in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// The import operation completed successfully.
    /// </summary>
    Complete,

    /// <summary>
    /// The import operation failed.
    /// </summary>
    Failed
}