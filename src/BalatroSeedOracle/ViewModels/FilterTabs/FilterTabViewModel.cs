using System.Collections.ObjectModel;
using BalatroSeedOracle.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class FilterTabViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _header = "";

        public ObservableCollection<FilterItem> Items { get; } = new();
    }
}
