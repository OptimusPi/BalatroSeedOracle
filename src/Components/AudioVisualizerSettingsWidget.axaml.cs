using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// AudioVisualizerSettingsWidget - A movable, minimizable widget for audio visualizer settings
    /// MVVM pattern - ALL business logic in ViewModel, drag handled by DraggableWidgetBehavior
    /// </summary>
    public partial class AudioVisualizerSettingsWidget : UserControl
    {
        public AudioVisualizerSettingsWidgetViewModel ViewModel { get; }

        public AudioVisualizerSettingsWidget()
        {
            InitializeComponent();

            // Initialize ViewModel (creates it lazily - only when widget is actually used)
            ViewModel = new AudioVisualizerSettingsWidgetViewModel();
            DataContext = ViewModel;

            // Update ZIndex when IsMinimized changes
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.IsMinimized))
                {
                    // Set ZIndex on this UserControl itself
                    // Expanded = 100, Minimized = 1
                    this.ZIndex = ViewModel.IsMinimized ? 1 : 100;
                }
            };

            // Set initial ZIndex
            this.ZIndex = ViewModel.IsMinimized ? 1 : 100;

            // Initialize ViewModel after XAML is loaded
            ViewModel.Initialize();

            // Wire up cleanup
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            ViewModel.Dispose();
        }
    }
}
