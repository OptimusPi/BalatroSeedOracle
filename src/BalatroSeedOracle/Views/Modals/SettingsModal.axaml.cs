using Avalonia.Controls;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// Settings Modal - Clean settings UI without feature flags
    /// Provides access to useful settings and resources
    /// </summary>
    public partial class SettingsModal : UserControl
    {
        public SettingsModal()
        {
            InitializeComponent();
            var modalHost = App.GetService<IModalHost>();
            var platformServices = App.GetService<IPlatformServices>();
            DataContext = new SettingsModalViewModel(modalHost, platformServices);
        }
    }
}
