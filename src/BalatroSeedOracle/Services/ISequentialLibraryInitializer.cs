namespace BalatroSeedOracle.Services;

/// <summary>
/// Abstraction for initializing the sequential library root (Motely.DB).
/// Desktop implements via Motely.DB.SequentialLibrary; Browser does not register.
/// </summary>
public interface ISequentialLibraryInitializer
{
    void SetLibraryRoot(string path);
}
