using Avalonia.Controls;
using Oracle.Views.Modals;
using Oracle.Components;
using Oracle.Services;
using System.Reflection;

namespace Oracle.Helpers;

/// <summary>
/// Helper methods for modal creation and management
/// </summary>
public static class ModalHelper
{
    /// <summary>
    /// Creates and shows a standard modal with the given content
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <param name="title">The modal title</param>
    /// <param name="content">The content to display in the modal</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowModal(this Views.BalatroMainMenu menu, string title, UserControl content)
    {
        var modal = new StandardModal(title);
        modal.SetContent(content);
        modal.BackClicked += (s, ev) => menu.HideModalContent();
        menu.ShowModalContent(modal);
        return modal;
    }
    
    /// <summary>
    /// Creates and shows a filters modal
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowFiltersModal(this Views.BalatroMainMenu menu)
    {
        var filtersContent = new FiltersModalContent();
        return menu.ShowModal("FILTER CONFIGURATION", filtersContent);
    }
    
    /// <summary>
    /// Creates and shows a fun runs modal
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowFunRunsModal(this Views.BalatroMainMenu menu)
    {
        var funRunView = new FunRunsModal();
        return menu.ShowModal("FUN RUNS", funRunView);
    }
    
    /// <summary>
    /// Creates and shows a search modal with search widget integration
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <param name="searchWidget">Optional search widget to integrate with</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowSearchModal(this Views.BalatroMainMenu menu, Components.SearchWidget? searchWidget = null)
    {
        var searchModal = new SearchModal();
        
        // If search widget provided, transfer state
        if (searchWidget != null)
        {
            // Get search service from widget using reflection (temporary)
            var searchServiceField = typeof(Components.SearchWidget).GetField("_searchService", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var configPathField = typeof(Components.SearchWidget).GetField("_configPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var foundCountField = typeof(Components.SearchWidget).GetField("_foundCount", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (searchServiceField?.GetValue(searchWidget) is Services.MotelySearchService searchService)
            {
                searchModal.SetSearchService(searchService);
                searchModal.SetResults(searchService.Results);
                searchModal.SetSearchState(searchService.IsRunning, (int)(foundCountField?.GetValue(searchWidget) ?? 0));
            }
            
            if (configPathField?.GetValue(searchWidget) is string configPath)
            {
                searchModal.SetConfigPath(configPath);
            }
        }
        
        var modal = new StandardModal("SEED SEARCH - MAXIMIZED");
        modal.SetContent(searchModal);
        
        // Custom back button behavior for search modal
        modal.BackClicked += (s, ev) =>
        {
            menu.HideModalContent();
            // Make search widget visible again if it exists
            if (searchWidget != null)
            {
                searchWidget.IsVisible = true;
            }
        };
        
        // Hide search widget while modal is open
        if (searchWidget != null)
        {
            searchWidget.IsVisible = false;
        }
        
        menu.ShowModalContent(modal);
        return modal;
    }
}