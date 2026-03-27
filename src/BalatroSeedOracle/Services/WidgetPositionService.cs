using System;
using System.Collections.Generic;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Manages widget positioning — stub for rebuild.
/// Will be rebuilt for the new C# markup widget system.
/// </summary>
public class WidgetPositionService
{
    private double _lastKnownParentWidth = 1200.0;
    private double _lastKnownParentHeight = 700.0;

    public void UpdateParentSize(double width, double height)
    {
        _lastKnownParentWidth = width;
        _lastKnownParentHeight = height;
    }

    public (double X, double Y) GetNextAvailablePosition() => (100, 100);
}
