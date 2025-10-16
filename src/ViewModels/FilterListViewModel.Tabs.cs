using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.ViewModels
{
    // TabControl index wiring for FilterSelector
    public partial class FilterListViewModel
    {
        // 0=Must Have, 1=Should Have, 2=Must Not
        [ObservableProperty]
        private int _selectedTabIndex = 0;

        partial void OnSelectedTabIndexChanged(int value)
        {
            var tabType = value switch
            {
                0 => "must_have",
                1 => "should_have",
                2 => "must_not_have",
                _ => "must_have"
            };

            SelectedTabType = tabType;
            LoadFilterItemsForTab(tabType);
        }

        partial void OnSelectedTabTypeChanged(string value)
        {
            SelectedTabIndex = value switch
            {
                "must_have" => 0,
                "should_have" => 1,
                "must_not_have" => 2,
                _ => SelectedTabIndex
            };
        }
    }
}