using System.ComponentModel;
using Avalonia.Controls;

namespace BalatroSeedOracle.Views.SearchModalTabs
{
    public partial class SearchTab : UserControl
    {
        private TextBox? _consoleOutput;

        public SearchTab()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            // Unsubscribe from old ViewModel
            if (sender is UserControl control && control.DataContext is INotifyPropertyChanged oldVm)
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
                // Lazy-load console reference
                _consoleOutput ??= this.FindControl<TextBox>("ConsoleOutput");

                if (_consoleOutput != null)
                {
                    // Scroll to end after UI renders the new text
                    Avalonia.Threading.Dispatcher.UIThread.Post(
                        () =>
                        {
                            _consoleOutput.CaretIndex = int.MaxValue;
                        },
                        Avalonia.Threading.DispatcherPriority.Background
                    );
                }
            }
        }
    }
}
