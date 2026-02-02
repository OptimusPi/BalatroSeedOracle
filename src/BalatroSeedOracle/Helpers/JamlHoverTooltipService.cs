using System;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Service for showing hover tooltips in JAML editor
    /// </summary>
    public class JamlHoverTooltipService
    {
        private readonly TextEditor _editor;
        private Popup? _tooltipPopup;
        private TextBlock? _tooltipContent;

        public JamlHoverTooltipService(TextEditor editor)
        {
            _editor = editor;
        }

        public void Install()
        {
            if (_editor.TextArea?.TextView == null)
                return;

            // Use PointerMoved for hover detection
            _editor.TextArea.TextView.PointerMoved += OnPointerMoved;
            _editor.TextArea.TextView.PointerExited += OnPointerExited;
        }

        private void OnPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (_editor.TextArea?.Caret == null || _editor.Document == null)
                return;

            // Use caret position for hover (simpler and more reliable)
            var offset = _editor.CaretOffset;
            var word = GetWordAtOffset(offset);

            if (string.IsNullOrEmpty(word))
            {
                HideTooltip();
                return;
            }

            var tooltipText = GetTooltipForWord(word, offset);
            if (!string.IsNullOrEmpty(tooltipText))
            {
                var location = _editor.Document.GetLocation(offset);
                ShowTooltip(tooltipText, new TextViewPosition(location));
            }
        }

        private void OnPointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            HideTooltip();
        }

        private string GetWordAtOffset(int offset)
        {
            if (_editor.Document == null || offset < 0 || offset >= _editor.Document.TextLength)
                return "";

            var line = _editor.Document.GetLineByOffset(offset);
            var lineText = _editor.Document.GetText(line.Offset, line.Length);
            var column = offset - line.Offset;

            // Find word boundaries
            int start = column;
            int end = column;

            while (
                start > 0
                && (
                    char.IsLetterOrDigit(lineText[start - 1])
                    || lineText[start - 1] == '_'
                    || lineText[start - 1] == '*'
                )
            )
                start--;

            while (
                end < lineText.Length
                && (char.IsLetterOrDigit(lineText[end]) || lineText[end] == '_')
            )
                end++;

            if (start < end)
                return lineText.Substring(start, end - start);

            return "";
        }

        private string GetTooltipForWord(string word, int offset)
        {
            if (string.IsNullOrEmpty(word))
                return "";

            // Check if it's an anchor reference
            if (word.StartsWith("*"))
            {
                var anchorName = word.Substring(1);
                return GetAnchorTooltip(anchorName);
            }

            // Check if it's a joker name
            if (BalatroData.Jokers.ContainsKey(word))
            {
                var jokerName = BalatroData.Jokers[word];
                var rarity = GetJokerRarity(word);
                return $"**{jokerName}**\n\nRarity: {rarity}\n\nJoker card in Balatro";
            }

            // Check if it's a property name
            var propertyTooltip = GetPropertyTooltip(word);
            if (!string.IsNullOrEmpty(propertyTooltip))
                return propertyTooltip;

            // Check context around the word
            var context = GetContextAroundOffset(offset);
            if (context.Contains("joker:") && BalatroData.Jokers.ContainsKey(word))
            {
                return GetJokerTooltip(word);
            }

            return "";
        }

        private string GetAnchorTooltip(string anchorName)
        {
            if (_editor.Document == null)
                return "";

            var text = _editor.Document.Text;
            var pattern = $@"(\w+)\s*:\s*&{Regex.Escape(anchorName)}\s+(.+)";
            var match = Regex.Match(text, pattern, RegexOptions.Multiline);

            if (match.Success)
            {
                var value = match.Groups[2].Value.Trim();
                return $"**Anchor: {anchorName}**\n\nDefined as: `{value}`\n\nClick to go to definition";
            }

            return $"**Anchor: {anchorName}**\n\n⚠️ Anchor not found";
        }

        private string GetJokerTooltip(string jokerKey)
        {
            if (!BalatroData.Jokers.ContainsKey(jokerKey))
                return "";

            var jokerName = BalatroData.Jokers[jokerKey];
            var rarity = GetJokerRarity(jokerKey);
            return $"**{jokerName}**\n\nRarity: {rarity}\n\nType: Joker";
        }

        private string GetJokerRarity(string jokerKey)
        {
            // Simple rarity detection based on joker key patterns
            if (
                jokerKey.Contains("Legendary")
                || jokerKey == "Perkeo"
                || jokerKey == "Triboulet"
                || jokerKey == "Canio"
                || jokerKey == "Chicot"
            )
                return "Legendary";
            if (jokerKey.Contains("Rare"))
                return "Rare";
            if (jokerKey.Contains("Uncommon"))
                return "Uncommon";
            return "Common";
        }

        private string GetPropertyTooltip(string property)
        {
            return property.ToLower() switch
            {
                "joker" => "**joker**\n\nSpecifies a joker card name",
                "antes" =>
                    "**antes**\n\nArray of antes (1-8) to check. Children inherit from parent And/Or clauses.",
                "score" => "**score**\n\nScoring weight for 'should' clauses",
                "shopslots" => "**ShopSlots**\n\nArray of shop slot positions (0-5)",
                "packslots" => "**PackSlots**\n\nArray of pack slot positions (0-5)",
                "edition" =>
                    "**edition**\n\nRequired edition: Foil, Holographic, Polychrome, or Negative",
                "mode" => "**Mode**\n\nAnd/Or clause mode: Max or Sum",
                "clauses" => "**clauses**\n\nArray of child clauses for And/Or operators",
                "and" => "**And**\n\nAND clause - all child clauses must pass",
                "or" => "**Or**\n\nOR clause - any child clause can pass",
                "smallblindtag" =>
                    "**smallblindtag**\n\nSmall blind tag requirement (e.g., NegativeTag)",
                "bigblindtag" => "**bigblindtag**\n\nBig blind tag requirement",
                _ => "",
            };
        }

        private string GetContextAroundOffset(int offset)
        {
            if (_editor.Document == null)
                return "";

            var line = _editor.Document.GetLineByOffset(offset);
            var start = Math.Max(0, line.Offset - 50);
            var length = Math.Min(100, _editor.Document.TextLength - start);
            return _editor.Document.GetText(start, length);
        }

        private void ShowTooltip(string text, TextViewPosition? position)
        {
            if (_editor.TextArea?.TextView == null)
                return;

            if (_tooltipPopup == null)
            {
                _tooltipPopup = new Popup
                {
                    Placement = PlacementMode.Pointer,
                    PlacementTarget = _editor,
                    IsLightDismissEnabled = true,
                    Child = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(8),
                        Child = _tooltipContent =
                            new TextBlock
                            {
                                Foreground = Brushes.White,
                                FontSize = 12,
                                TextWrapping = TextWrapping.Wrap,
                                MaxWidth = 300,
                            },
                    },
                };
            }

            if (_tooltipContent != null)
            {
                // Simple markdown-like formatting
                var formattedText = text.Replace("**", "").Replace("\n\n", "\n");
                _tooltipContent.Text = formattedText;
            }

            _tooltipPopup.IsOpen = true;
        }

        private void HideTooltip()
        {
            if (_tooltipPopup != null)
            {
                _tooltipPopup.IsOpen = false;
            }
        }
    }
}
