#if BROWSER
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using System.Diagnostics;

namespace BalatroSeedOracle.Services.Storage
{
    public partial class LocalStorageTester
    {
        public static async Task<bool> TestLocalStorageInterop()
        {
            try
            {
                Console.WriteLine("=== Testing LocalStorage Interop ===");
                
                // Test 1: Direct localStorage access
                try
                {
                    SetTestItem("bso:direct", "direct-test-value");
                    var directResult = GetTestItem("bso:direct");
                    Console.WriteLine($"Direct test: {directResult}");
                    RemoveTestItem("bso:direct");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Direct test failed: {ex.Message}");
                }
                
                // Test 2: Via window.BSO wrapper
                try
                {
                    SetLocalStorageItem("bso:wrapper", "wrapper-test-value");
                    var wrapperResult = GetLocalStorageItem("bso:wrapper");
                    Console.WriteLine($"Wrapper test: {wrapperResult}");
                    RemoveLocalStorageItem("bso:wrapper");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Wrapper test failed: {ex.Message}");
                }
                
                // Test 3: Test BrowserLocalStorageAppDataStore
                try
                {
                    var store = new BrowserLocalStorageAppDataStore();
                    await store.WriteTextAsync("bso:store-test", "store-test-value");
                    var storeResult = await store.ReadTextAsync("bso:store-test");
                    Console.WriteLine($"Store test: {storeResult}");
                    await store.DeleteAsync("bso:store-test");
                    return storeResult == "store-test-value";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Store test failed: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test suite failed: {ex.Message}");
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
