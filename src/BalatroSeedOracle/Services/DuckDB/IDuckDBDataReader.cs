using System;

namespace BalatroSeedOracle.Services.DuckDB;

/// <summary>
/// Abstraction for reading data from query results.
/// </summary>
public interface IDuckDBDataReader
{
    /// <summary>
    /// Number of columns in the result
    /// </summary>
    int FieldCount { get; }

    /// <summary>
    /// Get column name by ordinal
    /// </summary>
    string GetName(int ordinal);

    /// <summary>
    /// Get ordinal by column name
    /// </summary>
    int GetOrdinal(string name);

    /// <summary>
    /// Check if a column value is null
    /// </summary>
    bool IsDBNull(int ordinal);

    /// <summary>
    /// Get string value
    /// </summary>
    string GetString(int ordinal);

    /// <summary>
    /// Get int32 value
    /// </summary>
    int GetInt32(int ordinal);

    /// <summary>
    /// Get int64 value
    /// </summary>
    long GetInt64(int ordinal);

    /// <summary>
    /// Get double value
    /// </summary>
    double GetDouble(int ordinal);

    /// <summary>
    /// Get boolean value
    /// </summary>
    bool GetBoolean(int ordinal);

    /// <summary>
    /// Get value as object
    /// </summary>
    object GetValue(int ordinal);
}
