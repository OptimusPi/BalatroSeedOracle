using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;
using System;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Components
{
    public partial class DailyRitualWidget : BaseWidgetControl
    {
        public DailyRitualWidgetViewModel? ViewModel { get; }

        public DailyRitualWidget()
        {
            // Initialize ViewModel
            ViewModel = new DailyRitualWidgetViewModel(
                DaylatroHighScoreService.Instance,
                App.GetService<UserProfileService>() 
                    ?? throw new InvalidOperationException("UserProfileService not available"),
                App.GetService<FilterConfigurationService>()
                    ?? throw new InvalidOperationException("FilterConfigurationService not available")
            );

            DataContext = ViewModel;
            
            InitializeComponent();

            ViewModel.Initialize();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
