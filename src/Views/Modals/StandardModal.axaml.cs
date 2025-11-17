using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class StandardModal : UserControl
    {
        public event EventHandler? BackClicked;
        private bool _isBackRequestedHooked = false;

        /// <summary>
        /// When true, modal uses auto sizing instead of fixed 1080x600
        /// </summary>
        public static readonly StyledProperty<bool> SqueezeProperty = AvaloniaProperty.Register<
            StandardModal,
            bool
        >(nameof(Squeeze), defaultValue: false);

        public bool Squeeze
        {
            get => GetValue(SqueezeProperty);
            set => SetValue(SqueezeProperty, value);
        }

        public StandardModal()
        {
            InitializeComponent();

            // Wire up events
            var backButton = this.FindControl<Button>("BackButton");
            if (backButton != null)
            {
                backButton.Click += OnBackButtonClick;
            }

            // Only popup controls (like volume slider) should have click-away behavior

            // Hook TopLevel.BackRequested once the control is attached
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public StandardModal(string title)
            : this()
        {
            SetTitle(title);
        }

        /// <summary>
        /// Sets the modal title
        /// </summary>
        /// <param name="title">The title to display</param>
        public void SetTitle(string title)
        {
            var modalTitle = this.FindControl<TextBlock>("ModalTitle");
            if (modalTitle != null)
                modalTitle.Text = title;
        }

        /// <summary>
        /// Sets the modal content
        /// </summary>
        /// <param name="content">The content control to display</param>
        public void SetContent(Control content)
        {
            var modalContent = this.FindControl<ContentPresenter>("ModalContent");
            if (modalContent == null)
            {
                DebugLogger.LogError("ModalContent is null!");
                return;
            }
            DebugLogger.Log($"Setting content: {content?.GetType().Name ?? "null"}");
            DebugLogger.Log($"Content size: {content?.Width ?? 0} x {content?.Height ?? 0}");
            content?.InvalidateVisual();
            content?.UpdateLayout();
            modalContent.Content = content;

            // Force layout update
            content?.InvalidateVisual();
            this.InvalidateVisual();
        }

        /// <summary>
        /// Sets the back button text
        /// </summary>
        /// <param name="text">The text to display on the back button</param>
        public void SetBackButtonText(string text)
        {
            var backButton = this.FindControl<Button>("BackButton");
            if (backButton != null)
                backButton.Content = text;
        }

        private void OnBackButtonClick(object? sender, RoutedEventArgs e)
        {
            // First, allow in-modal back navigation for multi-step/progressive flows
            if (!TryNavigateBackWithinModal())
            {
                // Default behavior: close the modal
                BackClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnTopLevelBackRequested(object? sender, RoutedEventArgs e)
        {
            // Try in-modal back first; if not possible, close the modal
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
            if (topLevel != null && !_isBackRequestedHooked)
            {
                topLevel.BackRequested += OnTopLevelBackRequested;
                _isBackRequestedHooked = true;
            }

            // Apply sizing based on Squeeze property
            ApplySqueezeSizing();

            // Animate modal appearance for smooth slide-in
            try
            {
                AnimateIn();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("StandardModal", $"AnimateIn failed: {ex.Message}");
            }
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null && _isBackRequestedHooked)
            {
                topLevel.BackRequested -= OnTopLevelBackRequested;
                _isBackRequestedHooked = false;
            }
        }

        /// <summary>
        /// Applies sizing to the modal based on the Squeeze property
        /// </summary>
        private void ApplySqueezeSizing()
        {
            var modalSizeGrid = this.FindControl<Grid>("ModalSizeGrid");
            if (modalSizeGrid == null)
                return;

            if (Squeeze)
            {
                // Compact mode: auto-size with max constraints
                modalSizeGrid.Width = double.NaN; // Auto
                modalSizeGrid.Height = double.NaN; // Auto
                modalSizeGrid.MaxWidth = 700;
                modalSizeGrid.MaxHeight = 500;
            }
            else
            {
                // Standard mode: fixed size
                modalSizeGrid.Width = 1080;
                modalSizeGrid.Height = 600;
                modalSizeGrid.MaxWidth = double.PositiveInfinity;
                modalSizeGrid.MaxHeight = double.PositiveInfinity;
            }
        }

        /// <summary>
        /// Slide-in the modal with a subtle bounce and fade the overlay.
        /// </summary>
        private void AnimateIn()
        {
            var overlay = this.FindControl<Border>("OverlayBackground");
            var modalBorder = this.FindControl<Border>("ModalBorder");
            if (overlay == null || modalBorder == null)
                return;

            // Set up transitions
            overlay.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Border.OpacityProperty,
                    Duration = TimeSpan.FromMilliseconds(UIConstants.FastAnimationDurationMs),
                    Easing = new CubicEaseOut(),
                },
            };

            modalBorder.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Border.OpacityProperty,
                    Duration = TimeSpan.FromMilliseconds(UIConstants.StandardAnimationDurationMs),
                    Easing = new CubicEaseOut(),
                },
                new ThicknessTransition
                {
                    Property = Border.MarginProperty,
                    Duration = TimeSpan.FromMilliseconds(UIConstants.SlowAnimationDurationMs),
                    Easing = new BackEaseOut(),
                },
            };

            // Start from offscreen and transparent
            overlay.Opacity = UIConstants.InvisibleOpacity;
            modalBorder.Opacity = UIConstants.InvisibleOpacity;
            modalBorder.Margin = new Thickness(
                0,
                UIConstants.ModalSlideOffsetY,
                0,
                UIConstants.ModalSlideOffsetBottomMargin
            );

            // Animate to final state on next UI tick
            Dispatcher.UIThread.Post(
                () =>
                {
                    overlay.Opacity = UIConstants.FullOpacity;
                    modalBorder.Opacity = UIConstants.FullOpacity;
                    modalBorder.Margin = new Thickness(0, 0, 0, 0);
                },
                DispatcherPriority.Render
            );
        }

        /// <summary>
        /// Checks whether the current modal content supports back navigation and attempts it.
        /// Returns true if a back navigation occurred, false otherwise.
        /// </summary>
        private bool TryNavigateBackWithinModal()
        {
            try
            {
                var contentPresenter = this.FindControl<ContentPresenter>("ModalContent");
                var content = contentPresenter?.Content;

                // Try view-level implementation first
                if (
                    content is BalatroSeedOracle.Helpers.IModalBackNavigable viewNav
                    && viewNav.TryGoBack()
                )
                {
                    return true;
                }

                // Then try DataContext-level implementation (common for MVVM ViewModels)
                if (
                    content is Control control
                    && control.DataContext is BalatroSeedOracle.Helpers.IModalBackNavigable vmNav
                )
                {
                    if (vmNav.TryGoBack())
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

        /// <summary>
        /// Shows a modal dialog with the specified content
        /// </summary>
        /// <param name="parent">The parent window</param>
        /// <param name="title">The modal title</param>
        /// <param name="content">The content to display</param>
        /// <param name="showBackButton">Whether to show the back button</param>
        public static async Task ShowModal(
            Window parent,
            string title,
            Control content,
            bool showBackButton = true
        )
        {
            var modal = new StandardModal();
            modal.SetTitle(title);
            modal.SetContent(content);

            var backButton = modal.FindControl<Button>("BackButton");
            if (backButton != null)
            {
                backButton.IsVisible = showBackButton;
            }

            // Create overlay and show
            var overlay = new Grid();
            overlay.Children.Add(modal);

            var mainGrid = parent.Content as Grid;
            if (mainGrid != null)
            {
                mainGrid.Children.Add(overlay);

                // Handle back button click
                modal.BackClicked += (s, e) =>
                {
                    mainGrid.Children.Remove(overlay);
                };

                // Create task completion source to await modal close
                var tcs = new TaskCompletionSource<bool>();

                modal.BackClicked += (s, e) =>
                {
                    tcs.SetResult(true);
                };

                await tcs.Task;
            }
        }
    }
}
