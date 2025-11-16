using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// SMART JSON autocomplete - context-aware, schema-driven, with ALL Balatro data
    /// </summary>
    public static class JsonAutocompletionHelper
    {
        public static List<ICompletionData> GetCompletionsForContext(string textBeforeCursor)
        {
            var completions = new List<ICompletionData>();

            // Determine context - what property are we in?
            var context = DetermineContext(textBeforeCursor);

            switch (context)
            {
                case CompletionContext.PropertyName:
                    AddPropertyNameCompletions(completions);
                    break;

                case CompletionContext.TypeValue:
                    AddTypeValueCompletions(completions);
                    break;

                case CompletionContext.ValueField:
                case CompletionContext.JokerValue:
                    AddJokerCompletions(completions, textBeforeCursor);
                    break;

                case CompletionContext.TarotValue:
                    AddTarotCompletions(completions);
                    break;

                case CompletionContext.SpectralValue:
                    AddSpectralCompletions(completions);
                    break;

                case CompletionContext.PlanetValue:
                    AddPlanetCompletions(completions);
                    break;

                case CompletionContext.VoucherValue:
                    AddVoucherCompletions(completions);
                    break;

                case CompletionContext.EditionField:
                    AddEditionCompletions(completions);
                    break;

                case CompletionContext.DeckField:
                    AddDeckCompletions(completions);
                    break;

                case CompletionContext.StakeField:
                    AddStakeCompletions(completions);
                    break;

                case CompletionContext.AntesArray:
                    AddAnteSnippets(completions);
                    break;

                case CompletionContext.SlotArray:
                    AddSlotSnippets(completions);
                    break;

                default:
                    // Fallback: show all common properties
                    AddPropertyNameCompletions(completions);
                    break;
            }

            return completions;
        }

        private static CompletionContext DetermineContext(string textBefore)
        {
            // Check if we're after "type":
            if (Regex.IsMatch(textBefore, @"""type""\s*:\s*""?$"))
                return CompletionContext.TypeValue;

            // Check what type was specified to give context-aware value suggestions
            var typeMatch = Regex.Match(
                textBefore,
                @"""type""\s*:\s*""(Joker|TarotCard|SpectralCard|PlanetCard|Voucher|StandardCard|Boss)"""
            );

            // Check if we're after "value": or "values":
            if (Regex.IsMatch(textBefore, @"""values?""\s*:\s*(\[|\s*"")?$"))
            {
                if (typeMatch.Success)
                {
                    return typeMatch.Groups[1].Value switch
                    {
                        "Joker" => CompletionContext.JokerValue,
                        "TarotCard" => CompletionContext.TarotValue,
                        "SpectralCard" => CompletionContext.SpectralValue,
                        "PlanetCard" => CompletionContext.PlanetValue,
                        "Voucher" => CompletionContext.VoucherValue,
                        _ => CompletionContext.ValueField,
                    };
                }
                return CompletionContext.ValueField; // Generic fallback
            }

            // Check if we're after "edition":
            if (Regex.IsMatch(textBefore, @"""edition""\s*:\s*""?$"))
                return CompletionContext.EditionField;

            // Check if we're after "deck":
            if (Regex.IsMatch(textBefore, @"""deck""\s*:\s*""?$"))
                return CompletionContext.DeckField;

            // Check if we're after "stake":
            if (Regex.IsMatch(textBefore, @"""stake""\s*:\s*""?$"))
                return CompletionContext.StakeField;

            // Check if we're in "antes" array
            if (Regex.IsMatch(textBefore, @"""antes""\s*:\s*\["))
                return CompletionContext.AntesArray;

            // Check if we're in "shopSlots" or "packSlots" array
            if (Regex.IsMatch(textBefore, @"""(shop|pack)Slots""\s*:\s*\["))
                return CompletionContext.SlotArray;

            // Default: we're typing a property name
            return CompletionContext.PropertyName;
        }

        private static void AddPropertyNameCompletions(List<ICompletionData> completions)
        {
            // SNIPPETS - Complete filter condition objects
            completions.Add(
                new SmartCompletionData(
                    "snippet-joker",
                    "{\n  \"type\": \"Joker\",\n  \"value\": \"Blueprint\",\n  \"antes\": [1, 2],\n  \"score\": 1\n}",
                    "Complete joker condition",
                    priority: 15
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "snippet-multi",
                    "{\n  \"type\": \"Joker\",\n  \"values\": [\"Blueprint\", \"Brainstorm\"],\n  \"antes\": [1, 2],\n  \"score\": 1\n}",
                    "Multiple values (OR)",
                    priority: 15
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "snippet-edition",
                    "{\n  \"type\": \"Joker\",\n  \"value\": \"Blueprint\",\n  \"edition\": \"Negative\",\n  \"antes\": [1, 2],\n  \"score\": 10\n}",
                    "Edition requirement",
                    priority: 15
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "snippet-sources",
                    "\"sources\": {\n  \"shopSlots\": [0, 1, 2, 3, 4, 5],\n  \"packSlots\": [0, 1, 2, 3]\n}",
                    "Source slots",
                    priority: 14
                )
            );

            // Core filter properties
            completions.Add(
                new SmartCompletionData("type", "\"type\": \"Joker\"", "Item type", priority: 10)
            );
            completions.Add(
                new SmartCompletionData(
                    "value",
                    "\"value\": \"Blueprint\"",
                    "Single item value",
                    priority: 9
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "values",
                    "\"values\": [\"Blueprint\", \"Brainstorm\"]",
                    "Multiple values (OR)",
                    priority: 9
                )
            );
            completions.Add(
                new SmartCompletionData("min", "\"min\": 2", "Minimum occurrences", priority: 8)
            );
            completions.Add(
                new SmartCompletionData("score", "\"score\": 1", "Priority score", priority: 8)
            );
            completions.Add(
                new SmartCompletionData(
                    "edition",
                    "\"edition\": \"Negative\"",
                    "Edition requirement",
                    priority: 7
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "antes",
                    "\"antes\": [1, 2, 3, 4, 5, 6, 7, 8]",
                    "Antes to check",
                    priority: 7
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "sources",
                    "\"sources\": {\n  \"shopSlots\": [0, 1, 2, 3, 4, 5],\n  \"packSlots\": [0, 1, 2, 3],\n  \"tags\": true\n}",
                    "Source restrictions",
                    priority: 7
                )
            );

            // Advanced slot filtering
            completions.Add(
                new SmartCompletionData(
                    "shopSlots",
                    "\"shopSlots\": [0, 1, 2, 3, 4, 5]",
                    "Shop slot positions",
                    priority: 6
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "packSlots",
                    "\"packSlots\": [0, 1, 2, 3]",
                    "Pack slot positions",
                    priority: 6
                )
            );
            completions.Add(
                new SmartCompletionData("tags", "\"tags\": true", "Include tags", priority: 6)
            );
            completions.Add(
                new SmartCompletionData(
                    "minShopSlot",
                    "\"minShopSlot\": 0",
                    "Min shop slot",
                    priority: 5
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "maxShopSlot",
                    "\"maxShopSlot\": 10",
                    "Max shop slot",
                    priority: 5
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "minPackSlot",
                    "\"minPackSlot\": 0",
                    "Min pack slot",
                    priority: 5
                )
            );
            completions.Add(
                new SmartCompletionData(
                    "maxPackSlot",
                    "\"maxPackSlot\": 6",
                    "Max pack slot",
                    priority: 5
                )
            );

            // Filter structure
            completions.Add(
                new SmartCompletionData("must", "\"must\": []", "AND logic", priority: 10)
            );
            completions.Add(
                new SmartCompletionData("should", "\"should\": []", "OR logic", priority: 10)
            );
            completions.Add(
                new SmartCompletionData("mustNot", "\"mustNot\": []", "NOT logic", priority: 10)
            );

            // Top-level metadata
            completions.Add(
                new SmartCompletionData("deck", "\"deck\": \"Red\"", "Starting deck", priority: 6)
            );
            completions.Add(
                new SmartCompletionData("stake", "\"stake\": \"White\"", "Stake", priority: 6)
            );
            completions.Add(
                new SmartCompletionData(
                    "name",
                    "\"name\": \"MyFilter\"",
                    "Filter name",
                    priority: 6
                )
            );
            completions.Add(
                new SmartCompletionData("author", "\"author\": \"pifreak\"", "Author", priority: 5)
            );
            completions.Add(
                new SmartCompletionData(
                    "description",
                    "\"description\": \"My custom filter\"",
                    "Description",
                    priority: 5
                )
            );
        }

        private static void AddTypeValueCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("Joker", "Joker", "Jokers", priority: 10));
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
                new SmartCompletionData("SoulJoker", "SoulJoker", "Soul jokers", priority: 8)
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
            completions.Add(
                new SmartCompletionData(
                    "SmallBlindTag",
                    "SmallBlindTag",
                    "Small blind tags",
                    priority: 6
                )
            );
            completions.Add(
                new SmartCompletionData("BigBlindTag", "BigBlindTag", "Big blind tags", priority: 6)
            );
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
                { "RideTheBus", "Ride The Bus" },
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
            completions.Add(new SmartCompletionData("None", "None", "None", priority: 6));
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
                new SmartCompletionData("1-8", "1, 2, 3, 4, 5, 6, 7, 8", "All antes", priority: 10)
            );
            completions.Add(
                new SmartCompletionData("1-4", "1, 2, 3, 4", "Early game", priority: 9)
            );
            completions.Add(new SmartCompletionData("5-8", "5, 6, 7, 8", "Late game", priority: 9));
            completions.Add(new SmartCompletionData("1-2", "1, 2", "Antes 1-2", priority: 9));
            completions.Add(new SmartCompletionData("1", "1", "Ante 1", priority: 8));
            completions.Add(new SmartCompletionData("8", "8", "Ante 8", priority: 8));
        }

        private static void AddSlotSnippets(List<ICompletionData> completions)
        {
            completions.Add(
                new SmartCompletionData(
                    "all-shop",
                    "0, 1, 2, 3, 4, 5",
                    "All shop slots",
                    priority: 10
                )
            );
            completions.Add(
                new SmartCompletionData("all-pack", "0, 1, 2, 3", "All pack slots", priority: 10)
            );
            completions.Add(
                new SmartCompletionData("first-3", "0, 1, 2", "First 3 slots", priority: 9)
            );
            completions.Add(new SmartCompletionData("slot-0", "0", "First slot", priority: 8));
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

        private enum CompletionContext
        {
            PropertyName,
            TypeValue,
            ValueField,
            JokerValue,
            TarotValue,
            SpectralValue,
            PlanetValue,
            VoucherValue,
            EditionField,
            DeckField,
            StakeField,
            AntesArray,
            SlotArray,
        }
    }

    /// <summary>
    /// Smart completion item with description and priority
    /// </summary>
    public class SmartCompletionData : ICompletionData
    {
        public SmartCompletionData(
            string text,
            string content,
            string description,
            double priority = 1.0
        )
        {
            Text = text;
            Content = content;
            Description = description;
            Priority = priority;
        }

        public string Text { get; }
        public object Content { get; }
        public object Description { get; }
        public double Priority { get; }
        public Avalonia.Media.IImage? Image => null;

        public void Complete(
            TextArea textArea,
            ISegment completionSegment,
            EventArgs insertionRequestEventArgs
        )
        {
            // Replace the completion segment (which includes what the user already typed)
            // with the full content
            textArea.Document.Replace(completionSegment, Content.ToString());
        }
    }
}
