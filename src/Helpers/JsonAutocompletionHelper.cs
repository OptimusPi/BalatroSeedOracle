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
                    AddJokerCompletions(completions, textBeforeCursor);
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

            // Check if we're after "value": or "values":
            if (Regex.IsMatch(textBefore, @"""values?""\s*:\s*(\[|\s*"")?$"))
                return CompletionContext.ValueField;

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

            // Default: we're typing a property name
            return CompletionContext.PropertyName;
        }

        private static void AddPropertyNameCompletions(List<ICompletionData> completions)
        {
            // Core filter properties
            completions.Add(new SmartCompletionData("type", "\"type\": \"Joker\"", "Item type (Joker, TarotCard, SpectralCard, etc.)", priority: 10));
            completions.Add(new SmartCompletionData("value", "\"value\": \"Blueprint\"", "Single item value", priority: 9));
            completions.Add(new SmartCompletionData("values", "\"values\": [\"Blueprint\", \"Brainstorm\"]", "Multiple item values (OR)", priority: 9));
            completions.Add(new SmartCompletionData("min", "\"min\": 2", "Minimum occurrences required (NEW!)", priority: 8));
            completions.Add(new SmartCompletionData("edition", "\"edition\": \"Negative\"", "Edition requirement (Negative, Polychrome, etc.)", priority: 7));
            completions.Add(new SmartCompletionData("antes", "\"antes\": [1, 2, 3, 4, 5, 6, 7, 8]", "Which antes to check", priority: 7));

            // Advanced properties
            completions.Add(new SmartCompletionData("minShopSlot", "\"minShopSlot\": 0", "Minimum shop slot (inclusive)", priority: 5));
            completions.Add(new SmartCompletionData("maxShopSlot", "\"maxShopSlot\": 10", "Maximum shop slot (exclusive)", priority: 5));
            completions.Add(new SmartCompletionData("minPackSlot", "\"minPackSlot\": 0", "Minimum pack slot (inclusive)", priority: 5));
            completions.Add(new SmartCompletionData("maxPackSlot", "\"maxPackSlot\": 6", "Maximum pack slot (exclusive)", priority: 5));

            // Filter structure
            completions.Add(new SmartCompletionData("must", "\"must\": []", "ALL conditions must be true (AND)", priority: 10));
            completions.Add(new SmartCompletionData("should", "\"should\": []", "At least ONE condition must be true (OR)", priority: 10));
            completions.Add(new SmartCompletionData("mustNot", "\"mustNot\": []", "NONE of these conditions (NOT)", priority: 10));

            // Top-level properties
            completions.Add(new SmartCompletionData("deck", "\"deck\": \"Red\"", "Starting deck", priority: 6));
            completions.Add(new SmartCompletionData("stake", "\"stake\": \"White\"", "Difficulty stake", priority: 6));
            completions.Add(new SmartCompletionData("name", "\"name\": \"MyFilter\"", "Filter name", priority: 6));
        }

        private static void AddTypeValueCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("Joker", "Joker", "Regular jokers", priority: 10));
            completions.Add(new SmartCompletionData("SoulJoker", "SoulJoker", "Soul jokers (from spectral packs)", priority: 10));
            completions.Add(new SmartCompletionData("TarotCard", "TarotCard", "Tarot cards", priority: 9));
            completions.Add(new SmartCompletionData("SpectralCard", "SpectralCard", "Spectral cards", priority: 9));
            completions.Add(new SmartCompletionData("PlanetCard", "PlanetCard", "Planet cards", priority: 9));
            completions.Add(new SmartCompletionData("Voucher", "Voucher", "Vouchers", priority: 8));
            completions.Add(new SmartCompletionData("StandardCard", "StandardCard", "Playing cards", priority: 7));
            completions.Add(new SmartCompletionData("Boss", "Boss", "Boss blinds", priority: 7));
            completions.Add(new SmartCompletionData("SmallBlindTag", "SmallBlindTag", "Small blind tags", priority: 6));
            completions.Add(new SmartCompletionData("BigBlindTag", "BigBlindTag", "Big blind tags", priority: 6));
        }

        private static void AddJokerCompletions(List<ICompletionData> completions, string textBefore)
        {
            // Add wildcards first
            completions.Add(new SmartCompletionData("anylegendary", "anylegendary", "Any legendary joker", priority: 10));
            completions.Add(new SmartCompletionData("anyrare", "anyrare", "Any rare joker", priority: 10));
            completions.Add(new SmartCompletionData("anyuncommon", "anyuncommon", "Any uncommon joker", priority: 10));
            completions.Add(new SmartCompletionData("anycommon", "anycommon", "Any common joker", priority: 10));
            completions.Add(new SmartCompletionData("anyjoker", "anyjoker", "Any joker at all", priority: 10));

            // Add all jokers from BalatroData
            foreach (var joker in BalatroData.Jokers.OrderBy(j => j.Value))
            {
                if (!joker.Key.StartsWith("any")) // Skip wildcards (already added)
                {
                    completions.Add(new SmartCompletionData(
                        joker.Key,
                        joker.Key,
                        joker.Value,
                        priority: 5
                    ));
                }
            }
        }

        private static void AddEditionCompletions(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("Negative", "Negative", "+1 joker slot", priority: 10));
            completions.Add(new SmartCompletionData("Polychrome", "Polychrome", "x1.5 mult", priority: 9));
            completions.Add(new SmartCompletionData("Holographic", "Holographic", "+10 mult", priority: 8));
            completions.Add(new SmartCompletionData("Foil", "Foil", "+50 chips", priority: 7));
            completions.Add(new SmartCompletionData("None", "None", "No edition", priority: 6));
        }

        private static void AddDeckCompletions(List<ICompletionData> completions)
        {
            foreach (var deck in BalatroData.Decks.OrderBy(d => d.Value))
            {
                completions.Add(new SmartCompletionData(deck.Key, deck.Key, deck.Value, priority: 5));
            }
        }

        private static void AddStakeCompletions(List<ICompletionData> completions)
        {
            foreach (var stake in BalatroData.Stakes.OrderBy(s => s.Value))
            {
                completions.Add(new SmartCompletionData(stake.Key, stake.Key, stake.Value, priority: 5));
            }
        }

        private static void AddAnteSnippets(List<ICompletionData> completions)
        {
            completions.Add(new SmartCompletionData("1-8", "1, 2, 3, 4, 5, 6, 7, 8", "All antes", priority: 10));
            completions.Add(new SmartCompletionData("1-4", "1, 2, 3, 4", "Early game", priority: 9));
            completions.Add(new SmartCompletionData("5-8", "5, 6, 7, 8", "Late game", priority: 9));
            completions.Add(new SmartCompletionData("8", "8", "Final ante only", priority: 8));
        }

        private enum CompletionContext
        {
            PropertyName,
            TypeValue,
            ValueField,
            EditionField,
            DeckField,
            StakeField,
            AntesArray
        }
    }

    /// <summary>
    /// Smart completion item with description and priority
    /// </summary>
    public class SmartCompletionData : ICompletionData
    {
        public SmartCompletionData(string text, string content, string description, double priority = 1.0)
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

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Content.ToString());
        }
    }
}
