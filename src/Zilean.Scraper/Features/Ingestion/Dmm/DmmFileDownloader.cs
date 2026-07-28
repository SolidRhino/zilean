namespace Zilean.Scraper.Features.Ingestion.Dmm;

public class DmmFileDownloader(ILogger<DmmFileDownloader> logger, ZileanConfiguration configuration)
{
    private const string RepoUrl = "https://github.com/debridmediamanager/hashlists.git";
    private const string RepoBranch = "main";
    /// <summary>
    /// Git credential helper that supplies the GitHub token from the environment ($GITHUB_TOKEN)
    /// without embedding it in the remote URL or .git/config. Invoked by git as a shell command;
    /// the token is expanded from the process environment at auth time, never appears in argv or logs.
    /// </summary>
    private const string GitCredentialHelper = "!f() { echo \"username=x-access-token\"; echo \"password=$GITHUB_TOKEN\"; }; f";
    private const int MaxRetryAttempts = 5;
    private static readonly TimeSpan _initialRetryDelay = TimeSpan.FromSeconds(5);

    private static readonly IReadOnlyCollection<string> _filesToIgnore =
    [
        "index.html",
        "404.html",
        "dedupe.sh",
        "CNAME",
        ".git",
    ];

    public async Task<string> DownloadFileToTempPath(DmmLastImport? dmmLastImport, CancellationToken cancellationToken)
    {
        logger.LogInformation("Syncing DMM Hashlists");

        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data", "DMMHashlists");

        if (dmmLastImport is not null)
        {
            if (DateTime.UtcNow - dmmLastImport.OccuredAt < TimeSpan.FromMinutes(configuration.Dmm.MinimumReDownloadIntervalMinutes))
            {
                logger.LogInformation("DMM Hashlists sync not required as last sync was less than the configured {Minutes} minutes re-download interval set in DMM Configuration.", configuration.Dmm.MinimumReDownloadIntervalMinutes);
                return dataDirectory;
            }
        }

        var repoDirectory = Path.Combine(dataDirectory, "repo");
        var gitDirectory = Path.Combine(repoDirectory, ".git");

        if (Directory.Exists(gitDirectory))
        {
            logger.LogInformation("Repository exists, pulling latest changes");
            await GitPullAsync(repoDirectory, RepoUrl, cancellationToken);
        }
        else
        {
            logger.LogInformation("Repository does not exist, cloning");
            EnsureDirectoryIsClean(dataDirectory);
            await GitCloneAsync(RepoUrl, repoDirectory, cancellationToken);
        }

        CopyFilesToDataDirectory(repoDirectory, dataDirectory);

        logger.LogInformation("Synced Repository to {DataDirectory}", dataDirectory);

        return dataDirectory;
    }

    private async Task GitCloneAsync(string repoUrl, string targetDirectory, CancellationToken cancellationToken)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            ApplyGitAuth(process.StartInfo);
            process.StartInfo.ArgumentList.Add("clone");
            process.StartInfo.ArgumentList.Add("--depth");
            process.StartInfo.ArgumentList.Add("1");
            process.StartInfo.ArgumentList.Add("--branch");
            process.StartInfo.ArgumentList.Add(RepoBranch);
            process.StartInfo.ArgumentList.Add("--single-branch");
            process.StartInfo.ArgumentList.Add(repoUrl);
            process.StartInfo.ArgumentList.Add(targetDirectory);

            await RunGitProcessAsync(process, "clone", cancellationToken);
        }, "clone", targetDirectory, cancellationToken);
    }

    private async Task GitPullAsync(string repoDirectory, string repoUrl, CancellationToken cancellationToken)
    {
        // Scrub any token-embedded remote URL left by older versions (GetRepoUrlWithAuth embedded
        // the token directly in .git/config). Reset origin to the public URL — auth is now via
        // the credential helper, so the remote must not carry credentials.
        var setUrlProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        setUrlProcess.StartInfo.ArgumentList.Add("-C");
        setUrlProcess.StartInfo.ArgumentList.Add(repoDirectory);
        setUrlProcess.StartInfo.ArgumentList.Add("remote");
        setUrlProcess.StartInfo.ArgumentList.Add("set-url");
        setUrlProcess.StartInfo.ArgumentList.Add("origin");
        setUrlProcess.StartInfo.ArgumentList.Add(RepoUrl);

        await RunGitProcessAsync(setUrlProcess, "remote set-url", cancellationToken);

        // Pull latest changes with retry. Auth is supplied via the credential helper.
        await ExecuteWithRetryAsync(async () =>
        {
            var pullProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            ApplyGitAuth(pullProcess.StartInfo);
            pullProcess.StartInfo.ArgumentList.Add("-C");
            pullProcess.StartInfo.ArgumentList.Add(repoDirectory);
            pullProcess.StartInfo.ArgumentList.Add("pull");
            pullProcess.StartInfo.ArgumentList.Add("--ff-only");

            await RunGitProcessAsync(pullProcess, "pull", cancellationToken);
        }, "pull", repoDirectory, cancellationToken);
    }

    /// <summary>
    /// Configures the git process to authenticate via the <c>$GITHUB_TOKEN</c> environment variable
    /// using an inline credential helper, so the token is never embedded in the remote URL or
    /// <c>.git/config</c>. Git expands <c>$GITHUB_TOKEN</c> from the process environment at auth
    /// time; the literal text stays safe in argv. <c>GIT_TERMINAL_PROMPT</c> prevents any
    /// interactive prompt hanging the sync.
    /// </summary>
    private static void ApplyGitAuth(ProcessStartInfo psi)
    {
        psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add($"credential.helper={GitCredentialHelper}");
    }

    private async Task RunGitProcessAsync(Process process, string operation, CancellationToken cancellationToken)
    {
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            logger.LogError("Git {Operation} failed with exit code {ExitCode}: {Error}", operation, process.ExitCode, error);
            throw new InvalidOperationException($"Git {operation} failed: {error}");
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            logger.LogDebug("Git {Operation} output: {Output}", operation, output);
        }
    }

    private async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, string targetDirectory, CancellationToken cancellationToken)
    {
        var attempt = 0;
        var delay = _initialRetryDelay;

        while (true)
        {
            attempt++;
            try
            {
                await operation();
                return;
            }
            catch (InvalidOperationException ex) when (attempt < MaxRetryAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Git {Operation} attempt {Attempt}/{MaxAttempts} failed. Retrying in {Delay} seconds... Error: {Error}",
                    operationName,
                    attempt,
                    MaxRetryAttempts,
                    delay.TotalSeconds,
                    ex.Message);

                // Clean up the target directory before retry for clone operations
                if (operationName == "clone" && Directory.Exists(targetDirectory))
                {
                    try
                    {
                        Directory.Delete(targetDirectory, true);
                    }
                    catch (Exception cleanupEx)
                    {
                        logger.LogWarning("Failed to clean up directory {Directory} before retry: {Error}", targetDirectory, cleanupEx.Message);
                    }
                }

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60)); // Exponential backoff, max 60 seconds
            }
        }
    }

    private void CopyFilesToDataDirectory(string repoDirectory, string dataDirectory)
    {
        var files = Directory.GetFiles(repoDirectory);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);

            if (_filesToIgnore.Contains(fileName))
            {
                continue;
            }

            var destPath = Path.Combine(dataDirectory, fileName);
            File.Copy(file, destPath, true);
        }
    }

    private static void EnsureDirectoryIsClean(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }

        Directory.CreateDirectory(directory);
    }
}
