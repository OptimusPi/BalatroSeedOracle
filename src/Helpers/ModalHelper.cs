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
    /// Creates and shows a browse filters modal
    /// </summary>
    /// <param name="menu">The main menu to show the modal on</param>
    /// <returns>The created modal</returns>
    public static StandardModal ShowBrowseFiltersModal(this Views.BalatroMainMenu menu)
    {
        var browseModal = new BrowseFiltersModal();
        
        // Handle filter selection - launch search
        browseModal.FilterSelected += (sender, filterPath) =>
        {
            menu.HideModalContent();
            menu.ShowSearchWidget(filterPath);
        };
        
        // Handle edit request - open filters modal with loaded config
        browseModal.EditRequested += async (sender, filterPath) =>
        {
            menu.HideModalContent();
            var filtersModal = menu.ShowFiltersModal();
            
            // Load the filter into the modal - the modal itself is StandardModal, need to get its content
            var modalContentPresenter = filtersModal.FindControl<ContentPresenter>("ModalContent");
            if (modalContentPresenter?.Content is FiltersModalContent filtersContent)
            {
                await filtersContent.LoadConfigAsync(filterPath);
            }
        };
        
        return menu.ShowModal("BROWSE FILTERS", browseModal);
    }
    
}