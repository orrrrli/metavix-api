namespace API.IntegrationTests.Infrastructure;

/// <summary>
/// Shares a single <see cref="CustomWebApplicationFactory"/> (and its one Postgres
/// Testcontainer + booted host) across every integration test class, and forces them
/// to run serially. Without this each class span up its own factory via IClassFixture,
/// and booting several WebApplicationFactory&lt;Program&gt; instances in parallel raced on
/// the shared entry point ("The entry point exited without ever building an IHost").
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}
