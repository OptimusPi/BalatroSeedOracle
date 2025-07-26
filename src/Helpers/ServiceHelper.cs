using System;
using Microsoft.Extensions.DependencyInjection;

namespace Oracle.Helpers;

/// <summary>
/// Helper class to eliminate duplicate service provider retrieval code
/// </summary>
public static class ServiceHelper
{
    /// <summary>
    /// Gets a service from the application's service provider
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The service instance or null if not found</returns>
    public static T? GetService<T>() where T : class
    {
        var app = (App)App.Current!;
        var serviceProvider = app.GetType().GetField("_serviceProvider", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(app) as IServiceProvider;
        return serviceProvider?.GetService(typeof(T)) as T;
    }

    /// <summary>
    /// Gets a required service from the application's service provider
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The service instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not found</exception>
    public static T GetRequiredService<T>() where T : class
    {
        return GetService<T>() ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} not found");
    }
}