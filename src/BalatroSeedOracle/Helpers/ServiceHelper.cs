using System;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Static service locator bridging view code to the hand-wired registry in <see cref="App"/>.
/// TIER0-STUB: replace in Tier 1 with constructor injection once a real container exists.
/// </summary>
public static class ServiceHelper
{
    /// <summary>Resolve a registered service, or a well-known fallback, or throw.</summary>
    public static T GetRequiredService<T>()
        where T : class =>
        App.GetService<T>()
        ?? (T?)Fallback<T>()
        ?? throw new InvalidOperationException(
            $"Service '{typeof(T).Name}' is not registered in App."
        );

    /// <summary>Resolve a registered service, or null when none is registered.</summary>
    public static T? GetService<T>()
        where T : class =>
        App.GetService<T>() ?? (T?)Fallback<T>();

    private static object? Fallback<T>()
        where T : class =>
        typeof(T) == typeof(SpriteService) ? SpriteService.Instance
        : typeof(T) == typeof(UserProfileService) ? new UserProfileService()
        : null;
}
