using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Controls
{
    public partial class TagEditor : UserControl
    {
        private WrapPanel? _tagContainer;
        private AutoCompleteBox? _tagInput;

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

        private readonly ObservableCollection<string> _currentTags =
            new ObservableCollection<string>();

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

            _tagContainer = this.FindControl<WrapPanel>("TagContainer");
            _tagInput = this.FindControl<AutoCompleteBox>("TagInput");

            if (_tagInput != null)
            {
                _tagInput.ItemsSource = _availableTags.OrderBy(t => t);
            }

            // Watch for tag changes
            _currentTags.CollectionChanged += (s, e) =>
            {
                Tags = _currentTags.ToList();
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
            if (_tagContainer != null)
            {
                var toRemove = _tagContainer.Children.Where(c => c != _tagInput).ToList();
                foreach (var child in toRemove)
                {
                    _tagContainer.Children.Remove(child);
                }
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
            if (_tagContainer == null || _tagInput == null)
                return;

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
            var inputIndex = _tagContainer.Children.IndexOf(_tagInput);
            _tagContainer.Children.Insert(inputIndex, chip);
        }

        private void OnRemoveTag(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                _currentTags.Remove(tag);

                // Remove the chip
                if (button.Parent?.Parent is Border chip && _tagContainer != null)
                {
                    _tagContainer.Children.Remove(chip);
                }
            }
        }

        private void OnTagInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (_tagInput == null || string.IsNullOrWhiteSpace(_tagInput.Text))
                return;

            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                var newTag = _tagInput.Text.Trim();

                // Ensure tag starts with #
                if (!newTag.StartsWith("#"))
                {
                    newTag = "#" + newTag;
                }

                // Check if tag already exists
                if (!_currentTags.Contains(newTag))
                {
                    // Add to available tags for future autocomplete
                    _availableTags.Add(newTag);
                    if (_tagInput.ItemsSource is IEnumerable<string>)
                    {
                        _tagInput.ItemsSource = _availableTags.OrderBy(t => t);
                    }

                    // Add the tag
                    _currentTags.Add(newTag);
                    AddTagChip(newTag);

                    // Clear input
                    _tagInput.Text = "";
                    _tagInput.SelectedItem = null;
                }
                else
                {
                    // Tag already exists, just clear
                    _tagInput.Text = "";
                }

                e.Handled = true;
            }
        }

        private void OnDropDownOpened(object? sender, EventArgs e)
        {
            // Ensure dropdown shows even with empty text
            if (_tagInput != null && string.IsNullOrEmpty(_tagInput.Text))
            {
                _tagInput.ItemsSource = _availableTags.OrderBy(t => t);
            }
        }

        private void OnTagSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (_tagInput?.SelectedItem is string selectedTag && !string.IsNullOrEmpty(selectedTag))
            {
                if (!_currentTags.Contains(selectedTag))
                {
                    _currentTags.Add(selectedTag);
                    AddTagChip(selectedTag);
                }

                // Clear input
                _tagInput.Text = "";
                _tagInput.SelectedItem = null;
            }
        }
    }
}
