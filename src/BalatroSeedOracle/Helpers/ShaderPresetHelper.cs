using System.IO;
using System.Text.Json;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Helpers
{
    public static class ShaderPresetHelper
    {
        // Use the proper VisualizerPresets folder (not UserDir/ShaderPresets)
        private static readonly string Dir = AppPaths.VisualizerPresetsDir;

        public static ShaderParameters Load(string name)
        {
            var defaults =
                name.ToLowerInvariant() == "intro"
                    ? VisualizerPresetExtensions.CreateDefaultIntroParameters()
                    : VisualizerPresetExtensions.CreateDefaultNormalParameters();

            var path = Path.Combine(Dir, name.ToLowerInvariant() + ".json");
            if (!File.Exists(path))
                return defaults;

            try
            {
                var json = File.ReadAllText(path);
                // AOT-compatible: Use source-generated serializer context
                var cfg = JsonSerializer.Deserialize(json, BsoJsonSerializerContext.Default.ShaderParametersConfig);
                if (cfg == null)
                    return defaults;
                return cfg.ToShaderParameters(defaults);
            }
            catch (System.Exception ex)
            {
                // Log the failure but gracefully fall back to defaults
                DebugLogger.LogError(
                    "ShaderPresetHelper",
                    $"❌ Failed to load shader preset '{name}' from {path}: {ex.Message}"
                );
                return defaults;
            }
        }

        public static System.Collections.Generic.List<string> ListNames()
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                if (!Directory.Exists(Dir))
                    return list;
                foreach (var f in Directory.GetFiles(Dir, "*.json"))
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    if (name.Equals("intro", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (name.Equals("normal", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    list.Add(name);
                }
            }
            catch (System.Exception ex)
            {
                // Log the failure but gracefully return empty list
                DebugLogger.LogError(
                    "ShaderPresetHelper",
                    $"❌ Failed to list shader presets from {Dir}: {ex.Message}"
                );
            }
            return list;
        }

        public static void Activate(string role, string name)
        {
            var roleFile = Path.Combine(Dir, role.ToLowerInvariant() + ".json");
            if (
                string.IsNullOrWhiteSpace(name)
                || name.Equals("Default", System.StringComparison.OrdinalIgnoreCase)
            )
            {
                if (File.Exists(roleFile))
                    File.Delete(roleFile);
                return;
            }
            var srcFile = Path.Combine(Dir, name + ".json");
            if (!File.Exists(srcFile))
                return;
            File.Copy(srcFile, roleFile, true);
        }
    }
}
