using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Media;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class SearchModal : UserControl
    {
        public SearchModalViewModel ViewModel { get; }

        public event EventHandler? CloseRequested;

        public SearchModal()
        {
            var searchManager = ServiceHelper.GetRequiredService<SearchManager>();
            ViewModel = new SearchModalViewModel(searchManager);
            DataContext = ViewModel;

            ViewModel.CloseRequested += (s, e) => CloseRequested?.Invoke(this, e);

            InitializeComponent();
            WireUpComponentEvents();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            ViewModel?.Dispose();
        }

        /// <summary>
        /// Minimal adapter: wire component events to ViewModel commands
        /// This is proper MVVM - components communicate via events, we forward to ViewModel
        /// </summary>
        private void WireUpComponentEvents()
        {
            // FilterSelectorControl → ViewModel.LoadFilterCommand
            var filterSelector = this.FindControl<Components.FilterSelectorControl>("FilterSelector");
            if (filterSelector != null)
            {
                filterSelector.FilterSelected += (s, path) =>
                {
                    ViewModel.LoadFilterCommand.Execute(path);
                    // STAY on Select Filter tab - don't auto-switch
                    // User can manually click Settings tab when ready
                };
            }

            // DeckAndStakeSelector → ViewModel properties
            var deckStakeSelector = this.FindControl<Components.DeckAndStakeSelector>("DeckStakeSelector");
            if (deckStakeSelector != null)
            {
                deckStakeSelector.SelectionChanged += (s, selection) =>
                {
                    var deckNames = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                                           "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };

                    if (selection.deckIndex >= 0 && selection.deckIndex < deckNames.Length)
                        ViewModel.DeckSelection = deckNames[selection.deckIndex];

                    ViewModel.StakeSelection = selection.stakeIndex.ToString();
                };
            }
        }

        /// <summary>
        /// Tab click handler - PROPER MVVM: Updates ViewModel instead of directly manipulating UI
        /// </summary>
        private void OnTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedButton) return;

            // Find all tab buttons
            var selectFilterTab = this.FindControl<Button>("SelectFilterTab");
            var settingsTab = this.FindControl<Button>("SettingsTab");
            var searchTab = this.FindControl<Button>("SearchTab");
            var resultsTab = this.FindControl<Button>("ResultsTab");

            // Remove 'active' class from all tabs
            selectFilterTab?.Classes.Remove("active");
            settingsTab?.Classes.Remove("active");
            searchTab?.Classes.Remove("active");
            resultsTab?.Classes.Remove("active");

            // Determine which tab was clicked and update ViewModel
            int tabIndex = 0;
            if (clickedButton.Name == "SelectFilterTab")
            {
                clickedButton.Classes.Add("active");
                tabIndex = 0;
            }
            else if (clickedButton.Name == "SettingsTab")
            {
                clickedButton.Classes.Add("active");
                tabIndex = 1;
            }
            else if (clickedButton.Name == "SearchTab")
            {
                clickedButton.Classes.Add("active");
                tabIndex = 2;
            }
            else if (clickedButton.Name == "ResultsTab")
            {
                clickedButton.Classes.Add("active");
                tabIndex = 3;
            }

            // PROPER MVVM: Let ViewModel control visibility
            ViewModel.UpdateTabVisibility(tabIndex);
            UpdateTrianglePosition(tabIndex);
        }

        /// <summary>
        /// Update bouncing triangle position to be under the active tab
        /// </summary>
        private void UpdateTrianglePosition(int tabIndex)
        {
            var triangleContainer = this.FindControl<Grid>("TriangleContainer");
            if (triangleContainer != null)
            {
                // Move triangle to the active tab's column
                Grid.SetColumn(triangleContainer, tabIndex);
            }
        }
    }
}
