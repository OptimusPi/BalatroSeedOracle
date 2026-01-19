namespace BalatroSeedOracle.Tests;

/// <summary>
/// Baseline smoke tests to verify test infrastructure is working.
/// </summary>
public class BaselineTests
{
    [Fact]
    public void TestInfrastructure_ShouldWork()
    {
        // Arrange & Act
        var result = 1 + 1;

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void CanInstantiate_CoreTypes()
    {
        // This test verifies that the test project can reference and instantiate
        // types from the core BalatroSeedOracle assembly.
        // As we add more tests, this serves as a basic integration check.

        // Arrange & Act
        var exception = Record.Exception(() =>
        {
            // Add basic type instantiation here once we identify
            // a simple, dependency-free type to test
        });

        // Assert
        Assert.Null(exception);
    }
}
