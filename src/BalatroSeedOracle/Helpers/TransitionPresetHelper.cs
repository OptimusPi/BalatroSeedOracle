using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Helpers
{
    public static class TransitionPresetHelper
    {
        private static readonly string Dir = AppPaths.EnsureDir(
            Path.Combine(AppPaths.UserDir, "Transitions")
        );

        public static bool Save(TransitionPreset preset)
        {
            try
            {
                if (preset == null || string.IsNullOrWhiteSpace(preset.Name))
                    return false;
                var safe = Normalize(preset.Name);
                var path = Path.Combine(Dir, safe + ".json");
                // AOT-compatible: Use source-generated serializer context
                File.WriteAllText(path, JsonSerializer.Serialize(preset, BsoJsonSerializerContext.Default.TransitionPreset));
                DebugLogger.Log(
                    "TransitionPresetHelper",
                    $"Saved transition '{preset.Name}' → {path}"
                );
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("TransitionPresetHelper", $"Save failed: {ex.Message}");
                return false;
            }
        }

        public static TransitionPreset? Load(string name)
        {
            try
            {
                var safe = Normalize(name);
                var path = Path.Combine(Dir, safe + ".json");
                if (!File.Exists(path))
                    return null;
                // AOT-compatible: Use source-generated serializer context
                return JsonSerializer.Deserialize(File.ReadAllText(path), BsoJsonSerializerContext.Default.TransitionPreset);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("TransitionPresetHelper", $"Load failed: {ex.Message}");
                return null;
            }
        }

        public static List<string> ListNames()
        {
            try
            {
                if (!Directory.Exists(Dir))
                    return new List<string>();
                return Directory
                    .GetFiles(Dir, "*.json")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .OrderBy(n => n)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static string Normalize(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        }
    }
}
