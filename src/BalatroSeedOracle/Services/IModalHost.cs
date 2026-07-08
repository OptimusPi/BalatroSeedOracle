using Avalonia.Controls;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Interface for modal navigation, abstracting the BalatroMainMenu modal management.
    /// Enables ViewModels to request modal display without coupling to the view.
    /// </summary>
    public interface IModalHost
    {
        /// <summary>
        /// Shows a modal with the specified content.
        /// </summary>
        void ShowModal(UserControl content, string title);

        /// <summary>
        /// Hides the current modal content.
        /// </summary>
        void HideModal();

        /// <summary>
        /// Shows the Tools modal.
        /// </summary>
        void ShowToolsModal();

        /// <summary>
        /// Shows the Word Lists modal.
        /// </summary>
        void ShowWordListsModal();

        /// <summary>
        /// Shows the Word Lists modal with back navigation to Settings.
        /// </summary>
        void ShowWordListsModalFromSettings();

        /// <summary>
        /// Shows the Credits modal.
        /// </summary>
        void ShowCreditsModal();

        /// <summary>
        /// Shows the Settings modal.
        /// </summary>
        void ShowSettingsModal();

        /// <summary>
        /// Shows the Audio Visualizer Settings modal.
        /// </summary>
        void ShowAudioVisualizerSettingsModal();

        /// <summary>
        /// Shows the Filter Selection modal.
        /// </summary>
        void ShowFilterSelectionModal();

        /// <summary>
        /// Shows the Filters modal.
        /// </summary>
        void ShowFiltersModal();

        /// <summary>
        /// Shows the Search modal.
        /// </summary>
        void ShowSearchModal();

        /// <summary>
        /// Shows the Analyze modal.
        /// </summary>
        void ShowAnalyzeModal();
    }
}
