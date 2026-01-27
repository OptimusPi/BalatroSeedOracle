using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// SMART JAML autocomplete - context-aware, schema-driven, with ALL Balatro data
    /// Supports YAML anchors, schema validation, and YAML best practices
    /// </summary>
    public static class JamlAutocompletionHelper
    {
        public static List<ICompletionData> GetCompletionsForContext(string textBeforeCursor)
        {
            var completions = new List<ICompletionData>();

            // Determine context - what are we typing?
            var context = DetermineYamlContext(textBeforeCursor);

            switch (context)
            {
                case YamlCompletionContext.TopLevelProperty:
                    AddTopLevelPropertyCompletions(completions, textBeforeCursor);
                    break;

                case YamlCompletionContext.ClauseProperty:
                    AddClausePropertyCompletions(completions, textBeforeCursor);
                    break;

                case YamlCompletionContext.DeckValue:
                    AddDeckCompletions(completions);
                    break;

                case YamlCompletionContext.StakeValue:
                    AddStakeCompletions(completions);
                    break;

                case YamlCompletionContext.TypeValue:
                    AddTypeValueCompletions(completions);
                    break;

                case YamlCompletionContext.JokerValue:
                    AddJokerCompletions(completions, textBeforeCursor);
                    break;

                case YamlCompletionContext.TarotValue:
                    AddTarotCompletions(completions);
                    break;

                case YamlCompletionContext.SpectralValue:
                    AddSpectralCompletions(completions);
                    break;

                case YamlCompletionContext.PlanetValue:
                    AddPlanetCompletions(completions);
                    break;

                case YamlCompletionContext.VoucherValue:
                    AddVoucherCompletions(completions);
                    break;

                case YamlCompletionContext.EditionValue:
                    AddEditionCompletions(completions);
                    break;

                case YamlCompletionContext.SealValue:
                    AddSealCompletions(completions);
                    break;

                case YamlCompletionContext.EnhancementValue:
                    AddEnhancementCompletions(completions);
                    break;

                case YamlCompletionContext.RankValue:
                    AddRankCompletions(completions);
                    break;

                case YamlCompletionContext.SuitValue:
                    AddSuitCompletions(completions);
                    break;

                case YamlCompletionContext.AntesArray:
                    AddAnteSnippets(completions);
                    break;

                case YamlCompletionContext.SlotsArray:
                    AddSlotSnippets(completions);
                    break;

                case YamlCompletionContext.AnchorDefinition:
                    AddAnchorDefinitionCompletions(completions);
                    break;

                case YamlCompletionContext.AnchorReference:
                    AddAnchorReferenceCompletions(completions, textBeforeCursor);
                    break;

                default:
                    // Fallback: show top-level properties
                    AddTopLevelPropertyCompletions(completions, textBeforeCursor);
                    break;
            }

            return completions;
        }

        private static YamlCompletionContext DetermineYamlContext(string textBefore)
        {
            // Check for anchor definition (key: &anchor_name)
            if (Regex.IsMatch(textBefore, @":\s*&\w*$", RegexOptions.Multiline))
                return YamlCompletionContext.AnchorDefinition;

            // Check for anchor reference (*anchor_name)
            if (Regex.IsMatch(textBefore, @"\*\w*$"))
                return YamlCompletionContext.AnchorReference;

            // Check if we're after "deck:"
            if (Regex.IsMatch(textBefore, @"^deck\s*:\s*$", RegexOptions.Multiline))
                return YamlCompletionContext.DeckValue;

            // Check if we're after "stake:"
            if (Regex.IsMatch(textBefore, @"^stake\s*:\s*$", RegexOptions.Multiline))
                return YamlCompletionContext.StakeValue;

            // Check if we're after "type:" or "Type:"
            if (
                Regex.IsMatch(
                    textBefore,
                    @"type\s*:\s*$",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                )
            )
                return YamlCompletionContext.TypeValue;

            // Check what type was specified to give context-aware value suggestions
            var typeMatch = Regex.Match(
                textBefore,
                @"(?:type|Type)\s*:\s*(Joker|SoulJoker|Voucher|Tarot|TarotCard|Planet|PlanetCard|Spectral|SpectralCard|Tag|Boss|BossBlind|PlayingCard|StandardCard)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );

            // Check if we're after "value:" or "joker:" or "soulJoker:" etc
            if (
                Regex.IsMatch(
                    textBefore,
                    @"(?:value|joker|soulJoker|SoulJoker)\s*:\s*$",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                )
            )
            {
                if (typeMatch.Success)
                {
                    return typeMatch.Groups[1].Value switch
                    {
                        "Joker" or "SoulJoker" => YamlCompletionContext.JokerValue,
                        "Tarot" or "TarotCard" => YamlCompletionContext.TarotValue,
                        "Spectral" or "SpectralCard" => YamlCompletionContext.SpectralValue,
                        "Planet" or "PlanetCard" => YamlCompletionContext.PlanetValue,
                        "Voucher" => YamlCompletionContext.VoucherValue,
                        _ => YamlCompletionContext.ClauseProperty,
                    };
                }
                return YamlCompletionContext.JokerValue; // Default to joker if no type specified
            }

            // Check if we're after "edition:"
            if (
                Regex.IsMatch(
                    textBefore,
                    @"edition\s*:\s*$",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                )
            )
                return YamlCompletionContext.EditionValue;

            // Check if we're after "seal:"
            if (
                Regex.IsMatch(
                    textBefore,
                    @"seal\s*:\s*$",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                )
            )
                return YamlCompletionContext.SealValue;

            // Check if we're after "enhancement:"
            if (
                Regex.IsMatch(
                    textBefore,
                    @"enhancement\s*:\s*$",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                )
            )
                return YamlCompletionContext.EnhancementValue;

            // Check if we're after "rank:"
            if (
                Regex.IsMatch(
                    textBefore,
                    @"rank\s*:\s*$",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                )
            )
                return YamlCompletionContext.RankValue;

            // Check if we're after "suit:"
            if (
                Regex.IsMatch(
                    textBefore,
                    @"suit\s*:\s*$",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                )
            )
                return YamlCompletionContext.SuitValue;

            // Check if we're in "antes:" array
            if (
                Regex.IsMatch(
                    textBefore,
                    @"antes\s*:\s*\[",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                )
            )
                return YamlCompletionContext.AntesArray;

            // Check if we're in "shopSlots:" or "packSlots:" array
            if (
                Regex.IsMatch(
                    textBefore,
                    @"(?:shop|pack)Slots\s*:\s*\[",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                )
            )
                return YamlCompletionContext.SlotsArray;

            // Check if we're at top level (not indented much) or in a clause
            var lines = textBefore.Split('\n');
            var lastLine = lines.LastOrDefault() ?? "";
            var indentLevel = GetIndentLevel(lastLine);

            // Top level (0-2 spaces) = top-level properties
            if (indentLevel <= 2)
                return YamlCompletionContext.TopLevelProperty;

            // Otherwise, we're in a clause
            return YamlCompletionContext.ClauseProperty;
        }

        private static int GetIndentLevel(string line)
        {
            int spaces = 0;
            foreach (char c in line)
            {
                if (c == ' ')
                    spaces++;
                else if (c == '\t')
                    spaces += 4; // Treat tabs as 4 spaces
                else
                    break;
            }
            return spaces;
        }

        private static void AddTopLevelPropertyCompletions(
            List<ICompletionData> completions,
            string textBefore
        )
        {
            // YAML anchor definition snippet (YAML best practice!)
            completions.Add(
                new SmartCompletionData(
                    "anchor-param",
                    "desired_joker: &desired_joker OopsAll6s",
                    "YAML anchor parameter (define reusable value)",
                    priority: 20
                )
            );

            // Top-level metadata
            completions.Add(
                new SmartCompletionData("name", "name: MyFilter", "Filter name", priority: 15)
            );
            completions.Add(
                new SmartCompletionData(
                    "description",
                    "description: Description of filter",
                    "Filter description",
                    priority: 15
                )
            );
            completions.Add(
                new SmartCompletionData("author", "author: YourName", "Author name", priority: 15)
            );
            completions.Add(
                new SmartCompletionData("deck", "deck: Red", "Starting deck", priority: 14)
            );
            completions.Add(
                new SmartCompletionData("stake", "stake: White", "Stake level", priority: 14)
            );

            // Main filter arrays
            completions.Add(
                new SmartCompletionData(
                    "must",
                    "must:\n  - joker: Blueprint\n    antes: [1, 2]",
                    "Items that MUST appear (required)",
                    priority: 15
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "should",
                    "should:\n  - joker: Blueprint\n    antes: [1, 2]\n    score: 10",
                    "Items that SHOULD appear (bonus scoring)",
                    priority: 15
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "mustNot",
                    "mustNot:\n  - joker: Joker\n    antes: [1, 2]",
                    "Items that MUST NOT appear (banned)",
                    priority: 15
                )
            );
        }

        private static void AddClausePropertyCompletions(
            List<ICompletionData> completions,
            string textBefore
        )
        {
            // Check if we're in an And/Or clause
            bool inAndOr = Regex.IsMatch(textBefore, @"(?:And|Or)\s*:\s*$", RegexOptions.Multiline);

            if (inAndOr)
            {
                completions.Add(
                    new SmartCompletionData(
                        "Antes",
                        "Antes: [1, 2, 3, 4, 5, 6, 7, 8]",
                        "Antes array (children inherit from parent)",
                        priority: 15
                    )
                );
                completions.Add(
                    new SmartCompletionData("Mode", "Mode: Max", "Mode: Max or Sum", priority: 14)
                );
                completions.Add(
                    new SmartCompletionData("Score", "Score: 100", "Score weight", priority: 14)
                );
                completions.Add(
                    new SmartCompletionData(
                        "clauses",
                        "clauses:\n  - joker: Blueprint",
                        "Child clauses",
                        priority: 15
                    )
                );
            }
            else
            {
                // Regular clause properties
                completions.Add(
                    new SmartCompletionData("joker", "joker: Blueprint", "Joker name", priority: 15)
                );
                completions.Add(
                    new SmartCompletionData(
                        "soulJoker",
                        "soulJoker: Perkeo",
                        "Soul joker",
                        priority: 15
                    )
                );
                completions.Add(
                    new SmartCompletionData(
                        "voucher",
                        "voucher: Telescope",
                        "Voucher",
                        priority: 14
                    )
                );
                completions.Add(
                    new SmartCompletionData("tarot", "tarot: Fool", "Tarot card", priority: 14)
                );
                completions.Add(
                    new SmartCompletionData(
                        "spectral",
                        "spectral: Spectral",
                        "Spectral card",
                        priority: 14
                    )
                );
                completions.Add(
                    new SmartCompletionData(
                        "planet",
                        "planet: Mercury",
                        "Planet card",
                        priority: 14
                    )
                );
                completions.Add(
                    new SmartCompletionData(
                        "smallblindtag",
                        "smallblindtag: NegativeTag",
                        "Small blind tag",
                        priority: 13
                    )
                );
                completions.Add(
                    new SmartCompletionData(
                        "bigblindtag",
                        "bigblindtag: NegativeTag",
                        "Big blind tag",
                        priority: 13
                    )
                );
                completions.Add(
                    new SmartCompletionData("antes", "antes: [1, 2]", "Antes array", priority: 12)
                );
                completions.Add(
                    new SmartCompletionData("score", "score: 10", "Score weight", priority: 12)
                );
                completions.Add(
                    new SmartCompletionData("edition", "edition: Negative", "Edition", priority: 11)
                );
                completions.Add(
                    new SmartCompletionData(
                        "ShopSlots",
                        "ShopSlots: [0, 1, 2, 3, 4, 5]",
                        "Shop slot positions",
                        priority: 11
                    )
                );
                completions.Add(
                    new SmartCompletionData(
                        "PackSlots",
                        "PackSlots: [0, 1, 2, 3]",
                        "Pack slot positions",
                        priority: 11
                    )
                );
                completions.Add(
                    new SmartCompletionData("And", "And:\n  Mode: Max", "AND clause", priority: 10)
                );
                completions.Add(
                    new SmartCompletionData(
                        "Or",
                        "Or:\n  - joker: Blueprint",
                        "OR clause",
                        priority: 10
                    )
                );
            }
        }

        private static void AddTypeValueCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("Joker", "Joker", "Jokers", priority: 10));
            completions.Add(
                new SmartCompletionData("SoulJoker", "SoulJoker", "Soul jokers", priority: 9)
            );
            completions.Add(new SmartCompletionData("Voucher", "Voucher", "Vouchers", priority: 9));
            completions.Add(
                new SmartCompletionData("TarotCard", "TarotCard", "Tarot cards", priority: 9)
            );
            completions.Add(
                new SmartCompletionData(
                    "SpectralCard",
                    "SpectralCard",
                    "Spectral cards",
                    priority: 9
                )
            );
            completions.Add(
                new SmartCompletionData("PlanetCard", "PlanetCard", "Planet cards", priority: 8)
            );
            completions.Add(
                new SmartCompletionData(
                    "StandardCard",
                    "StandardCard",
                    "Playing cards",
                    priority: 7
                )
            );
            completions.Add(new SmartCompletionData("Boss", "Boss", "Boss blinds", priority: 7));
        }

        private static void AddJokerCompletions(
            List<ICompletionData> completions,
            string textBefore
        )
        {
            // Wildcards
            completions.Add(
                new SmartCompletionData(
                    "anylegendary",
                    "anylegendary",
                    "Any legendary",
                    priority: 15
                )
            );
            completions.Add(
                new SmartCompletionData("anyrare", "anyrare", "Any rare", priority: 14)
            );
            completions.Add(
                new SmartCompletionData("anyuncommon", "anyuncommon", "Any uncommon", priority: 13)
            );
            completions.Add(
                new SmartCompletionData("anycommon", "anycommon", "Any common", priority: 12)
            );
            completions.Add(
                new SmartCompletionData("anyjoker", "anyjoker", "Any joker", priority: 11)
            );

            // Popular jokers
            var popularJokers = new Dictionary<string, string>
            {
                { "Blueprint", "Blueprint" },
                { "Brainstorm", "Brainstorm" },
                { "Perkeo", "Perkeo (Legendary)" },
                { "MrBones", "Mr Bones" },
                { "Showman", "Showman" },
                { "Egg", "Egg" },
                { "GiftCard", "Gift Card" },
                { "MerryAndy", "Merry Andy" },
                { "Triboulet", "Triboulet (Legendary)" },
                { "Canio", "Canio (Legendary)" },
                { "Chicot", "Chicot (Legendary)" },
                { "Yorick", "Yorick" },
                { "HitTheRoad", "Hit The Road" },
                { "TheIdol", "The Idol" },
                { "Cavendish", "Cavendish" },
                { "OopsAll6s", "Oops All 6s" },
            };

            foreach (var joker in popularJokers)
            {
                if (BalatroData.Jokers.ContainsKey(joker.Key))
                {
                    completions.Add(
                        new SmartCompletionData(joker.Key, joker.Key, joker.Value, priority: 10)
                    );
                }
            }

            // All other jokers
            foreach (var joker in BalatroData.Jokers.OrderBy(j => j.Value))
            {
                if (!joker.Key.StartsWith("any") && !popularJokers.ContainsKey(joker.Key))
                {
                    completions.Add(
                        new SmartCompletionData(joker.Key, joker.Key, joker.Value, priority: 5)
                    );
                }
            }
        }

        private static void AddEditionCompletions(List<ICompletionData> completions)
        {
            completions.Add(
                new SmartCompletionData("Negative", "Negative", "Negative", priority: 10)
            );
            completions.Add(
                new SmartCompletionData("Polychrome", "Polychrome", "Polychrome", priority: 9)
            );
            completions.Add(
                new SmartCompletionData("Holographic", "Holographic", "Holographic", priority: 8)
            );
            completions.Add(new SmartCompletionData("Foil", "Foil", "Foil", priority: 7));
        }

        private static void AddSealCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("Red", "Red", "Red seal", priority: 10));
            completions.Add(new SmartCompletionData("Blue", "Blue", "Blue seal", priority: 9));
            completions.Add(new SmartCompletionData("Gold", "Gold", "Gold seal", priority: 8));
            completions.Add(
                new SmartCompletionData("Purple", "Purple", "Purple seal", priority: 7)
            );
        }

        private static void AddEnhancementCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("Bonus", "Bonus", "Bonus", priority: 10));
            completions.Add(new SmartCompletionData("Mult", "Mult", "Mult", priority: 9));
            completions.Add(new SmartCompletionData("Wild", "Wild", "Wild", priority: 8));
            completions.Add(new SmartCompletionData("Glass", "Glass", "Glass", priority: 7));
            completions.Add(new SmartCompletionData("Steel", "Steel", "Steel", priority: 6));
            completions.Add(new SmartCompletionData("Stone", "Stone", "Stone", priority: 5));
            completions.Add(new SmartCompletionData("Lucky", "Lucky", "Lucky", priority: 4));
            completions.Add(new SmartCompletionData("Gold", "Gold", "Gold", priority: 3));
        }

        private static void AddRankCompletions(List<ICompletionData> completions)
        {
            var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            foreach (var rank in ranks)
            {
                completions.Add(new SmartCompletionData(rank, rank, $"Rank {rank}", priority: 5));
            }
        }

        private static void AddSuitCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("Hearts", "Hearts", "Hearts", priority: 10));
            completions.Add(
                new SmartCompletionData("Diamonds", "Diamonds", "Diamonds", priority: 9)
            );
            completions.Add(new SmartCompletionData("Clubs", "Clubs", "Clubs", priority: 8));
            completions.Add(new SmartCompletionData("Spades", "Spades", "Spades", priority: 7));
        }

        private static void AddDeckCompletions(List<ICompletionData> completions)
        {
            foreach (var deck in BalatroData.Decks.OrderBy(d => d.Value))
            {
                completions.Add(
                    new SmartCompletionData(deck.Key, deck.Key, deck.Value, priority: 5)
                );
            }
        }

        private static void AddStakeCompletions(List<ICompletionData> completions)
        {
            foreach (var stake in BalatroData.Stakes)
            {
                completions.Add(
                    new SmartCompletionData(stake.Key, stake.Key, stake.Value, priority: 5)
                );
            }
        }

        private static void AddAnteSnippets(List<ICompletionData> completions)
        {
            completions.Add(
                new SmartCompletionData(
                    "1-8",
                    "[1, 2, 3, 4, 5, 6, 7, 8]",
                    "All antes",
                    priority: 10
                )
            );
            completions.Add(
                new SmartCompletionData("1-4", "[1, 2, 3, 4]", "Early game", priority: 9)
            );
            completions.Add(
                new SmartCompletionData("5-8", "[5, 6, 7, 8]", "Late game", priority: 9)
            );
            completions.Add(
                new SmartCompletionData(
                    "2-12",
                    "[2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]",
                    "Antes 2-12 (extended)",
                    priority: 8
                )
            );
        }

        private static void AddSlotSnippets(List<ICompletionData> completions)
        {
            completions.Add(
                new SmartCompletionData(
                    "all-shop",
                    "[0, 1, 2, 3, 4, 5]",
                    "All shop slots",
                    priority: 10
                )
            );
            completions.Add(
                new SmartCompletionData("all-pack", "[0, 1, 2, 3]", "All pack slots", priority: 10)
            );
            completions.Add(
                new SmartCompletionData("first-3", "[0, 1, 2]", "First 3 slots", priority: 9)
            );
            completions.Add(
                new SmartCompletionData("top-3", "[2, 3, 4]", "Top 3 shop slots", priority: 9)
            );
        }

        private static void AddAnchorDefinitionCompletions(List<ICompletionData> completions)
        {
            completions.Add(
                new SmartCompletionData(
                    "joker-param",
                    "desired_joker: &desired_joker OopsAll6s",
                    "Joker parameter anchor",
                    priority: 15
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "score-param",
                    "score_per_joker: &score_per_joker 100",
                    "Score parameter anchor",
                    priority: 14
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "cluster-pattern",
                    "joker_cluster: &joker_cluster\n  - joker: *desired_joker\n    ShopSlots: [2,3,4]\n    score: *score_per_joker",
                    "Joker cluster pattern with anchors",
                    priority: 13
                )
            );
        }

        private static void AddAnchorReferenceCompletions(
            List<ICompletionData> completions,
            string textBefore
        )
        {
            // Extract all anchor definitions from the text
            var anchorMatches = Regex.Matches(
                textBefore,
                @"(\w+)\s*:\s*&(\w+)",
                RegexOptions.Multiline
            );

            foreach (Match match in anchorMatches)
            {
                var anchorName = match.Groups[2].Value;
                completions.Add(
                    new SmartCompletionData(
                        anchorName,
                        $"*{anchorName}",
                        $"Reference anchor: {anchorName}",
                        priority: 10
                    )
                );
            }

            // Common anchor names
            if (anchorMatches.Count == 0)
            {
                completions.Add(
                    new SmartCompletionData(
                        "desired_joker",
                        "*desired_joker",
                        "Reference joker anchor (define first)",
                        priority: 5
                    )
                );
            }
        }

        private static void AddTarotCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("any", "any", "Any Tarot", priority: 10));
            foreach (var tarot in BalatroData.TarotCards.OrderBy(t => t.Value))
            {
                if (tarot.Key != "any")
                {
                    completions.Add(
                        new SmartCompletionData(tarot.Key, tarot.Key, tarot.Value, priority: 5)
                    );
                }
            }
        }

        private static void AddSpectralCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("any", "any", "Any Spectral", priority: 10));
            foreach (var spectral in BalatroData.SpectralCards.OrderBy(s => s.Value))
            {
                if (spectral.Key != "any")
                {
                    completions.Add(
                        new SmartCompletionData(
                            spectral.Key,
                            spectral.Key,
                            spectral.Value,
                            priority: 5
                        )
                    );
                }
            }
        }

        private static void AddPlanetCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("any", "any", "Any Planet", priority: 10));
            foreach (var planet in BalatroData.PlanetCards.OrderBy(p => p.Value))
            {
                if (planet.Key != "any")
                {
                    completions.Add(
                        new SmartCompletionData(planet.Key, planet.Key, planet.Value, priority: 5)
                    );
                }
            }
        }

        private static void AddVoucherCompletions(List<ICompletionData> completions)
        {
            foreach (var voucher in BalatroData.Vouchers.OrderBy(v => v.Value))
            {
                completions.Add(
                    new SmartCompletionData(voucher.Key, voucher.Key, voucher.Value, priority: 5)
                );
            }
        }

        private enum YamlCompletionContext
        {
            TopLevelProperty,
            ClauseProperty,
            DeckValue,
            StakeValue,
            TypeValue,
            JokerValue,
            TarotValue,
            SpectralValue,
            PlanetValue,
            VoucherValue,
            EditionValue,
            SealValue,
            EnhancementValue,
            RankValue,
            SuitValue,
            AntesArray,
            SlotsArray,
            AnchorDefinition,
            AnchorReference,
        }
    }
}
