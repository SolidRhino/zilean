namespace Zilean.Shared.Features.Python;

public class PythonRuntimeService
{
    private readonly Task _initAsync;
    // ReSharper disable once NotAccessedField.Local
    private IntPtr _mainThreadState;
    private bool _isInitialized;
    private dynamic? _sys;
    private readonly ILogger<PythonRuntimeService> _logger;

    public Task Initialization => _initAsync;
    public bool IsAvailable => _initAsync.IsCompletedSuccessfully;

    public PythonRuntimeService(ILogger<PythonRuntimeService> logger, ZileanConfiguration configuration)
    {
        _logger = logger;
        _initAsync = InitializePythonEngine();
    }

    public async Task StopPythonEngine()
    {
        if (!IsAvailable)
        {
            return;
        }

        await _initAsync;
        _sys.Dispose();

        PythonEngine.Shutdown();

        _isInitialized = false;
    }

    private Task InitializePythonEngine()
    {
        if (_isInitialized)
        {
            return Task.CompletedTask;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var pathToVirtualEnv = Environment.GetEnvironmentVariable("ZILEAN_PYTHON_VENV") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(pathToVirtualEnv))
                {
                    _logger.LogError("`ZILEAN_PYTHON_VENV` env is not set. Python engine will be unavailable.");
                    return Task.FromException(new InvalidOperationException("ZILEAN_PYTHON_VENV environment variable is not set."));
                }

                var path = Environment.GetEnvironmentVariable("PATH").TrimEnd(';');
                path = string.IsNullOrEmpty(path) ? pathToVirtualEnv : path + ";" + pathToVirtualEnv;
                Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PATH", pathToVirtualEnv, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONHOME", pathToVirtualEnv, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONPATH", $@"{pathToVirtualEnv}\Lib\site-packages;{pathToVirtualEnv}\Lib", EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("ZILEAN_PYTHON_PYLIB", $@"{pathToVirtualEnv}\python312.dll", EnvironmentVariableTarget.Process);
            }

            var pythonDllEnv = Environment.GetEnvironmentVariable("ZILEAN_PYTHON_PYLIB");

            if (string.IsNullOrWhiteSpace(pythonDllEnv))
            {
                _logger.LogError("`ZILEAN_PYTHON_PYLIB` env is not set. Python engine will be unavailable.");
                return Task.FromException(new InvalidOperationException("ZILEAN_PYTHON_PYLIB environment variable is not set."));
            }

            Runtime.PythonDLL = pythonDllEnv;
            PythonEngine.Initialize();
            _mainThreadState = PythonEngine.BeginAllowThreads();
            using (Py.GIL())
            {
                _sys = Py.Import("sys");
                _sys.path.append(Path.Combine(AppContext.BaseDirectory, "python"));
            }
            _isInitialized = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialize Python engine: {Message}", e.Message);
            return Task.FromException(e);
        }

        return Task.CompletedTask;
    }
}