namespace Zilean.Scraper.Features.Commands;

/// <summary>
/// Default command for the Zilean scraper CLI; logs a completion message and exits.
/// </summary>
/// <param name="logger">Logger for diagnostic output.</param>
public sealed class DefaultCommand(ILogger<DefaultCommand> logger) : Command<DefaultCommand.Settings>
{
    /// <summary>
    /// Settings for <see cref="DefaultCommand"/>. No options or arguments are accepted.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
    }

    /// <summary>
    /// Executes the default command, logging that execution completed.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>Zero on successful completion.</returns>
    public override int Execute(CommandContext context, Settings settings)
    {
        logger.LogInformation("Zilean Scraper: Execution Completed");
        return 0;
    }
}
