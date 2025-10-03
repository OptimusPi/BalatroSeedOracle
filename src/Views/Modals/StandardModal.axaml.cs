using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class StandardModal : UserControl
    {
        public event EventHandler? BackClicked;

        public StandardModal()
        {
            InitializeComponent();

            // Wire up events
            var backButton = this.FindControl<Button>("BackButton");
            if (backButton != null)
            {
                backButton.Click += OnBackButtonClick;
            }

            // Click-away-to-close: Click on overlay background to close
            var overlayBackground = this.FindControl<Border>("OverlayBackground");
            if (overlayBackground != null)
            {
                overlayBackground.PointerPressed += OnOverlayClicked;
            }
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
            BackClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnOverlayClicked(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            // Only close if clicked directly on overlay, not on modal content
            var clickedElement = e.Source as Control;
            var overlayBackground = this.FindControl<Border>("OverlayBackground");
            var modalBorder = this.FindControl<Border>("ModalBorder");

            // Check if click was on the overlay background (not on the modal itself)
            if (clickedElement == overlayBackground)
            {
                BackClicked?.Invoke(this, EventArgs.Empty);
            }
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
