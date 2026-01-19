using DuckDB.NET.Data;

namespace Motely.DuckDB;

/// <summary>
/// Cross-platform DuckDB operations helper
/// </summary>
public static class DuckDBOperations
{
    /// <summary>
    /// Get row count for a table
    /// </summary>
    public static long GetRowCount(DuckDBConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    /// <summary>
    /// Check if a table exists
    /// </summary>
    public static bool TableExists(DuckDBConnection connection, string tableName)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            cmd.ExecuteScalar();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if a column exists in a table
    /// </summary>
    public static bool ColumnExists(DuckDBConnection connection, string tableName, string columnName)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $@"
                SELECT COUNT(*) 
                FROM information_schema.columns 
                WHERE table_name = '{tableName}' AND column_name = '{columnName}'";
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt64(result) > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Execute a query and return results
    /// </summary>
    public static List<Dictionary<string, object?>> ExecuteQuery(DuckDBConnection connection, string sql)
    {
        var results = new List<Dictionary<string, object?>>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();
        
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }
        
        return results;
    }

    /// <summary>
    /// Execute a scalar query
    /// </summary>
    public static T? ExecuteScalar<T>(DuckDBConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
            return default;
        return (T)Convert.ChangeType(result, typeof(T));
    }
}
