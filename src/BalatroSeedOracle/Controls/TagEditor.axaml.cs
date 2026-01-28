using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// Tag editor control with autocomplete.
    /// Uses StyledProperty for external binding, direct x:Name field access (no FindControl).
    /// </summary>
    public partial class TagEditor : UserControl
    {
        // Predefined tags for autocomplete
        private readonly HashSet<string> _availableTags = new HashSet<string>
        {
            "#WeeJoker",
            "#Chips",
            "#Mult",
            "#XMult",
            "#Faceless",
            "#Legendary",
            "#Spectral",
            "#Voucher",
            "#Boss",
            "#Tarot",
            "#Money",
            "#Gold",
            "#EarlyGame",
            "#LateGame",
            "#Ante8",
            "#HandSize",
            "#Discards",
            "#Rerolls",
            "#Negative",
            "#Foil",
            "#Holographic",
            "#Polychrome",
            "#Blueprint",
            "#Brainstorm",
            "#Soul",
            "#Ankh",
            "#Wraith",
            "#Immolate",
            "#Cryptid",
        };

        private readonly ObservableCollection<string> _currentTags = new();

        public static readonly StyledProperty<List<string>> TagsProperty =
            AvaloniaProperty.Register<TagEditor, List<string>>(nameof(Tags), new List<string>());

        public List<string> Tags
        {
            get => GetValue(TagsProperty);
            set => SetValue(TagsProperty, value);
        }

        public TagEditor()
        {
            InitializeComponent();

            // Direct x:Name field access - no FindControl!
            TagInput.ItemsSource = _availableTags.OrderBy(t => t);

            // Watch for tag changes
            _currentTags.CollectionChanged += (s, e) =>
            {
                Tags = _currentTags.ToList();
            };
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TagsProperty && change.NewValue is List<string> newTags)
            {
                LoadTags(newTags);
            }
        }

        private void LoadTags(List<string> tags)
        {
            _currentTags.Clear();

            // Clear existing tag chips except the input
            var toRemove = TagContainer.Children.Where(c => c != TagInput).ToList();
            foreach (var child in toRemove)
            {
                TagContainer.Children.Remove(child);
            }

            // Add tag chips
            foreach (var tag in tags)
            {
                _currentTags.Add(tag);
                AddTagChip(tag);
            }
        }

        private void AddTagChip(string tag)
        {
            // Create tag chip
            var chip = new Border { Classes = { "tag-chip" } };
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var tagText = new TextBlock { Text = tag, Classes = { "tag-text" } };
            var removeButton = new Button
            {
                Content = "×",
                Classes = { "tag-remove" },
                Tag = tag,
            };

            removeButton.Click += OnRemoveTag;

            panel.Children.Add(tagText);
            panel.Children.Add(removeButton);
            chip.Child = panel;

            // Insert before the input box
            var inputIndex = TagContainer.Children.IndexOf(TagInput);
            TagContainer.Children.Insert(inputIndex, chip);
        }

        private void OnRemoveTag(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                _currentTags.Remove(tag);

                // Remove the chip
                if (button.Parent?.Parent is Border chip)
                {
                    TagContainer.Children.Remove(chip);
                }
            }
        }

        private void OnTagInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TagInput.Text))
                return;

            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                var newTag = TagInput.Text.Trim();

                // Ensure tag starts with #
                if (!newTag.StartsWith("#"))
                {
                    newTag = "#" + newTag;
                }

                // Check if tag already exists
                if (!_currentTags.Contains(newTag))
                {
                    _availableTags.Add(newTag);
                    TagInput.ItemsSource = _availableTags.OrderBy(t => t);

                    // Add the tag
                    _currentTags.Add(newTag);
                    AddTagChip(newTag);

                    // Clear input
                    TagInput.Text = "";
                    TagInput.SelectedItem = null;
                }
                else
                {
                    TagInput.Text = "";
                }

                e.Handled = true;
            }
        }

        private void OnDropDownOpened(object? sender, EventArgs e)
        {
            // Ensure dropdown shows even with empty text
            if (string.IsNullOrEmpty(TagInput.Text))
            {
                TagInput.ItemsSource = _availableTags.OrderBy(t => t);
            }
        }

        private void OnTagSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (TagInput.SelectedItem is string selectedTag && !string.IsNullOrEmpty(selectedTag))
            {
                if (!_currentTags.Contains(selectedTag))
                {
                    _currentTags.Add(selectedTag);
                    AddTagChip(selectedTag);
                }

                TagInput.Text = "";
                TagInput.SelectedItem = null;
            }
        }
    }
}
