using System.Collections.ObjectModel;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class FilterTabViewModel : BaseViewModel
    {
        public string Header { get; set; } = "";
        public ObservableCollection<FilterItem> Items { get; } = new();
    }
}