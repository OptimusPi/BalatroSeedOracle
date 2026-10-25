using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Small, safe file persistence primitives shared by the settings-style services:
/// atomic replace (write temp file, then move over the target) and "keep what we don't
/// understand" JSON merging, so a save never drops keys written by another app version.
/// </summary>
public static class JsonFileStore
{
    private static readonly JsonSerializerOptions IndentedWrite = new() { WriteIndented = true };

    private static readonly JsonDocumentOptions LenientRead = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// Write <paramref name="contents"/> to <paramref name="path"/> atomically: the target is
    /// either the old file or the complete new file, never a truncated one.
    /// </summary>
    public static void WriteAllTextAtomic(string path, string contents)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
        try
        {
            using (
                var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)
            )
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(contents);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Best effort cleanup of the temp file; the original target is untouched.
            }
        }
    }

    /// <summary>
    /// Read a JSON object from disk. Returns null when the file is missing; throws
    /// <see cref="JsonException"/> when it exists but is not a JSON object.
    /// </summary>
    public static JsonObject? ReadObject(string path)
    {
        if (!File.Exists(path))
            return null;
        var text = File.ReadAllText(path);
        var node = JsonNode.Parse(text, documentOptions: LenientRead);
        return node as JsonObject
            ?? throw new JsonException($"{path} does not contain a JSON object.");
    }

    /// <summary>
    /// Overlay <paramref name="updates"/> onto <paramref name="original"/>: every key of the
    /// update replaces the original key with the same name (case-insensitive, so a PascalCase
    /// file stays PascalCase), and original keys the update does not know are kept.
    /// </summary>
    public static JsonObject Merge(JsonObject? original, JsonObject updates)
    {
        var result = original?.DeepClone().AsObject() ?? new JsonObject();
        foreach (var (key, value) in updates)
        {
            string? existingKey = null;
            foreach (var (candidate, _) in result)
            {
                if (string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase))
                {
                    existingKey = candidate;
                    break;
                }
            }
            var copy = value?.DeepClone();
            if (existingKey is null)
                result[key] = copy;
            else
                result[existingKey] = copy;
        }
        return result;
    }

    /// <summary>Indented JSON text for a node.</summary>
    public static string ToIndentedJson(JsonNode node) => node.ToJsonString(IndentedWrite);

    /// <summary>
    /// Keep a copy of a file that could not be parsed before anything overwrites it, so a
    /// bad hand edit or a partial write from an older build is never silently destroyed.
    /// </summary>
    public static string? PreserveUnreadableFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;
            var backup = $"{path}.unreadable-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
            File.Copy(path, backup, overwrite: true);
            return backup;
        }
        catch
        {
            return null;
        }
    }
}
