namespace Zilean.Tests.Collections;

/// <summary>
/// Marks tests that mutate process-global state (ZILEAN_PYTHON_PYLIB env var or
/// static TorznabCapabilities lists). DisableParallelization ensures these tests
/// cannot interleave with each other; assembly-wide xunit.runner.json further
/// disables all collection parallelization so ApiTestCollection tests that read
/// the same static state also serialize. Provides PostgresLifecycleFixture for the
/// PythonUnavailableHealthCheckTests integration test that needs a healthy DB.
/// </summary>
[CollectionDefinition(nameof(SerializedEnvVarCollection), DisableParallelization = true)]
public class SerializedEnvVarCollection : ICollectionFixture<PostgresLifecycleFixture>;