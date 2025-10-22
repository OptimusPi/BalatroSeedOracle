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

            // Calculate triangle X position for downward-pointing indicator above tabs
            // Triangle is 16px wide (Points="0,0 16,0 8,12"), so subtract 8 from center
            // Tab text widths: "Must Have"≈100px, "Should Have"≈115px, "Must Not"≈105px
            // Each tab has Margin="4,0", Padding="16,8"
            SelectedTabTriangleX = value switch
            {
                0 => 46,      // "Must Have" tab center at ~54px → 54-8=46
                1 => 158,     // "Should Have" tab center at ~166px → 166-8=158
                2 => 272,     // "Must Not" tab center at ~280px → 280-8=272
                _ => 46
            };
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