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
        private TextBlock? _titleTextBlock;
        private Button? _confirmButton;
        private TextBox? _filterNameTextBox;
        private string? _pendingTitle;
        private string? _pendingButtonText;
        private string? _pendingDefaultName;

        public FilterNameInputDialog()
        {
            InitializeComponent();

            // This project's InitializeComponent only calls AvaloniaXamlLoader.Load(this), which
            // builds the visual tree but does NOT assign the generated x:Name fields — so
            // TitleTextBlock/ConfirmButton/FilterNameTextBox are null. Resolve them through the
            // name scope (as StandardModal does) and null-guard every access so this can't throw.
            this.Loaded += (s, e) =>
            {
                _titleTextBlock ??= this.FindControl<TextBlock>("TitleTextBlock");
                _confirmButton ??= this.FindControl<Button>("ConfirmButton");
                _filterNameTextBox ??= this.FindControl<TextBox>("FilterNameTextBox");

                if (_pendingTitle is not null && _titleTextBlock is not null)
                    _titleTextBlock.Text = _pendingTitle;
                if (_pendingButtonText is not null && _confirmButton is not null)
                    _confirmButton.Content = _pendingButtonText;
                if (_pendingDefaultName is not null && _filterNameTextBox is not null)
                    _filterNameTextBox.Text = _pendingDefaultName;

                // Focus the textbox and select all
                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(100);
                    _filterNameTextBox?.Focus();
                    _filterNameTextBox?.SelectAll();
                });
            };

            // Keyboard navigation (Enter / Escape)
            this.KeyDown += OnKeyDownHandler;
        }

        public FilterNameInputDialog(string title, string buttonText, string defaultName) : this()
        {
            _pendingTitle = title;
            _pendingButtonText = buttonText;
            _pendingDefaultName = defaultName;
        }

        public Task<string?> GetResultAsync() => _tcs.Task;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        private void OnConfirmClick(object? sender, RoutedEventArgs e)
        {
            _tcs.TrySetResult(_filterNameTextBox?.Text);
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            _tcs.TrySetResult(null);
        }

        private void OnKeyDownHandler(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _tcs.TrySetResult(_filterNameTextBox?.Text);
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
