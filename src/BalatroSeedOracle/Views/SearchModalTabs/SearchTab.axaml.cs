using System.ComponentModel;
using Avalonia.Controls;

namespace BalatroSeedOracle.Views.SearchModalTabs
{
    /// <summary>
    /// Search tab for filter search modal.
    /// Uses direct x:Name field access (no FindControl anti-pattern).
    /// </summary>
    public partial class SearchTab : UserControl
    {
        public SearchTab()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            // Unsubscribe from old ViewModel
            if (
                sender is UserControl control
                && control.DataContext is INotifyPropertyChanged oldVm
            )
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            // Subscribe to new ViewModel
            if (DataContext is INotifyPropertyChanged newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // CRITICAL FIX: Auto-scroll console to bottom when ConsoleText updates
            if (e.PropertyName == "ConsoleText")
            {
                // Direct x:Name field access - no FindControl!
                if (ConsoleOutput != null)
                {
                    // Scroll to end after UI renders the new text
                    Avalonia.Threading.Dispatcher.UIThread.Post(
                        () =>
                        {
                            ConsoleOutput.CaretIndex = int.MaxValue;
                        },
                        Avalonia.Threading.DispatcherPriority.Background
                    );
                }
            }
        }
    }
}
