using System;
using System.IO;
using Motely.Filters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Motely;

public static class JamlConfigLoader
{
    public static bool TryLoadFromJaml(
        string filePath,
        out JamlRootDocument? config,
        out string? error)
    {
        config = null;
        error = null;
        try
        {
            if (!File.Exists(filePath))
            {
                error = $"File not found: {filePath}";
                return false;
            }
            return TryLoadFromJamlString(File.ReadAllText(filePath), out config, out error);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryLoadFromJamlString(
        string content,
        out JamlRootDocument? config,
        out string? error)
    {
        config = null;
        error = null;
        if (string.IsNullOrWhiteSpace(content))
        {
            error = "JAML content is empty.";
            return false;
        }
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            config = deserializer.Deserialize<JamlRootDocument>(content);
            if (config == null)
            {
                error = "JAML document was empty.";
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
}
