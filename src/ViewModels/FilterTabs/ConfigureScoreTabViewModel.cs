using System.Collections.ObjectModel;
using System.Windows.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    /// <summary>
    /// ViewModel for Configure Score tab - handles score columns with weights in a row-based UI.
    /// Replaces the SHOULD zone from Visual Builder with a more intuitive scoring interface.
    /// Refactored to use FilterTabViewModelBase - eliminates ~580 lines of duplicate code.
    /// </summary>
    public partial class ConfigureScoreTabViewModel : FilterTabViewModelBase
    {
        // Score row model
        public class ScoreRow : ObservableObject
        {
            private FilterItem _item = new();
            private int _weight = 10;

            public FilterItem Item
            {
                get => _item;
                set => SetProperty(ref _item, value);
            }

            public int Weight
            {
                get => _weight;
                set => SetProperty(ref _weight, value);
            }

            // For parent reference to handle removal
            public ConfigureScoreTabViewModel? Parent { get; set; }

            private ICommand? _removeCommand;
            public ICommand RemoveCommand => _removeCommand ??= new RelayCommand(Remove);

            private void Remove()
            {
                Parent?.RemoveScoreRow(this);
            }
        }

        // Score rows collection (replaces SHOULD zone)
        public ObservableCollection<ScoreRow> ScoreRows { get; } = new();

        // Minimum score filter properties
        [ObservableProperty]
        private string _minimumScore = "100000";

        [ObservableProperty]
        private bool _enableMinimumScore = false;

        public ConfigureScoreTabViewModel(
            FiltersModalViewModel? parentViewModel,
            IFilterItemDataService dataService,
            IFilterItemFilterService filterService)
            : base(parentViewModel, dataService, filterService)
        {
            // Subscribe to score rows changes for auto-save
            ScoreRows.CollectionChanged += (s, e) => TriggerAutoSave();
        }

        /// <summary>
        /// Add a new score row when an item is dropped
        /// </summary>
        public void AddScoreRow(FilterItem item, int weight = 10)
        {
            var scoreRow = new ScoreRow
            {
                Item = item,
                Weight = weight,
                Parent = this
            };

            ScoreRows.Add(scoreRow);

            // Sync with parent ViewModel
            if (_parentViewModel != null)
            {
                var itemKey = _parentViewModel.GenerateNextItemKey();
                var itemConfig = new ItemConfig
                {
                    ItemKey = itemKey,
                    ItemType = item.Type,
                    ItemName = item.Name,
                    Score = weight // Weight becomes Score in config
                };
                _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                _parentViewModel.SelectedShould.Add(itemKey); // Store in SHOULD zone (score columns)
            }

            DebugLogger.Log("ConfigureScoreTab", $"Added {item.Name} to score columns with weight {weight}");
            NotifyJsonEditorOfChanges();
        }

        /// <summary>
        /// Remove a score row
        /// </summary>
        public void RemoveScoreRow(ScoreRow row)
        {
            var index = ScoreRows.IndexOf(row);
            if (index < 0) return;

            ScoreRows.RemoveAt(index);

            // Sync with parent ViewModel
            if (_parentViewModel != null && index < _parentViewModel.SelectedShould.Count)
            {
                var itemKey = _parentViewModel.SelectedShould[index];
                _parentViewModel.SelectedShould.RemoveAt(index);
                _parentViewModel.ItemConfigs.Remove(itemKey);
            }

            DebugLogger.Log("ConfigureScoreTab", $"Removed {row.Item.Name} from score columns");
            NotifyJsonEditorOfChanges();
        }

        /// <summary>
        /// Update weight for a score row
        /// </summary>
        public void UpdateScoreWeight(ScoreRow row, int newWeight)
        {
            var index = ScoreRows.IndexOf(row);
            if (index < 0) return;

            row.Weight = newWeight;

            // Sync with parent ViewModel
            if (_parentViewModel != null && index < _parentViewModel.SelectedShould.Count)
            {
                var itemKey = _parentViewModel.SelectedShould[index];
                if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                {
                    itemConfig.Score = newWeight;
                }
            }

            DebugLogger.Log("ConfigureScoreTab", $"Updated {row.Item.Name} weight to {newWeight}");
            NotifyJsonEditorOfChanges();
        }

        private void NotifyJsonEditorOfChanges()
        {
            if (_parentViewModel?.JsonEditorTab is JsonEditorTabViewModel jsonEditorVm)
            {
                jsonEditorVm.AutoGenerateFromVisual();
            }
        }
    }
}
