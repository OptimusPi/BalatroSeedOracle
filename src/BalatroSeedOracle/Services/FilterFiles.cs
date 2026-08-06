using System.Collections.Generic;
using System.IO;
using System.Linq;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Services;

/// <summary>
/// JamlFilters/ in the run directory. JAML in, JAML out. No cache, no interface, no ceremony.
/// </summary>
public static class FilterFiles
{
    public const string Dir = "JamlFilters";

    /// <summary>All .jaml filter files, excluding temp/unsaved scratch.</summary>
    public static IEnumerable<string> List() =>
        Directory
            .GetFiles(Dir, "*.jaml")
            .Where(f => !Path.GetFileName(f).StartsWith("_"))
            .OrderBy(Path.GetFileName);

    /// <summary>Bare name → JamlFilters/name.jaml; explicit paths pass through.</summary>
    public static string Resolve(string nameOrPath) =>
        Path.IsPathRooted(nameOrPath) || nameOrPath.Contains(Path.DirectorySeparatorChar)
            ? nameOrPath
            : Path.Combine(Dir, Path.HasExtension(nameOrPath) ? nameOrPath : nameOrPath + ".jaml");

    public static JamlConfig? Load(string path, out string? error)
    {
        if (!JamlConfigLoader.TryLoad(File.ReadAllText(path), out var config, out error))
            return null;
        return config;
    }

    public static void Save(JamlConfig config, string path) =>
        File.WriteAllText(path, JamlConfigLoader.ToJaml(config));

    public static void Delete(string path) => File.Delete(path);
}
