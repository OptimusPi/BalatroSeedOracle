namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Implemented by modal view models with internal navigation stacks: the modal chrome
/// asks before closing so internal state can unwind first.
/// </summary>
public interface IModalBackNavigable
{
    /// <summary>
    /// Navigate back through internal state. Returns true when the internal stack consumed
    /// the back gesture (the modal stays open); false lets the modal close.
    /// </summary>
    bool TryGoBack();
}
