# BalatroSeedOracle.Tests

Unit tests for Balatro Seed Oracle core functionality.

## Overview

This test project provides unit tests for the core `BalatroSeedOracle` library, focusing on testable business logic without UI dependencies.

## Running Tests

### Using Taskfile (Recommended)

```bash
task test
```

### Using dotnet CLI

```bash
dotnet test tests/BalatroSeedOracle.Tests/BalatroSeedOracle.Tests.csproj -c Release
```

## Test Framework

- **xUnit** - Test framework
- **Moq** - Mocking library for dependencies
- **coverlet.collector** - Code coverage collection

## What to Test

Focus on testing:

- **Services** - Business logic in `src/BalatroSeedOracle/Services/`
- **Helpers** - Utility functions in `src/BalatroSeedOracle/Helpers/`
- **Data transformations** - Filter serialization, configuration parsing
- **Pure logic** - Any code without UI or platform dependencies

Avoid testing:

- ViewModels with complex UI interactions (requires Avalonia test host)
- Platform-specific code (requires platform-specific test setup)
- Motely submodule internals (tested in Motely repo)

## CI Integration

Tests run automatically in CI via `.github/workflows/ci-quality.yml` and are required to pass before PRs can be merged.

## Current Status

The test infrastructure is set up with baseline smoke tests. As the project evolves, add tests for:

- `FilterSerializationService` - JSON/JAML roundtrip
- `FilterConfigurationService` - Filter validation and transformation
- `ConfigurationService` - App settings management
- `SearchStateManager` - Search state transitions

## Adding Tests

1. Create test files that mirror the structure of the code being tested
2. Use descriptive test names following the pattern: `MethodName_Scenario_ExpectedBehavior`
3. Follow Arrange-Act-Assert pattern
4. Mock external dependencies using Moq

Example:

```csharp
public class FilterSerializationServiceTests
{
    [Fact]
    public void SerializeFilter_ValidFilter_ReturnsJsonString()
    {
        // Arrange
        var service = new FilterSerializationService();
        var filter = new MotelyJsonConfig { /* ... */ };

        // Act
        var result = service.Serialize(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"version\"", result);
    }
}
```
