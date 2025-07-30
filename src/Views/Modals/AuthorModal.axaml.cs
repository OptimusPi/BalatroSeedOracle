using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Oracle.Helpers;
using Oracle.Services;

namespace Oracle.Views.Modals
{
    public partial class AuthorModalContent : UserControl
    {
        private readonly UserProfileService? _userProfileService;
        
        public AuthorModalContent()
        {
            InitializeComponent();
            
            _userProfileService = ServiceHelper.GetService<UserProfileService>();
            
            // Load current author name
            if (_userProfileService != null)
            {
                var authorBox = this.FindControl<TextBox>("AuthorNameBox");
                if (authorBox != null)
                {
                    authorBox.Text = _userProfileService.GetAuthorName();
                }
            }
        }
        
        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }
        
        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var authorBox = this.FindControl<TextBox>("AuthorNameBox");
                var newName = authorBox?.Text?.Trim();
                
                if (!string.IsNullOrEmpty(newName) && _userProfileService != null)
                {
                    _userProfileService.SetAuthorName(newName);
                    DebugLogger.Log("AuthorModal", $"Author name updated to: {newName}");
                }
                
                // Close the modal
                CloseModal();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AuthorModal", $"Error saving author name: {ex.Message}");
            }
        }
        
        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CloseModal();
        }
        
        private void CloseModal()
        {
            // Find the main menu and hide the modal
            var mainWindow = TopLevel.GetTopLevel(this) as Window;
            if (mainWindow != null)
            {
                // Find BalatroMainMenu within the window's content grid
                if (mainWindow.Content is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is BalatroMainMenu balMenu)
                        {
                            balMenu.HideModalContent();
                            break;
                        }
                    }
                }
            }
        }
    }
}