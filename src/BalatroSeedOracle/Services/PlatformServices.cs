using System;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Static registry for platform-specific service registration.
/// Each platform (Desktop, Browser) sets this before App starts.
/// </summary>
public static class PlatformServices
{
    /// <summary>
    /// Action to register platform-specific services.
    /// Set this in Program.cs before BuildAvaloniaApp().
    /// </summary>
    public static Action<IServiceCollection>? RegisterServices { get; set; }
}
