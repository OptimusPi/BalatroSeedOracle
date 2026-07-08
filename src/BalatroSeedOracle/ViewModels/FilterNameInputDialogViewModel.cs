using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    public partial class FilterNameInputDialogViewModel : ObservableObject
    {
        private readonly TaskCompletionSource<string?> _tcs = new();

        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private string? _buttonText = "Confirm";

        [ObservableProperty]
        private string? _defaultName;

        [ObservableProperty]
        private string? _inputText;

        public FilterNameInputDialogViewModel()
        {
        }

        public FilterNameInputDialogViewModel(string title, string buttonText, string defaultName)
        {
            Title = title;
            ButtonText = buttonText;
            DefaultName = defaultName;
            InputText = defaultName;
        }

        public Task<string?> GetResultAsync() => _tcs.Task;

        [RelayCommand]
        private void Confirm()
        {
            _tcs.TrySetResult(InputText);
        }

        [RelayCommand]
        private void Cancel()
        {
            _tcs.TrySetResult(null);
        }

        public void HandleKeyDown(Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                _tcs.TrySetResult(InputText);
                e.Handled = true;
            }
            else if (e.Key == Avalonia.Input.Key.Escape)
            {
                _tcs.TrySetResult(null);
                e.Handled = true;
            }
        }
    }
}
