using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Views.SearchModalTabs
{
    public partial class ResultsTab : UserControl
    {
        public ResultsTab()
        {
            InitializeComponent();
            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            var grid = this.FindControl<SortableResultsGrid>("ResultsGrid");
            if (DataContext is SearchModalViewModel vm && grid != null)
            {
                // Bind results to the sortable grid
                grid.ItemsSource = vm.SearchResults;

                // Forward export-all to the ViewModel command
                grid.ExportAllRequested += (s, results) =>
                {
                    vm.ExportResultsCommand?.Execute(null);
                };

                // Wire up add-to-favorites
                grid.AddToFavoritesRequested += (s, result) =>
                {
                    if (!string.IsNullOrWhiteSpace(result?.Seed))
                    {
                        FavoritesService.Instance.AddFavoriteItem(result.Seed);
                    }
                };
            }
        }
    }
}
