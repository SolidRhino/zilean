namespace Zilean.Shared.Features.Shell;

/// <summary>
/// Defines a service for executing external shell commands via CliWrap.
/// </summary>
public interface IShellExecutionService
{
    /// <summary>
    /// Executes the command described by <paramref name="options"/>, streaming stdout and stderr to the console.
    /// </summary>
    /// <param name="options">The command configuration to execute.</param>
    /// <returns>A task representing the asynchronous command execution.</returns>
    Task ExecuteCommand(ShellCommandOptions options);
}

/// <summary>
/// Default implementation of <see cref="IShellExecutionService"/> that shells out to external
/// processes using CliWrap, piping stdout/stderr to the console.
/// </summary>
public class ShellExecutionService(ILogger<ShellExecutionService> logger) : IShellExecutionService
{
    /// <summary>
    /// Executes the command described by <paramref name="options"/>, streaming stdout and stderr to the console.
    /// </summary>
    /// <param name="options">The command configuration to execute.</param>
    /// <returns>A task representing the asynchronous command execution.</returns>
    public async Task ExecuteCommand(ShellCommandOptions options)
    {
        try
        {
            var arguments = options.ArgumentsBuilder.RenderArguments(propertyKeySeparator: options.PropertyKeySeparator);

            if (options.ShowOutput)
            {
                logger.LogInformation(string.IsNullOrEmpty(options.PreCommandMessage)
                    ? $"Executing: {options.Command} {arguments}"
                    : options.PreCommandMessage);
            }

            var executionDirectory = string.IsNullOrEmpty(options.WorkingDirectory)
                ? Directory.GetCurrentDirectory()
                : options.WorkingDirectory;

            await using var stdOut = Console.OpenStandardOutput();
            await using var stdErr = Console.OpenStandardError();

            await Cli.Wrap(options.Command)
                .WithWorkingDirectory(executionDirectory)
                .WithArguments(arguments)
                .WithEnvironmentVariables(options.EnvironmentVariables)
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToStream(stdOut))
                .WithStandardErrorPipe(PipeTarget.ToStream(stdErr))
                .ExecuteAsync(options.CancellationToken);
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation("Command execution was cancelled");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Command execution was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command");
        }
    }
}