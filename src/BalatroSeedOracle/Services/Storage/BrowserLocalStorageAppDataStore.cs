#if BROWSER
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using System.Diagnostics;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services.Storage;

public sealed partial class BrowserLocalStorageAppDataStore : IAppDataStore
{
    private const string Prefix = "bso:";

    private static string MakeKey(string key) => Prefix + key;

    public BrowserLocalStorageAppDataStore()
    {
        Debug.WriteLine("BrowserLocalStorageAppDataStore initialized");
        // Console.WriteLine removed for AI compatibility - use DebugLogger instead
        DebugLogger.Log("BrowserLocalStorageAppDataStore", "Initialized");
        
        // Test basic localStorage interop
        try
        {
            SetItem("bso:test", "test-value");
            var testValue = GetItem("bso:test");
            DebugLogger.Log("BrowserLocalStorageAppDataStore", $"LocalStorage test result: {testValue}");
            Debug.WriteLine($"LocalStorage test result: {testValue}");
            
            if (testValue == "test-value")
            {
                DebugLogger.LogImportant("BrowserLocalStorageAppDataStore", "LocalStorage interop is working!");
            }
            else
            {
                DebugLogger.LogError("BrowserLocalStorageAppDataStore", "LocalStorage interop failed - wrong value returned");
            }
            
            // Clean up test
            RemoveItem("bso:test");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("BrowserLocalStorageAppDataStore", $"LocalStorage test failed: {ex.Message}");
            DebugLogger.LogError("BrowserLocalStorageAppDataStore", $"Exception type: {ex.GetType().Name}");
            Debug.WriteLine($"LocalStorage test failed: {ex.Message}");
        }
    }

    public Task<bool> ExistsAsync(string key)
    {
        try
        {
            var value = GetItem(MakeKey(key));
            return Task.FromResult(value != null);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("BrowserLocalStorageAppDataStore", $"Error checking if key exists: {ex.Message}");
            Debug.WriteLine($"Error checking if key exists: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<string?> ReadTextAsync(string key)
    {
        try
        {
            var value = GetItem(MakeKey(key));
            return Task.FromResult<string?>(value);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("BrowserLocalStorageAppDataStore", $"Error reading text for key {key}: {ex.Message}");
            Debug.WriteLine($"Error reading text for key {key}: {ex.Message}");
            return Task.FromResult<string?>(null);
        }
    }

    public Task WriteTextAsync(string key, string content)
    {
        try
        {
            SetItem(MakeKey(key), content);
            DebugLogger.Log("BrowserLocalStorageAppDataStore", $"Successfully wrote {content.Length} chars to key {key}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("BrowserLocalStorageAppDataStore", $"Error writing text for key {key}: {ex.Message}");
            Debug.WriteLine($"Error writing text for key {key}: {ex.Message}");
            throw;
        }
    }

    public Task DeleteAsync(string key)
    {
        try
        {
            RemoveItem(MakeKey(key));
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("BrowserLocalStorageAppDataStore", $"Error deleting key {key}: {ex.Message}");
            Debug.WriteLine($"Error deleting key {key}: {ex.Message}");
            throw;
        }
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix)
    {
        try
        {
            var results = new List<string>();
            var desired = MakeKey(prefix);

            // Try to iterate through localStorage
            var len = GetLocalStorageLength();
            DebugLogger.Log("BrowserLocalStorageAppDataStore", $"LocalStorage has {len} items total");
            
            for (var i = 0; i < len; i++)
            {
                var k = GetLocalStorageKey(i);
                if (k == null)
                    continue;
                if (!k.StartsWith(desired, StringComparison.Ordinal))
                    continue;

                // Strip the bso: prefix back off
                results.Add(k.Substring(Prefix.Length));
            }

            return Task.FromResult<IReadOnlyList<string>>(results);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("BrowserLocalStorageAppDataStore", $"Error listing keys with prefix {prefix}: {ex.Message}");
            Debug.WriteLine($"Error listing keys with prefix {prefix}: {ex.Message}");
            return Task.FromResult<IReadOnlyList<string>>(new List<string>());
        }
    }

    // Try using wrapper functions for localStorage properties
    [JSImport("window.BSO.testLocalStorage")]
    private static partial string TestLocalStorage();

    [JSImport("window.BSO.getLocalStorageItem")]
    private static partial string? GetItem(string key);

    [JSImport("window.BSO.setLocalStorageItem")]
    private static partial void SetItem(string key, string value);

    [JSImport("window.BSO.removeLocalStorageItem")]
    private static partial void RemoveItem(string key);

    [JSImport("window.BSO.getLocalStorageLength")]
    private static partial int GetLocalStorageLength();

    [JSImport("window.BSO.getLocalStorageKey")]
    private static partial string? GetLocalStorageKey(int index);
}
#endif
