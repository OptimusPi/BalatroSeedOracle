using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using Oracle.Helpers;

namespace Oracle.Views.Modals
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
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public StandardModal(string title) : this()
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
    }
}