using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FiltersModal : UserControl
    {
        private static FiltersModal? _activeInstance;
        private readonly FiltersModalViewModel? _viewModel;
        private ContentControl? _tabContentHost;
        private ContentControl? _popupContentHost;

        public FiltersModalViewModel? ViewModel => _viewModel ?? DataContext as FiltersModalViewModel;

        /// <summary>Parameterless ctor for XAML loader only. Throws at runtime. Creator must pass ViewModel.</summary>
        public FiltersModal()
            : this(throwForDesignTimeOnly: true) { }

        private FiltersModal(bool throwForDesignTimeOnly)
        {
            if (throwForDesignTimeOnly)
                throw new InvalidOperationException(
                    "Do not use FiltersModal(). Use new FiltersModal(menu.ViewModel.FiltersModalViewModel)."
                );
            InitializeComponent();
            _tabContentHost = this.FindControl<ContentControl>("TabContentHost");
            _popupContentHost = this.FindControl<ContentControl>("PopupContentHost");
        }

        public FiltersModal(FiltersModalViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            if (_activeInstance != null && _activeInstance != this)
            {
                _activeInstance.DetachFromViewModel();
            }

            DataContext = null;
            InitializeComponent();
            _tabContentHost = this.FindControl<ContentControl>("TabContentHost");
            _popupContentHost = this.FindControl<ContentControl>("PopupContentHost");
            _viewModel.InitializeTabs();
            AttachToViewModel();
        }

        private void AttachToViewModel()
        {
            if (_viewModel == null)
                return;

            if (_activeInstance != null && _activeInstance != this)
            {
                _activeInstance.DetachFromViewModel();
            }

            _activeInstance = this;
            _viewModel.DetachTabViews();
            _viewModel.CurrentTabContent =
                (
                    _viewModel.SelectedTabIndex >= 0
                    && _viewModel.SelectedTabIndex < _viewModel.TabItems.Count
                )
                    ? _viewModel.TabItems[_viewModel.SelectedTabIndex].Content
                    : null;
            DataContext = _viewModel;
        }

        private void DetachFromViewModel()
        {
            if (_tabContentHost != null)
            {
                _tabContentHost.Content = null;
            }
            if (_popupContentHost != null)
            {
                _popupContentHost.Content = null;
            }
            DataContext = null;

            if (_activeInstance == this)
            {
                _viewModel?.DetachTabViews();
                _activeInstance = null;
            }
        }

        protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (_viewModel != null && DataContext == null)
            {
                AttachToViewModel();
            }
        }

        protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            DetachFromViewModel();
        }

        protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            DetachFromViewModel();
        }

        /// <summary>
        /// Load configuration from file (delegates to ViewModel)
        /// </summary>
        public async Task LoadConfigAsync(string configPath)
        {
            if (ViewModel == null)
                return;

            if (!string.IsNullOrWhiteSpace(configPath))
            {
                ViewModel.CurrentFilterPath = configPath;
                await ViewModel.ReloadVisualFromSavedFileCommand.ExecuteAsync(null);
            }
            else
            {
                ViewModel.LoadFilter();
            }
        }

        /// <summary>
        /// Enable all tabs (for editing existing filters)
        /// </summary>
        public void EnableAllTabs()
        {
            // In proper MVVM, tabs are always enabled via binding
        }
    }
}
