using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for BalatroTabControl - handles tab switching with triangle indicator
    /// REUSABLE: Works for any modal that needs Balatro-style tabs
    /// </summary>
    public partial class BalatroTabControlViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<BalatroTabItem> _tabs = new();

        [ObservableProperty]
        private int _activeTabIndex = 0;

        [ObservableProperty]
        private double _triangleOffset = 0;

        /// <summary>
        /// Switches to the specified tab and updates triangle position
        /// </summary>
        [RelayCommand]
        private void SwitchTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= Tabs.Count) return;

            // Update all tab states
            for (int i = 0; i < Tabs.Count; i++)
            {
                Tabs[i].IsActive = (i == tabIndex);
            }

            ActiveTabIndex = tabIndex;

            // Triangle auto-centers on active button (built into template!)
        }

        /// <summary>
        /// Enables/disables a specific tab
        /// </summary>
        public void SetTabEnabled(int tabIndex, bool enabled)
        {
            if (tabIndex >= 0 && tabIndex < Tabs.Count)
            {
                Tabs[tabIndex].IsEnabled = enabled;
            }
        }
    }

    /// <summary>
    /// Represents a single tab item for BalatroTabControl
    /// </summary>
    public partial class BalatroTabItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = "";

        [ObservableProperty]
        private bool _isActive = false;

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private int _index = 0;
    }
}
