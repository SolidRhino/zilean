namespace Zilean.Shared.Features.Shell;

/// <summary>
/// Configures options for a shell command executed via <see cref="IShellExecutionService"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ShellCommandOptions
{
    /// <summary>
    /// The command (executable path or name) to run.
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// The builder used to construct command-line arguments.
    /// </summary>
    public ArgumentsBuilder? ArgumentsBuilder { get; set; } = new();

    /// <summary>
    /// Whether the command should run in non-interactive mode.
    /// </summary>
    public bool NonInteractive { get; set; }

    /// <summary>
    /// Whether to log command output and status messages.
    /// </summary>
    public bool ShowOutput { get; set; } = false;

    /// <summary>
    /// The working directory for the command, or <c>null</c> to use the current directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// The separator character between an argument key and its value when rendering arguments.
    /// </summary>
    public char PropertyKeySeparator { get; set; } = ' ';

    /// <summary>
    /// An optional message logged before command execution when <see cref="ShowOutput"/> is enabled.
    /// </summary>
    public string? PreCommandMessage { get; set; }

    /// <summary>
    /// An optional message logged on successful command completion.
    /// </summary>
    public string? SuccessCommandMessage { get; set; }

    /// <summary>
    /// An optional message logged when command execution fails.
    /// </summary>
    public string? FailureCommandMessage { get; set; }

    /// <summary>
    /// Additional environment variables to set for the command process.
    /// </summary>
    public Dictionary<string, string?> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// The cancellation token to cancel command execution.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}