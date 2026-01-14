using System;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using BalatroSeedOracle.Services.DuckDB;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation of IDuckDBAppender wrapping DuckDBAppender.
/// Adapts DuckDB.NET's row-based API to our fluent interface.
/// </summary>
public class DesktopDuckDBAppender : IDuckDBAppender
{
    private readonly DuckDBAppender _appender;
    private IDuckDBAppenderRow? _currentRow;
    private bool _disposed;

    public DesktopDuckDBAppender(DuckDBAppender appender)
    {
        _appender = appender ?? throw new ArgumentNullException(nameof(appender));
    }

    public IDuckDBAppender CreateRow()
    {
        _currentRow = _appender.CreateRow();
        return this;
    }

    public IDuckDBAppender AppendValue(string? value)
    {
        if (_currentRow == null)
            throw new InvalidOperationException("CreateRow must be called before AppendValue");

        if (value == null)
            _currentRow.AppendNullValue();
        else
            _currentRow.AppendValue(value);
        return this;
    }

    public IDuckDBAppender AppendValue(int value)
    {
        if (_currentRow == null)
            throw new InvalidOperationException("CreateRow must be called before AppendValue");

        _currentRow.AppendValue(value);
        return this;
    }

    public IDuckDBAppender AppendValue(long value)
    {
        if (_currentRow == null)
            throw new InvalidOperationException("CreateRow must be called before AppendValue");

        _currentRow.AppendValue(value);
        return this;
    }

    public IDuckDBAppender AppendValue(double value)
    {
        if (_currentRow == null)
            throw new InvalidOperationException("CreateRow must be called before AppendValue");

        _currentRow.AppendValue(value);
        return this;
    }

    public IDuckDBAppender AppendValue(bool value)
    {
        if (_currentRow == null)
            throw new InvalidOperationException("CreateRow must be called before AppendValue");

        _currentRow.AppendValue(value);
        return this;
    }

    public IDuckDBAppender AppendNullValue()
    {
        if (_currentRow == null)
            throw new InvalidOperationException("CreateRow must be called before AppendNullValue");

        _currentRow.AppendNullValue();
        return this;
    }

    public IDuckDBAppender EndRow()
    {
        if (_currentRow == null)
            throw new InvalidOperationException("CreateRow must be called before EndRow");

        _currentRow.EndRow();
        _currentRow = null;
        return this;
    }

    public void Flush()
    {
        // DuckDBAppender doesn't have a Flush method - data is committed on Close
    }

    public void Close()
    {
        _appender.Close();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _appender?.Close();
        _appender?.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
