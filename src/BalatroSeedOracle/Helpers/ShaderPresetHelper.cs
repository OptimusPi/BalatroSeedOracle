using System.IO;
using System.Text.Json;
using BalatroSeedOracle.Extensions;
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
                var cfg = JsonSerializer.Deserialize<ShaderParametersConfig>(json);
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
                if (!System.IO.Directory.Exists(Dir))
                    return list;
                foreach (var f in System.IO.Directory.GetFiles(Dir, "*.json"))
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(f);
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
                DebugLogger.LogError("ShaderPresetHelper", $"❌ Failed to list shader presets from {Dir}: {ex.Message}");
            }
            return list;
        }

        public static void Activate(string role, string name)
        {
            var roleFile = System.IO.Path.Combine(Dir, role.ToLowerInvariant() + ".json");
            if (string.IsNullOrWhiteSpace(name) || name.Equals("Default", System.StringComparison.OrdinalIgnoreCase))
            {
                if (System.IO.File.Exists(roleFile))
                    System.IO.File.Delete(roleFile);
                return;
            }
            var srcFile = System.IO.Path.Combine(Dir, name + ".json");
            if (!System.IO.File.Exists(srcFile))
                return;
            System.IO.File.Copy(srcFile, roleFile, true);
        }
    }
}
