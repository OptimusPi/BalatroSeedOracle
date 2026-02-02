using System;
using BalatroSeedOracle.Services;
using Motely.DB;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation: delegates to Motely.DB.SequentialLibrary.
/// </summary>
public sealed class SequentialLibraryInitializerService : ISequentialLibraryInitializer
{
    /// <inheritdoc />
    public void SetLibraryRoot(string path)
    {
        SequentialLibrary.SetLibraryRoot(path);
    }
}
