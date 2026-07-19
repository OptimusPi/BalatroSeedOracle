using System;
using System.Collections.Generic;
using BalatroSeedOracle.Helpers;
using Motely.Enums;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Converts the Visual Builder's raw UI selection state (item keys grouped by must/should/
/// mustNot, plus per-item edition/ante/score config) directly into a real <see cref="JamlConfig"/>
/// — the engine's own model, not a duplicate DTO. See CLAUDE.md's "one bridge" rule: this is a
/// UI-selections-to-engine-model builder, not a second JAML representation.
/// </summary>
public interface IFilterConfigurationService
{
    /// <summary>Builds a new <see cref="JamlConfig"/> (fresh <c>Id</c>, Red deck, White stake)
    /// from selected item keys and their per-item <see cref="IJamlClause"/> (edition, antes,
    /// score). <paramref name="itemConfigs"/> keys not present in any selected list are ignored.</summary>
    JamlConfig BuildConfigFromSelections(
        List<string> selectedMust,
        List<string> selectedShould,
        List<string> selectedMustNot,
        Dictionary<string, IJamlClause> itemConfigs,
        string filterName = "",
        string filterDescription = ""
    );
}

public class FilterConfigurationService : IFilterConfigurationService
{
    private readonly UserProfileService _userProfileService;

    public FilterConfigurationService(UserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    public JamlConfig BuildConfigFromSelections(
        List<string> selectedMust,
        List<string> selectedShould,
        List<string> selectedMustNot,
        Dictionary<string, IJamlClause> itemConfigs,
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
        Dictionary<string, IJamlClause> itemConfigs,
        int defaultScore = 0
    )
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item))
                continue;

            if (!itemConfigs.TryGetValue(item, out var clause))
                continue;
            if (clause is null)
                continue;
            if (clause.Score <= 0)
                clause.Score = defaultScore;
            targetList.Add(clause);
        }
    }
}
