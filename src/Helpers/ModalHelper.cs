using Avalonia.Controls;
using Avalonia.Controls.Presenters;
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
    /// Creates and shows a search modal
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <param name="configPath">Optional config path to load</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowSearchModal(this Views.BalatroMainMenu menu, string? configPath = null)
    {
        var searchContent = new SearchModal();
        
        // Handle desktop icon creation when modal closes with active search
        searchContent.CreateDesktopIconRequested += (sender, cfgPath) => 
        {
            Oracle.Helpers.DebugLogger.Log("ModalHelper", $"Desktop icon requested for config: {cfgPath}");
            // Get the search ID from the modal
            var searchId = searchContent.GetCurrentSearchId();
            if (!string.IsNullOrEmpty(searchId))
            {
                menu.ShowSearchDesktopIcon(searchId, cfgPath);
            }
        };
        
        if (!string.IsNullOrEmpty(configPath))
        {
            // Load the config file into the search modal
            _ = searchContent.LoadFilterAsync(configPath);
        }
        return menu.ShowModal("MOTELY SEARCH", searchContent);
    }
    
    /// <summary>
    /// Creates and shows a search modal with a config object (no temp files!)
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <param name="config">The OuijaConfig object to search with</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowSearchModalWithConfig(this Views.BalatroMainMenu menu, Motely.Filters.OuijaConfig config)
    {
        var searchContent = new SearchModal();
        
        // Handle desktop icon creation when modal closes with active search
        searchContent.CreateDesktopIconRequested += (sender, cfgPath) => 
        {
            Oracle.Helpers.DebugLogger.Log("ModalHelper", $"Desktop icon requested for config: {cfgPath}");
            // Get the search ID from the modal
            var searchId = searchContent.GetCurrentSearchId();
            if (!string.IsNullOrEmpty(searchId))
            {
                menu.ShowSearchDesktopIcon(searchId, cfgPath);
            }
        };
        
        // Load the config object directly and start search
        _ = searchContent.LoadConfigDirectlyAsync(config);
        
        return menu.ShowModal("MOTELY SEARCH", searchContent);
    }

    /// <summary>
    /// Creates and shows a tools modal
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowToolsModal(this Views.BalatroMainMenu menu)
    {
        var ToolView = new ToolsModal();
        return menu.ShowModal("MORE", ToolView);
    }

    /// <summary>
    /// Creates and shows an analyzer modal
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowAnalyzerModal(this Views.BalatroMainMenu menu)
    {
        var analyzerView = new AnalyzerModal();
        return menu.ShowModal("SEED ANALYZER", analyzerView);
    }

    /// <summary>
    /// Creates and shows a search modal
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowSearchModal(this Views.BalatroMainMenu menu)
    {
        var searchModal = new SearchModal();
        
        // Handle desktop icon creation when modal closes with active search
        searchModal.CreateDesktopIconRequested += (sender, configPath) => 
        {
            Oracle.Helpers.DebugLogger.Log("ModalHelper", $"Desktop icon requested for config: {configPath}");
            // Get the search ID from the modal
            var searchId = searchModal.GetCurrentSearchId();
            if (!string.IsNullOrEmpty(searchId))
            {
                menu.ShowSearchDesktopIcon(searchId, configPath);
            }
        };
        
        return menu.ShowModal("MOTELY SEARCH", searchModal);
    }

    /// <summary>
    /// Creates and shows a search modal for an existing search instance
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <param name="searchId">The ID of the search instance to reconnect to</param>
    /// <param name="configPath">The config path for context</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowSearchModalForInstance(this Views.BalatroMainMenu menu, string searchId, string? configPath = null)
    {
        var searchModal = new SearchModal();
        
        // Set the search ID so the modal can reconnect
        searchModal.SetSearchInstance(searchId);
        
        // Handle desktop icon creation when modal closes with active search
        searchModal.CreateDesktopIconRequested += (sender, cfgPath) => 
        {
            Oracle.Helpers.DebugLogger.Log("ModalHelper", $"Desktop icon requested for search: {searchId}");
            menu.ShowSearchDesktopIcon(searchId, cfgPath);
        };
        
        return menu.ShowModal("MOTELY SEARCH", searchModal);
    }

    /// <summary>
    /// Creates and shows a word lists modal
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowWordListsModal(this Views.BalatroMainMenu menu)
    {
        var wordListsView = new WordListsModal();
        return menu.ShowModal("WORD LISTS", wordListsView);
    }

    /// <summary>
    /// Creates and shows a credits modal
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowCreditsModal(this Views.BalatroMainMenu menu)
    {
        var creditsView = new CreditsModal();
        return menu.ShowModal("CREDITS", creditsView);
    }

}