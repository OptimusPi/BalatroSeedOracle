using System;
using System.Collections.Generic;
using System.Linq;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using Motely.Enums;
using Motely.Filters;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Services;

public interface IFilterConfigurationService
{
    JamlConfig BuildConfigFromSelections(
        List<string> selectedMust,
        List<string> selectedShould,
        List<string> selectedMustNot,
        Dictionary<string, ItemConfig> itemConfigs,
        string filterName = "",
        string filterDescription = ""
    );
}

public class FilterConfigurationService : IFilterConfigurationService
{
    private readonly UserProfileService _userProfileService;
    private readonly ClauseConversionService _clauseConversion;

    public FilterConfigurationService(UserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
        _clauseConversion = new ClauseConversionService();
    }

    public JamlConfig BuildConfigFromSelections(
        List<string> selectedMust,
        List<string> selectedShould,
        List<string> selectedMustNot,
        Dictionary<string, ItemConfig> itemConfigs,
        string filterName = "",
        string filterDescription = ""
    )
    {
        var config = new JamlConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            Deck = MotelyDeck.Red,
            Stake = MotelyStake.White,
            Name = string.IsNullOrEmpty(filterName) ? "Untitled Filter" : filterName,
            Description = filterDescription,
            Author = _userProfileService.GetAuthorName(),
            Must = [],
            Should = [],
            MustNot = [],
        };

        ConvertSelections(selectedMust, config.Must, itemConfigs, 0);
        ConvertSelections(selectedShould, config.Should, itemConfigs, 1);
        ConvertSelections(selectedMustNot, config.MustNot, itemConfigs, 0);

        DebugLogger.Log(
            "FilterConfigurationService",
            $"Built config: {config.Must.Count} must, {config.Should.Count} should, {config.MustNot.Count} mustNot"
        );
        return config;
    }

    private void ConvertSelections(
        List<string> items,
        List<IJamlClause> targetList,
        Dictionary<string, ItemConfig> itemConfigs,
        int defaultScore = 0
    )
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item))
                continue;

            var itemConfig = itemConfigs.TryGetValue(item, out var cfg) ? cfg : new ItemConfig();
            var clause = BuildClause(item, itemConfig, defaultScore);
            if (clause is not null)
                targetList.Add(clause);
        }
    }

    private IJamlClause? BuildClause(string itemKey, ItemConfig config, int defaultScore)
    {
        // Operators: ItemConfig children become a LogicClause
        if (config.ItemType == "Operator" && !string.IsNullOrEmpty(config.OperatorType))
        {
            var isOr = config.OperatorType.Equals("Or", StringComparison.OrdinalIgnoreCase);
            var logic = isOr ? (LogicClause)new OrClause() : new AndClause();
            var children = new List<IJamlClause>();
            foreach (var child in config.Children ?? [])
            {
                var childClause = _clauseConversion.ConvertFilterItemToClause(
                    new FilterItem { Category = child.ItemType, Name = child.ItemName, ItemKey = child.ItemKey },
                    child
                );
                if (childClause is not null)
                    children.Add(childClause);
            }
            logic.Clauses = children.ToArray();
            DebugLogger.Log(
                "FilterConfigurationService",
                $"Created {config.OperatorType} operator with {children.Count} children"
            );
            return logic;
        }

        var colonIndex = itemKey.IndexOf(':');
        if (colonIndex <= 0)
        {
            DebugLogger.LogError("FilterConfigurationService", $"Invalid item key '{itemKey}'");
            return null;
        }

        var category = itemKey.Substring(0, colonIndex);
        var itemNameWithSuffix = itemKey.Substring(colonIndex + 1);
        var hashIndex = itemNameWithSuffix.IndexOf('#');
        var itemName = hashIndex > 0
            ? itemNameWithSuffix.Substring(0, hashIndex)
            : itemNameWithSuffix;

        var filterItem = new FilterItem
        {
            Category = category,
            Name = itemName,
            ItemKey = itemKey,
        };

        var clause = _clauseConversion.ConvertFilterItemToClause(filterItem, config);
        if (clause is not null)
            clause.Score = config.Score > 0 ? config.Score : defaultScore;
        return clause;
    }
}
