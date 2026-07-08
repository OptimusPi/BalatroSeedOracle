using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FilterNameInputDialog : UserControl
    {
        private readonly FilterNameInputDialogViewModel _viewModel;

        public FilterNameInputDialog()
        {
            _viewModel = new FilterNameInputDialogViewModel();
            InitializeComponent();
            DataContext = _viewModel;

            this.Loaded += OnLoaded;
            this.KeyDown += OnKeyDownHandler;
        }

        public FilterNameInputDialog(string title, string buttonText, string defaultName)
        {
            _viewModel = new FilterNameInputDialogViewModel(title, buttonText, defaultName);
            InitializeComponent();
            DataContext = _viewModel;

            this.Loaded += OnLoaded;
            this.KeyDown += OnKeyDownHandler;
        }

        public Task<string?> GetResultAsync() => _viewModel.GetResultAsync();

        private void OnLoaded(object? sender, System.EventArgs e)
        {
            // Focus the textbox and select all
            Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay(100);
                FilterNameTextBox.Focus();
                FilterNameTextBox.SelectAll();
            });
        }

        private void OnKeyDownHandler(object? sender, KeyEventArgs e)
        {
            _viewModel.HandleKeyDown(e);
        }
    }
}
