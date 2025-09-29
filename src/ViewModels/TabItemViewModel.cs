using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for individual tabs in a TabControl
    /// </summary>
    public partial class TabItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _header = string.Empty;

        [ObservableProperty] 
        private object? _content;

        [ObservableProperty]
        private bool _isSelected;

        public TabItemViewModel(string header, object? content = null)
        {
            Header = header;
            Content = content;
        }
    }
}