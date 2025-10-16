using System.Collections.ObjectModel;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.ViewModels
{
    public class FilterTabViewModel : BaseViewModel
    {
        public ObservableCollection<FilterItem> MustHaveItems { get; set; }
        public ObservableCollection<FilterItem> ShouldHaveItems { get; set; }
        public ObservableCollection<FilterItem> MustNotHaveItems { get; set; }

        public FilterTabViewModel()
        {
            MustHaveItems = new ObservableCollection<FilterItem>();
            ShouldHaveItems = new ObservableCollection<FilterItem>();
            MustNotHaveItems = new ObservableCollection<FilterItem>();
        }
    }
}