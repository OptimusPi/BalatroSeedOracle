using System;
using Avalonia;
using Avalonia.Controls;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Controls
{
    public partial class SourceSelector : UserControl
    {
        private SourceSelectorViewModel? _viewModel;

        // Bindable property for MVVM
        public static readonly StyledProperty<string> SelectedSourceTagProperty =
            AvaloniaProperty.Register<SourceSelector, string>(nameof(SelectedSourceTag), defaultValue: "");

        public string SelectedSourceTag
        {
            get => GetValue(SelectedSourceTagProperty);
            set => SetValue(SelectedSourceTagProperty, value);
        }

        public event EventHandler<string>? SourceChanged;

        public SourceSelector()
        {
            InitializeComponent();
            InitializeViewModel();

            // Wire up property changes
            this.GetObservable(SelectedSourceTagProperty).Subscribe(OnSelectedSourceTagChanged);
        }

        private void InitializeViewModel()
        {
            _viewModel = new SourceSelectorViewModel();
            DataContext = _viewModel;

            // Forward ViewModel events to maintain API compatibility
            _viewModel.SourceChanged += OnViewModelSourceChanged;
        }

        private void OnViewModelSourceChanged(object? sender, string sourceTag)
        {
            // Update the property without triggering a loop
            if (SelectedSourceTag != sourceTag)
            {
                SetCurrentValue(SelectedSourceTagProperty, sourceTag);
            }
            SourceChanged?.Invoke(this, sourceTag);
        }

        private void OnSelectedSourceTagChanged(string sourceTag)
        {
            // Update ViewModel when property changes externally
            if (_viewModel != null && _viewModel.GetSelectedSource() != sourceTag)
            {
                _viewModel.SetSelectedSource(sourceTag);
            }
        }

        // Public API - delegates to ViewModel (kept for backward compatibility)
        public string GetSelectedSource()
        {
            return _viewModel?.GetSelectedSource() ?? "";
        }

        public void SetSelectedSource(string source)
        {
            _viewModel?.SetSelectedSource(source);
        }

        // Static helper methods
        public static string GetSourceDisplayName(string source)
        {
            return SourceSelectorViewModel.GetSourceDisplayName(source);
        }

        public static string[] GetAllSources()
        {
            return SourceSelectorViewModel.GetAllSources();
        }
    }
}
