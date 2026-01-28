using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Helpers
{
    public static class MixerHelper
    {
        private static readonly string MixerDirectory = AppPaths.MixerPresetsDir;

        static MixerHelper() { }

        public static bool SaveMixer(string name, MixerSettings settings)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    DebugLogger.LogError("MixerHelper", "Cannot save mixer without a name");
                    return false;
                }

                var safeName = NormalizeName(name);
                var path = Path.Combine(MixerDirectory, safeName + ".json");
                // AOT-compatible: Use source-generated serializer context
                var json = JsonSerializer.Serialize(settings, BsoJsonSerializerContext.Default.MixerSettings);
                File.WriteAllText(path, json);
                DebugLogger.Log("MixerHelper", $"Saved mixer '{name}' → {path}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MixerHelper", $"Error saving mixer: {ex.Message}");
                return false;
            }
        }

        public static MixerSettings? LoadMixer(string name)
        {
            try
            {
                var safeName = NormalizeName(name);
                var path = Path.Combine(MixerDirectory, safeName + ".json");
                if (!File.Exists(path))
                {
                    DebugLogger.LogError("MixerHelper", $"Mixer file not found: {path}");
                    return null;
                }
                var json = File.ReadAllText(path);
                // AOT-compatible: Use source-generated serializer context
                var settings = JsonSerializer.Deserialize(json, BsoJsonSerializerContext.Default.MixerSettings);
                return settings;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MixerHelper", $"Error loading mixer: {ex.Message}");
                return null;
            }
        }

        public static List<string> LoadAllMixerNames()
        {
            var names = new List<string>();
            try
            {
                if (!Directory.Exists(MixerDirectory))
                    return names;
                var files = Directory.GetFiles(MixerDirectory, "*.json");
                names = files
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .OrderBy(n => n)
                    .ToList();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MixerHelper", $"Error listing mixers: {ex.Message}");
            }
            return names;
        }

        public static int ClearAllMixers()
        {
            try
            {
                if (!Directory.Exists(MixerDirectory))
                    return 0;
                var files = Directory.GetFiles(MixerDirectory, "*.json");
                int deleted = 0;
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deleted++;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "MixerHelper",
                            $"Failed to delete '{file}': {ex.Message}"
                        );
                    }
                }
                return deleted;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MixerHelper", $"ClearAllMixers failed: {ex.Message}");
                return 0;
            }
        }

        private static string NormalizeName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return safe.Trim();
        }
    }
}
