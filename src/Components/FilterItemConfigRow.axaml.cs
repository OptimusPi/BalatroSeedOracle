using Avalonia.Controls;
using Avalonia.VisualTree;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components
{
    public partial class FilterItemConfigRow : UserControl
    {
        public FilterItemConfigRow()
        {
            InitializeComponent();

            // When DataContext changes to a FilterItem, wrap it in a ViewModel
            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is FilterItem item)
                {
                    // Determine if this is a SHOULD item based on parent context
                    bool isShouldItem = false;

                    // Try to find parent UserControl to access ViewModel
                    var parent = this.FindAncestorOfType<UserControl>();
                    if (parent?.DataContext is VisualBuilderTabViewModel vm)
                    {
                        isShouldItem = vm.SelectedShould.Contains(item);
                    }

                    // Create ViewModel wrapper with remove callback
                    var viewModel = new FilterItemConfigRowViewModel(
                        item,
                        isShouldItem,
                        removeCallback: (removedItem) =>
                        {
                            // Find parent ViewModel and remove from appropriate collection
                            var parentControl = this.FindAncestorOfType<UserControl>();
                            if (parentControl?.DataContext is VisualBuilderTabViewModel parentVm)
                            {
                                if (parentVm.SelectedMust.Contains(removedItem))
                                {
                                    parentVm.SelectedMust.Remove(removedItem);
                                }
                                else if (parentVm.SelectedShould.Contains(removedItem))
                                {
                                    parentVm.SelectedShould.Remove(removedItem);
                                }
                            }
                        }
                    );

                    // Set the ViewModel as DataContext
                    DataContext = viewModel;
                }
            };
        }
    }
}
