using System.Collections.Generic;
using Avalonia.Controls;

namespace Oracle.Views.Modals
{
    public partial class SearchResultsWindow : Window
    {
        public SearchResultsWindow()
        {
            InitializeComponent();
        }
        
        public void LoadResults(List<SearchResult> results, string filterName, System.TimeSpan? searchDuration = null)
        {
            var modal = this.FindControl<SearchResultsModal>("ResultsModal");
            modal?.LoadResults(results, filterName, searchDuration);
        }
    }
}