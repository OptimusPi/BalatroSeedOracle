using System;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Implement on a modal's View or ViewModel to handle Back navigation.
    /// The StandardModal will call TryGoBack(); if it returns true, the modal stays open.
    /// If it returns false, the modal closes (default behavior).
    /// </summary>
    public interface IModalBackNavigable
    {
        /// <summary>
        /// Attempt to navigate back within the modal (e.g., previous tab/step).
        /// Return true if navigation occurred; false to indicate there is no back step.
        /// </summary>
        bool TryGoBack();
    }
}