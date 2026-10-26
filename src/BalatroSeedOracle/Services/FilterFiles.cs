using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Motely.Filters;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Filter file locations and load/save: everything under YAMLFilters/ in the run directory
/// (created at startup by App, same convention as Motely.CLI), one file per filter. A JAML
/// filter is .yaml, .yml or .jaml, as the engine reads them; new filters are written .yaml.
/// Saves are atomic (temp file + move) so a crash mid-save never leaves a truncated filter.
/// </summary>
public static class FilterFiles
{
    /// <summary>The run-directory filter folder (created at startup by App).</summary>
    public static string Dir => "YAMLFilters";

    /// <summary>The extensions a JAML filter is written in; the first is the one new filters get.</summary>
    public static readonly string[] Extensions = [".yaml", ".yml", ".jaml"];

    /// <summary>File-picker patterns for <see cref="Extensions"/>.</summary>
    public static readonly string[] PickerPatterns = [.. Extensions.Select(e => "*" + e)];

    /// <summary>True when the path has a filter extension.</summary>
    public static bool IsFilterFile(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolve a filter name or id to its path inside <see cref="Dir"/>: the existing file
    /// whatever its extension (.yaml, .yml, .jaml), else the .yaml path a new filter would get.
    /// </summary>
    public static string Resolve(string filterName)
    {
        var safe = string.Concat(
            (filterName ?? string.Empty).Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)
        ).Trim();
        if (safe.Length == 0)
            safe = "filter";
        if (IsFilterFile(safe))
            return Path.Combine(Dir, safe);
        foreach (var extension in Extensions)
        {
            var existing = Path.Combine(Dir, safe + extension);
            if (File.Exists(existing))
                return existing;
        }
        return Path.Combine(Dir, safe + Extensions[0]);
    }

    /// <summary>Load and parse a filter file; null + message when it cannot be read.</summary>
    public static JamlConfig? Load(string path, out string? error)
    {
        try
        {
            error = null;
            return JamlConfigLoader.FromFile(path);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    /// <summary>Serialize and write a filter; true on success.</summary>
    public static bool Save(JamlConfig config, string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            Helpers.JsonFileStore.WriteAllTextAtomic(filePath, Helpers.JamlConfigText.ToJaml(config));
            return true;
        }
        catch (Exception ex)
        {
            Helpers.DebugLogger.LogError(nameof(FilterFiles), $"Save failed for {filePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>All filter file paths in <see cref="Dir"/>, sorted for stable UI lists.</summary>
    public static List<string> List()
    {
        try
        {
            return Directory.Exists(Dir)
                ? Directory.EnumerateFiles(Dir).Where(IsFilterFile).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList()
                : [];
        }
        catch (Exception ex)
        {
            Helpers.DebugLogger.LogError(nameof(FilterFiles), $"List failed: {ex.Message}");
            return [];
        }
    }

    /// <summary>Delete a filter file when it exists.</summary>
    public static void Delete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Helpers.DebugLogger.LogError(nameof(FilterFiles), $"Delete failed for {path}: {ex.Message}");
        }
    }
}
