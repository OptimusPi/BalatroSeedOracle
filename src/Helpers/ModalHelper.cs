using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using BalatroSeedOracle.Views.Modals;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;
using System.Reflection;

namespace BalatroSeedOracle.Helpers
{
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
            menu.ShowModalContent(modal, title);
            return modal;
        }

        /// <summary>
        /// Creates and shows a filters modal
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowFiltersModal(this Views.BalatroMainMenu menu)
        {
            // Go directly to the filters modal - no chooser
            var filtersContent = new FiltersModalContent();
            return menu.ShowModal("FILTER DESIGNER", filtersContent);
        }
    
        /// <summary>
        /// Creates and shows a search modal
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <param name="configPath">Optional config path to load</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowSearchModal(this Views.BalatroMainMenu menu, string? configPath = null)
        {
            try
            {
                var searchContent = new SearchModal();
            
                // Handle desktop icon creation when modal closes with active search
                searchContent.CreateShortcutRequested += (sender, cfgPath) => 
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("ModalHelper", $"Desktop icon requested for config: {cfgPath}");
                    // Get the search ID from the modal
                    var searchId = searchContent.GetCurrentSearchId();
                    if (!string.IsNullOrEmpty(searchId))
                    {
                        menu.ShowSearchDesktopIcon(searchId, cfgPath);
                    }
                };
            
                if (!string.IsNullOrEmpty(configPath))
                {
                    // Just set the filter path immediately so it's ready when user clicks Cook
                    searchContent.SetCurrentFilterPath(configPath);
                    // Also load it async for the UI
                    _ = searchContent.LoadFilterAsync(configPath);
                }
                return menu.ShowModal("MOTELY SEARCH", searchContent);
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("ModalHelper", $"Failed to create SearchModal: {ex}");
                throw;
            }
        }
    
        /// <summary>
        /// Creates and shows a search modal with a config object (no temp files!)
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <param name="config">The OuijaConfig object to search with</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowSearchModalWithConfig(this Views.BalatroMainMenu menu, Motely.Filters.MotelyJsonConfig config)
        {
            // This method should not be used - filters must be saved first!
            throw new NotSupportedException("Filters must be saved before searching. Use ShowSearchModal with a file path instead.");
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
        /// Creates and shows a search modal
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowSearchModal(this Views.BalatroMainMenu menu)
        {
            var searchModal = new SearchModal();
        
            // Handle desktop icon creation when modal closes with active search
            searchModal.CreateShortcutRequested += (sender, configPath) => 
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("ModalHelper", $"Desktop icon requested for config: {configPath}");
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
        
            // Remove the desktop widget that opened this modal
            menu.RemoveSearchDesktopIcon(searchId);
        
            // Set the search ID so the modal can reconnect
            searchModal.SetSearchInstance(searchId);
        
            // Handle desktop icon creation when modal closes with active search
            searchModal.CreateShortcutRequested += (sender, cfgPath) => 
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("ModalHelper", $"Desktop icon requested for search: {searchId}");
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

        /// <summary>
        /// Creates a temp filter file for new filter creation
        /// </summary>
        private static async System.Threading.Tasks.Task<string> CreateTempFilter()
        {
            var baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? System.AppDomain.CurrentDomain.BaseDirectory;
            var filtersDir = System.IO.Path.Combine(baseDir, "JsonItemFilters");
            System.IO.Directory.CreateDirectory(filtersDir);
            
            var tempPath = System.IO.Path.Combine(filtersDir, "_UNSAVED_CREATION.json");
            
            // Create basic empty filter structure
            var emptyFilter = new Motely.Filters.MotelyJsonConfig
            {
                Name = "New Filter",
                Description = "Created with Filter Designer",
                Author = ServiceHelper.GetService<UserProfileService>()?.GetAuthorName() ?? "Unknown",
                DateCreated = System.DateTime.UtcNow,
                Must = new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                Should = new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                MustNot = new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>()
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(emptyFilter, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(tempPath, json);
            
            return tempPath;
        }

        /// <summary>
        /// Creates a cloned copy of an existing filter
        /// </summary>
        private static async System.Threading.Tasks.Task<string> CreateClonedFilter(string originalPath)
        {
            try
            {
                var originalJson = await System.IO.File.ReadAllTextAsync(originalPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(originalJson);
                
                if (config != null)
                {
                    // Update clone metadata
                    config.Name = $"{config.Name} (Copy)";
                    config.Author = ServiceHelper.GetService<UserProfileService>()?.GetAuthorName() ?? "Unknown";
                    config.DateCreated = System.DateTime.UtcNow;
                    
                    var baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? System.AppDomain.CurrentDomain.BaseDirectory;
                    var filtersDir = System.IO.Path.Combine(baseDir, "JsonItemFilters");
                    var clonedPath = System.IO.Path.Combine(filtersDir, $"{config.Name}.json");
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    await System.IO.File.WriteAllTextAsync(clonedPath, json);
                    
                    return clonedPath;
                }
            }
            catch (System.Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("ModalHelper", $"Failed to clone filter: {ex.Message}");
            }
            
            return string.Empty;
        }

    }
}