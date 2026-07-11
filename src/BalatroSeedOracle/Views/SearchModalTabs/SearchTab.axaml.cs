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
        // Avalonia's DataContextChanged carries no old-value event args, and by the time the
        // handler runs, DataContext already IS the new value — reading DataContext here can
        // never reach the previous ViewModel. Track it ourselves so reassignment unsubscribes
        // the actual old VM instead of a no-op on the new one.
        private INotifyPropertyChanged? _subscribedViewModel;

        public SearchTab()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            // Unsubscribe from old ViewModel
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedViewModel = null;
            }

            // Subscribe to new ViewModel
            if (DataContext is INotifyPropertyChanged newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                _subscribedViewModel = newVm;
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

        protected override void OnDetachedFromVisualTree(
            Avalonia.VisualTreeAttachmentEventArgs e
        )
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedViewModel = null;
            }
            base.OnDetachedFromVisualTree(e);
        }
    }
}
