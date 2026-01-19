using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    public partial class PaginatedFilterBrowser : UserControl
    {
        // Styled properties for parent configuration
        public static readonly StyledProperty<string> MainButtonTextProperty =
            AvaloniaProperty.Register<PaginatedFilterBrowser, string>(
                nameof(MainButtonText),
                "Select"
            );

        public static readonly StyledProperty<string> SecondaryButtonTextProperty =
            AvaloniaProperty.Register<PaginatedFilterBrowser, string>(
                nameof(SecondaryButtonText),
                "View"
            );

        public static readonly StyledProperty<bool> ShowSecondaryButtonProperty =
            AvaloniaProperty.Register<PaginatedFilterBrowser, bool>(
                nameof(ShowSecondaryButton),
                true
            );

        public static readonly StyledProperty<bool> ShowDeleteButtonProperty =
            AvaloniaProperty.Register<PaginatedFilterBrowser, bool>(nameof(ShowDeleteButton), true);

        public static readonly StyledProperty<ICommand> MainButtonCommandProperty =
            AvaloniaProperty.Register<PaginatedFilterBrowser, ICommand>(nameof(MainButtonCommand));

        public static readonly StyledProperty<ICommand> SecondaryButtonCommandProperty =
            AvaloniaProperty.Register<PaginatedFilterBrowser, ICommand>(
                nameof(SecondaryButtonCommand)
            );

        public static readonly StyledProperty<ICommand> DeleteCommandProperty =
            AvaloniaProperty.Register<PaginatedFilterBrowser, ICommand>(nameof(DeleteCommand));

        // Properties
        public string MainButtonText
        {
            get => GetValue(MainButtonTextProperty);
            set => SetValue(MainButtonTextProperty, value);
        }

        public string SecondaryButtonText
        {
            get => GetValue(SecondaryButtonTextProperty);
            set => SetValue(SecondaryButtonTextProperty, value);
        }

        public bool ShowSecondaryButton
        {
            get => GetValue(ShowSecondaryButtonProperty);
            set => SetValue(ShowSecondaryButtonProperty, value);
        }

        public bool ShowDeleteButton
        {
            get => GetValue(ShowDeleteButtonProperty);
            set => SetValue(ShowDeleteButtonProperty, value);
        }

        public ICommand MainButtonCommand
        {
            get => GetValue(MainButtonCommandProperty);
            set => SetValue(MainButtonCommandProperty, value);
        }

        public ICommand SecondaryButtonCommand
        {
            get => GetValue(SecondaryButtonCommandProperty);
            set => SetValue(SecondaryButtonCommandProperty, value);
        }

        public ICommand DeleteCommand
        {
            get => GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        public PaginatedFilterBrowserViewModel ViewModel { get; }

        // Events for parent notification
        public event EventHandler<string>? FilterSelected;

        public PaginatedFilterBrowser()
        {
            ViewModel = new PaginatedFilterBrowserViewModel();
            DataContext = ViewModel;

            InitializeComponent();

            // Wire up property changes to ViewModel
            this.PropertyChanged += OnPropertyChanged;

            // Wire up ViewModel events - convert FilterBrowserItem to string path
            ViewModel.FilterSelected += (s, filter) =>
                FilterSelected?.Invoke(this, filter.FilePath);

            // Handle item clicks in code-behind (simpler than complex XAML binding)
            this.Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            // Sync parent properties to ViewModel
            if (e.Property == MainButtonTextProperty)
            {
                ViewModel.MainButtonText = MainButtonText;
            }
            else if (e.Property == SecondaryButtonTextProperty)
            {
                ViewModel.SecondaryButtonText = SecondaryButtonText;
            }
            else if (e.Property == ShowSecondaryButtonProperty)
            {
                ViewModel.ShowSecondaryButton = ShowSecondaryButton;
            }
            else if (e.Property == ShowDeleteButtonProperty)
            {
                ViewModel.ShowDeleteButton = ShowDeleteButton;
            }
            else if (e.Property == MainButtonCommandProperty)
            {
                ViewModel.MainButtonCommand = MainButtonCommand;
            }
            else if (e.Property == SecondaryButtonCommandProperty)
            {
                ViewModel.SecondaryButtonCommand = SecondaryButtonCommand;
            }
            else if (e.Property == DeleteCommandProperty)
            {
                ViewModel.DeleteCommand = DeleteCommand;
            }
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            // Wire up button clicks when component loads
            WireUpButtonClicks();
        }

        private void WireUpButtonClicks()
        {
            // Note: Button clicks are handled via DataTemplate bindings in XAML
            // This method is kept for potential future use but currently not needed
            // Direct field access would be: var itemsControl = FilterItemsControl;
            // However, the ItemsControl in XAML doesn't have x:Name, so this is dead code
        }

        public void RefreshFilters()
        {
            ViewModel.RefreshFilters();
        }
    }
}
