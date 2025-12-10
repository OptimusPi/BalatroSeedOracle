using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace BalatroSeedOracle.Extensions
{
    /// <summary>
    /// Extension methods for ItemsControl to work with realized containers
    /// </summary>
    public static class ItemsControlExtensions
    {
        /// <summary>
        /// Gets all currently realized (visually generated) container controls for an ItemsControl.
        /// This is essential for layout behaviors that need to position items on a Canvas.
        /// </summary>
        /// <param name="itemsControl">The ItemsControl to get containers from</param>
        /// <returns>Enumerable of realized container controls</returns>
        public static IEnumerable<Control> GetRealizedContainers(this ItemsControl itemsControl)
        {
            if (itemsControl == null)
                yield break;

            // Get the items presenter panel (usually a Canvas, StackPanel, etc.)
            var panel = itemsControl.GetVisualChildren();

            foreach (var child in panel)
            {
                // Look for the ItemsPresenter
                if (child is ItemsPresenter presenter)
                {
                    // Get the panel inside the presenter
                    foreach (var panelChild in presenter.GetVisualChildren())
                    {
                        if (panelChild is Panel itemsPanel)
                        {
                            // Return all visual children of the panel (these are the realized containers)
                            foreach (var item in itemsPanel.GetVisualChildren())
                            {
                                if (item is Control control)
                                {
                                    yield return control;
                                }
                            }
                        }
                    }
                }
                // If there's no ItemsPresenter, check if the child is directly a Panel
                else if (child is Panel directPanel)
                {
                    foreach (var item in directPanel.GetVisualChildren())
                    {
                        if (item is Control control)
                        {
                            yield return control;
                        }
                    }
                }
            }
        }
    }
}
