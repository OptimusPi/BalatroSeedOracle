using Motely.Filters;

namespace Motely;

// Compatibility shim. BSO call sites reference `Motely.JamlConfigLoader.TryLoadFromJamlString`
// (the older API surface that produced MotelyJsonConfig). Upstream Motely moved the
// real loader to `Motely.Filters.JamlConfigLoader` and switched its output to
// JamlConfig. Until the call sites move to JamlConfig directly, this shim lets the
// existing names resolve and still produce a MotelyJsonConfig.
public static class JamlConfigLoader
{
    public static bool TryLoadFromJamlString(string jaml, out MotelyJsonConfig? config, out string? error) =>
        MotelyJsonConfigYaml.TryLoad(jaml, out config, out error);

    public static bool TryLoadFromJaml(string jaml, out MotelyJsonConfig? config, out string? error) =>
        MotelyJsonConfigYaml.TryLoad(jaml, out config, out error);
}
