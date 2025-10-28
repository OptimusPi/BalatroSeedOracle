using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Reusable Balatro-style tab control with bouncing triangle indicator
    /// Triangle is built into button template (global .tab-button style)
    /// </summary>
    public partial class BalatroTabControl : UserControl
    {
        public BalatroTabControlViewModel ViewModel { get; }

        public event EventHandler<int>? TabChanged;

        public BalatroTabControl()
        {
            ViewModel = new BalatroTabControlViewModel();
            DataContext = ViewModel;

            // Wire up ViewModel's TabChanged event to forward it to our consumers
            ViewModel.TabChanged += (s, tabIndex) => TabChanged?.Invoke(this, tabIndex);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Initialize tabs with titles
        /// </summary>
        public void SetTabs(params string[] tabTitles)
        {
            ViewModel.Tabs.Clear();
            for (int i = 0; i < tabTitles.Length; i++)
            {
                ViewModel.Tabs.Add(
                    new BalatroTabItem
                    {
                        Title = tabTitles[i],
                        Index = i,
                        IsActive = (i == 0),
                        IsEnabled = true,
                    }
                );
            }
        }

        /// <summary>
        /// Enable/disable specific tabs
        /// </summary>
        public void SetTabEnabled(int index, bool enabled)
        {
            ViewModel.SetTabEnabled(index, enabled);
        }

        /// <summary>
        /// Switch to a specific tab
        /// </summary>
        public void SwitchToTab(int index)
        {
            ViewModel.SwitchTabCommand.Execute(index);
            TabChanged?.Invoke(this, index);
        }
    }
}
