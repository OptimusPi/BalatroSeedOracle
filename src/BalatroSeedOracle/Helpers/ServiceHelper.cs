using System;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Static service locator bridging view code to the hand-wired registry in <see cref="App"/>.
/// <para>
/// There is deliberately no fallback for <see cref="UserProfileService"/>: a second instance
/// would be a second writer to userprofile.json holding a stale copy of the profile, and
/// could overwrite settings the live (App-registered) instance just saved.
/// </para>
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
        typeof(T) == typeof(SpriteService) ? SpriteService.Instance : null;
}
