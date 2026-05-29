using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FilterNameInputDialog : UserControl
    {
        private readonly TaskCompletionSource<string?> _tcs = new();

        public FilterNameInputDialog()
        {
            InitializeComponent();
            
            // Focus textbox and select all after loaded
            this.Loaded += (s, e) =>
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(100);
                    FilterNameTextBox.Focus();
                    FilterNameTextBox.SelectAll();
                });
            };

            // Keyboard navigation (Enter / Escape)
            this.KeyDown += OnKeyDownHandler;
        }

        public FilterNameInputDialog(string title, string buttonText, string defaultName) : this()
        {
            TitleTextBlock.Text = title;
            ConfirmButton.Content = buttonText;
            FilterNameTextBox.Text = defaultName;
        }

        public Task<string?> GetResultAsync() => _tcs.Task;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        private void OnConfirmClick(object? sender, RoutedEventArgs e)
        {
            _tcs.TrySetResult(FilterNameTextBox.Text);
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            _tcs.TrySetResult(null);
        }

        private void OnKeyDownHandler(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _tcs.TrySetResult(FilterNameTextBox.Text);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                _tcs.TrySetResult(null);
                e.Handled = true;
            }
        }
    }
}
