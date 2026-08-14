namespace Zilean.Shared.Features.Statistics;

/// <summary>
/// Base class for tracking metadata about the last import operation (timestamp and status).
/// </summary>
public abstract class BaseLastImport
{
    /// <summary>
    /// The UTC timestamp when the import operation occurred.
    /// </summary>
    public DateTime OccuredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The current status of the import operation.
    /// </summary>
    public ImportStatus Status { get; set; } = ImportStatus.InProgress;
}