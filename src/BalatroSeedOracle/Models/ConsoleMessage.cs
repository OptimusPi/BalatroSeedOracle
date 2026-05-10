using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a message in the search console with optional copy-to-clipboard functionality
    /// </summary>
    public class ConsoleMessage
    {
        /// <summary>
        /// The formatted message text to display (includes timestamp)
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// Optional text to copy when copy button is clicked (e.g., just the seed name without timestamp)
        /// If null, no copy button is shown
        /// </summary>
        public string? CopyableText { get; set; }

        /// <summary>
        /// Whether this message has a copy button
        /// </summary>
        public bool HasCopyButton => !string.IsNullOrEmpty(CopyableText);

        /// <summary>
        /// Command to copy the copyable text to clipboard
        /// </summary>
        public ICommand? CopyCommand { get; set; }
    }
}
