# Testing

## Unit Tests

Stack: **xUnit**, **NSubstitute**, **FluentAssertions**

Unit tests cover the `Application` layer handlers. All dependencies are mocked through the interfaces defined in `Application`, so no database or HTTP server is required.

```bash
# Run all unit tests
dotnet test tests/Application.Tests/Application.Tests.csproj

# Run all tests in the solution
dotnet test
```

## Integration Tests

Stack: **WebApplicationFactory** + **Testcontainers**

Integration tests spin up the API with a real PostgreSQL container and assert end-to-end behavior.

```bash
dotnet test tests/Integration.Tests/Integration.Tests.csproj
```

## Adding Tests

For the full testing workflow, see `.claude/recipes/05-add-testing.md`.
