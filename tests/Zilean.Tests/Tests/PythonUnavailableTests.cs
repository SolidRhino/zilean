using Zilean.Shared.Features.Python;

namespace Zilean.Tests.Tests;

/// <summary>
/// Unit test for the Python-unavailable branch of PythonRuntimeService. Deterministic:
/// an empty ZILEAN_PYTHON_PYLIB causes InitializePythonEngine to return a faulted Task
/// BEFORE any pythonnet native call. No DB, no Python runtime, no RequiresPython trait.
/// </summary>
public class PythonUnavailableTests
{
    [Fact]
    public async Task EmptyPythonLib_LeavesRuntimeUnavailable_FaultsInitialization()
    {
        var saved = Environment.GetEnvironmentVariable("ZILEAN_PYTHON_PYLIB");
        try
        {
            Environment.SetEnvironmentVariable("ZILEAN_PYTHON_PYLIB", "");

            var runtime = new PythonRuntimeService(
                Substitute.For<ILogger<PythonRuntimeService>>(),
                new ZileanConfiguration());

            runtime.IsAvailable.Should().BeFalse(
                "because an unset ZILEAN_PYTHON_PYLIB must leave the runtime unavailable");
            runtime.Initialization.IsFaulted.Should().BeTrue(
                "because the initialization Task must be faulted when the env var is empty");

            var ex = await FluentActions.Awaiting(() => runtime.Initialization)
                .Should().ThrowAsync<InvalidOperationException>(
                    "because InitializePythonEngine returns Task.FromException for an empty ZILEAN_PYTHON_PYLIB");
            ex.WithMessage("ZILEAN_PYTHON_PYLIB environment variable is not set.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ZILEAN_PYTHON_PYLIB", saved);
        }
    }
}