namespace Zilean.Tests.Collections;

/// <summary>
/// xUnit collection definition that shares a single <see cref="PostgresLifecycleFixture"/>
/// (ephemeral Postgres container + in-process API host) across all API integration tests.
/// Serializes tests within the collection so they cannot interleave.
/// </summary>
[CollectionDefinition(nameof(ApiTestCollection))]
public class ApiTestCollection : ICollectionFixture<PostgresLifecycleFixture>;
