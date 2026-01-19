#if BROWSER
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using System.Diagnostics;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Browser.Services;

namespace BalatroSeedOracle.Services.Storage
{
    public partial class LocalStorageTester
    {
        public static async Task<bool> TestLocalStorageInterop()
        {
            try
            {
                DebugLogger.LogImportant(
                    "LocalStorageTester",
                    "=== Testing LocalStorage Interop ==="
                );

                // Test 1: Direct localStorage access
                try
                {
                    SetTestItem("bso:direct", "direct-test-value");
                    var directResult = GetTestItem("bso:direct");
                    DebugLogger.Log("LocalStorageTester", $"Direct test: {directResult}");
                    RemoveTestItem("bso:direct");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("LocalStorageTester", $"Direct test failed: {ex.Message}");
                }

                // Test 2: Via window.BSO wrapper
                try
                {
                    SetLocalStorageItem("bso:wrapper", "wrapper-test-value");
                    var wrapperResult = GetLocalStorageItem("bso:wrapper");
                    DebugLogger.Log("LocalStorageTester", $"Wrapper test: {wrapperResult}");
                    RemoveLocalStorageItem("bso:wrapper");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "LocalStorageTester",
                        $"Wrapper test failed: {ex.Message}"
                    );
                }

                // Test 3: Test BrowserLocalStorageAppDataStore
                try
                {
                    var store = new BrowserLocalStorageAppDataStore();
                    await store.WriteTextAsync("bso:store-test", "store-test-value");
                    var storeResult = await store.ReadTextAsync("bso:store-test");
                    DebugLogger.Log("LocalStorageTester", $"Store test: {storeResult}");
                    await store.DeleteAsync("bso:store-test");
                    return storeResult == "store-test-value";
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("LocalStorageTester", $"Store test failed: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("LocalStorageTester", $"Test suite failed: {ex.Message}");
                return false;
            }
        }

        // Direct localStorage imports
        [JSImport("globalThis.localStorage.setItem")]
        private static partial void SetTestItem(string key, string value);

        [JSImport("globalThis.localStorage.getItem")]
        private static partial string? GetTestItem(string key);

        [JSImport("globalThis.localStorage.removeItem")]
        private static partial void RemoveTestItem(string key);

        // Wrapper imports
        [JSImport("window.BSO.setLocalStorageItem")]
        private static partial void SetLocalStorageItem(string key, string value);

        [JSImport("window.BSO.getLocalStorageItem")]
        private static partial string? GetLocalStorageItem(string key);

        [JSImport("window.BSO.removeLocalStorageItem")]
        private static partial void RemoveLocalStorageItem(string key);
    }
}
#endif
