using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// Generic modal shell. Animation, sizing, and layout live in the .axaml
    /// (Style.Animations for the slide-in + class selectors driven by the
    /// Squeeze styled property for sizing). This code-behind only owns:
    /// lifecycle wiring, back-navigation routing, and the small set of public
    /// methods that external callers depend on.
    /// </summary>
    public partial class StandardModal : UserControl
    {
        public event EventHandler? BackClicked;
        private bool _isBackRequestedHooked;

        /// <summary>
        /// When true, modal uses auto sizing (max 700x500) instead of fixed 1080x600.
        /// Drives the .squeeze pseudo-class on ModalSizeGrid via XAML styles.
        /// </summary>
        public static readonly StyledProperty<bool> SqueezeProperty =
            AvaloniaProperty.Register<StandardModal, bool>(nameof(Squeeze), defaultValue: false);

        public bool Squeeze
        {
            get => GetValue(SqueezeProperty);
            set => SetValue(SqueezeProperty, value);
        }

        public StandardModal()
        {
            InitializeComponent();

            var backButton = this.FindControl<Button>("BackButton");
            if (backButton is not null)
                backButton.Click += OnBackButtonClick;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public StandardModal(string title)
            : this()
        {
            SetTitle(title);
        }



        /// <summary>Sets the modal title. Currently a no-op placeholder retained for API compatibility.</summary>
        public void SetTitle(string title)
        {
            // No-op: ModalTitle x:Name does not exist in current XAML.
            // Subclasses / call sites still pass titles via the constructor, so the API is preserved.
        }

        /// <summary>Sets the modal content area.</summary>
        public void SetContent(Control content)
        {
            var modalContent = this.FindControl<ContentPresenter>("ModalContent");
            if (modalContent is null)
            {
                DebugLogger.LogError("ModalContent is null!");
                return;
            }
            modalContent.Content = content;
        }

        /// <summary>Sets the back button text.</summary>
        public void SetBackButtonText(string text)
        {
            var backButton = this.FindControl<Button>("BackButton");
            if (backButton is not null)
                backButton.Content = text;
        }

        private void OnBackButtonClick(object? sender, RoutedEventArgs e)
        {
            if (!TryNavigateBackWithinModal())
                BackClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnTopLevelBackRequested(object? sender, RoutedEventArgs e)
        {
            if (TryNavigateBackWithinModal())
            {
                e.Handled = true;
                return;
            }

            BackClicked?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is not null && !_isBackRequestedHooked)
            {
                topLevel.BackRequested += OnTopLevelBackRequested;
                _isBackRequestedHooked = true;
            }
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is not null && _isBackRequestedHooked)
            {
                topLevel.BackRequested -= OnTopLevelBackRequested;
                _isBackRequestedHooked = false;
            }
        }

        /// <summary>
        /// Asks the current modal content (View first, then DataContext) whether it can
        /// handle the Back gesture internally (e.g. switching tabs in a multi-step flow).
        /// </summary>
        private bool TryNavigateBackWithinModal()
        {
            try
            {
                var modalContent = this.FindControl<ContentPresenter>("ModalContent");
                var content = modalContent?.Content;

                if (content is IModalBackNavigable viewNav && viewNav.TryGoBack())
                    return true;

                if (content is Control control
                    && control.DataContext is IModalBackNavigable vmNav
                    && vmNav.TryGoBack())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "StandardModal",
                    $"Error during back navigation attempt: {ex.Message}"
                );
            }

            return false;
        }
    }
}
