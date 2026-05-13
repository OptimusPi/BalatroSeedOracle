using System;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Motely.Filters;

// BSO-owned helper that mirrors the parts of upstream JamlConfigLoader BSO used to
// call ("TryLoadFromJamlString" / "TryLoadFromJaml") but produces the BSO
// MotelyJsonConfig DTO. The real upstream loader now produces JamlConfig instead.
// We keep this seam in place so existing UI / cache / filter-browser code keeps
// compiling. Conversion MotelyJsonConfig → JamlConfig happens at the search
// boundary, not here.
public static class MotelyJsonConfigYaml
{
    private static IDeserializer CreateDeserializer() =>
        new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    private static ISerializer CreateSerializer() =>
        new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

    public static bool TryLoad(string jaml, out MotelyJsonConfig? config, out string? error)
    {
        config = null;
        error = null;
        if (string.IsNullOrWhiteSpace(jaml))
        {
            error = "JAML content is empty.";
            return false;
        }

        try
        {
            config = CreateDeserializer().Deserialize<MotelyJsonConfig>(jaml);
            if (config is null)
            {
                error = "Deserialized config was null.";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryLoadFromFile(string path, out MotelyJsonConfig? config, out string? error)
    {
        config = null;
        error = null;
        if (!File.Exists(path))
        {
            error = $"File not found: {path}";
            return false;
        }
        return TryLoad(File.ReadAllText(path), out config, out error);
    }

    public static string Serialize(MotelyJsonConfig config) =>
        CreateSerializer().Serialize(config);
}


// Minimal JamlFormatter replacement used by the JAML editor tab. Upstream lived in
// `Motely.Filters.JamlFormatter`; BSO keeps a smaller version here that round-trips
// MotelyJsonConfig through YAML for the "format current document" command.
public static class JamlFormatter
{
    public static string Format(MotelyJsonConfig config) =>
        MotelyJsonConfigYaml.Serialize(config);

    public static string Format(string jaml)
    {
        if (string.IsNullOrWhiteSpace(jaml))
            return jaml;
        if (!MotelyJsonConfigYaml.TryLoad(jaml, out var cfg, out _) || cfg is null)
            return jaml;
        return MotelyJsonConfigYaml.Serialize(cfg);
    }
}
