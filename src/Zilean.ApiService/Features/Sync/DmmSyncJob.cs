namespace Zilean.ApiService.Features.Sync;

/// <summary>
/// Coravel-scheduled job that shell-executes the <c>scraper</c> binary with the <c>dmm-sync</c> argument to scrape DebridMediaManager data.
/// </summary>
/// <param name="shellExecutionService">The shell execution service used to run the scraper binary.</param>
/// <param name="logger">The logger for sync job diagnostics.</param>
/// <param name="dbContext">The database context used to check whether parsed pages already exist.</param>
public class DmmSyncJob(IShellExecutionService shellExecutionService, ILogger<DmmSyncJob> logger, ZileanDbContext dbContext) : IInvocable, ICancellableInvocable
{
    /// <summary>
    /// Gets or sets the cancellation token provided by the Coravel scheduler.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
    private const string DmmSyncArg = "dmm-sync";

    /// <summary>
    /// Executes the <c>dmm-sync</c> scraper command, streaming output to the logger.
    /// </summary>
    /// <returns>A task that completes when the scraper command finishes.</returns>
    public async Task Invoke()
    {
        logger.LogInformation("Dmm SyncJob started");

        var argumentBuilder = ArgumentsBuilder.Create();
        argumentBuilder.AppendArgument(DmmSyncArg, string.Empty, false, false);

        await shellExecutionService.ExecuteCommand(new ShellCommandOptions
        {
            Command = Path.Combine(AppContext.BaseDirectory, "scraper"),
            ArgumentsBuilder = argumentBuilder,
            ShowOutput = true,
            CancellationToken = CancellationToken
        });

        logger.LogInformation("Dmm SyncJob completed");
    }

    /// <summary>
    /// Determines whether the job should run on startup by checking if any parsed pages already exist in the database.
    /// </summary>
    /// <returns><c>true</c> if parsed pages exist (no startup run needed); otherwise <c>false</c>.</returns>
    // ReSharper disable once MethodSupportsCancellation
    public Task<bool> ShouldRunOnStartup() => dbContext.ParsedPages.AnyAsync();
}