using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Desktop.Services;

public sealed class SequentialLibraryInitializerService : ISequentialLibraryInitializer
{
    public void SetLibraryRoot(string path)
    {
        // TODO(JAML-port): re-implement persistent search library when MJ exposes a public store
    }
}
