using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Services;

public sealed class FilterSerializationService
{
    private readonly UserProfileService _userProfileService;

    public FilterSerializationService(UserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    public string SerializeConfig(JamlConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
            config.Name = "Untitled Filter";
        if (string.IsNullOrWhiteSpace(config.Author))
            config.Author = _userProfileService?.GetAuthorName() ?? "Jimbo";

        return JamlConfigLoader.ToJaml(config);
    }

    public JamlConfig? DeserializeConfig(string yaml)
    {
        if (!JamlConfigLoader.TryLoad(yaml, out var config, out var error))
        {
            DebugLogger.LogError(
                "FilterSerializationService",
                $"Failed to deserialize config: {error}"
            );
            return null;
        }

        DebugLogger.Log(
            "FilterSerializationService",
            $"Deserialized config: Name='{config?.Name}', Must={(config?.Must.Count ?? 0)}, Should={(config?.Should.Count ?? 0)}, MustNot={(config?.MustNot.Count ?? 0)}"
        );
        return config;
    }

    public JamlConfig? DeserializeConfigFromFile(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                DebugLogger.LogError(
                    "FilterSerializationService",
                    $"File not found: {filePath}"
                );
                return null;
            }

            var text = File.ReadAllText(filePath);
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".yaml" || ext == ".yml" || ext == ".jaml")
            {
                return DeserializeConfig(text);
            }

            DebugLogger.LogError(
                "FilterSerializationService",
                $"Unsupported filter extension '{ext}' for '{filePath}'"
            );
            return null;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(
                "FilterSerializationService",
                $"Error loading config from file '{filePath}': {ex.Message}"
            );
            return null;
        }
    }

}
