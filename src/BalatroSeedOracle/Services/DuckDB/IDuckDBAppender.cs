using System;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services.DuckDB;

/// <summary>
/// Abstraction for DuckDB appender for bulk inserts.
/// </summary>
public interface IDuckDBAppender : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Start a new row for appending
    /// </summary>
    /// <returns>Row builder for chaining</returns>
    IDuckDBAppender CreateRow();

    /// <summary>
    /// Append a string value to the current row
    /// </summary>
    IDuckDBAppender AppendValue(string? value);

    /// <summary>
    /// Append an integer value to the current row
    /// </summary>
    IDuckDBAppender AppendValue(int value);

    /// <summary>
    /// Append a long value to the current row
    /// </summary>
    IDuckDBAppender AppendValue(long value);

    /// <summary>
    /// Append a double value to the current row
    /// </summary>
    IDuckDBAppender AppendValue(double value);

    /// <summary>
    /// Append a boolean value to the current row
    /// </summary>
    IDuckDBAppender AppendValue(bool value);

    /// <summary>
    /// Append a null value to the current row
    /// </summary>
    IDuckDBAppender AppendNullValue();

    /// <summary>
    /// End the current row (commits to the appender buffer)
    /// </summary>
    IDuckDBAppender EndRow();

    /// <summary>
    /// Flush the appender buffer to the database
    /// </summary>
    Task FlushAsync();

    /// <summary>
    /// Close the appender (automatically flushes)
    /// </summary>
    Task CloseAsync();
}
