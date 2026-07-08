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
    private readonly ClauseConversionService _clauseConversion;

    public FilterSerializationService(UserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
        _clauseConversion = new ClauseConversionService();
    }

    public string SerializeConfig(JamlConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
            config.Name = "Untitled Filter";
        if (string.IsNullOrWhiteSpace(config.Author))
            config.Author = _userProfileService?.GetAuthorName() ?? "Jimbo";

        return JamlConfigLoader.ToYaml(config);
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

    public IJamlClause? CreateFilterClause(
        string category,
        string itemName,
        ItemConfig config
    )
    {
        var filterItem = new FilterItem
        {
            Category = category,
            Name = itemName,
            ItemKey = $"{category}:{itemName}",
        };
        return _clauseConversion.ConvertFilterItemToClause(filterItem, config);
    }

    public void ConvertSelectionsToFilterClauses(
        ObservableCollection<string> items,
        Dictionary<string, ItemConfig> itemConfigs,
        List<IJamlClause> targetList,
        int defaultScore = 0
    )
    {
        foreach (var item in items)
        {
            var colonIndex = item.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            var category = item.Substring(0, colonIndex);
            var itemNameWithSuffix = item.Substring(colonIndex + 1);

            var hashIndex = itemNameWithSuffix.IndexOf('#');
            var itemName = hashIndex > 0
                ? itemNameWithSuffix.Substring(0, hashIndex)
                : itemNameWithSuffix;

            var itemConfig = itemConfigs.TryGetValue(item, out var cfg) ? cfg : new ItemConfig();

            var filterItem = CreateFilterClause(category, itemName, itemConfig);
            if (filterItem is not null)
            {
                filterItem.Score = itemConfig.Score > 0
                    ? itemConfig.Score
                    : Math.Max(defaultScore, 1);
                targetList.Add(filterItem);
            }
        }
    }
}
