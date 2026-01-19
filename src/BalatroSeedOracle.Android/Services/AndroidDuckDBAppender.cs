using System;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.DuckDB;
using DuckDB.NET.Data;

namespace BalatroSeedOracle.Android.Services;

/// <summary>
/// Android implementation of IDuckDBAppender wrapping DuckDBAppender.
/// Adapts DuckDB.NET's row-based API to our fluent interface.
/// </summary>
public class AndroidDuckDBAppender : IDuckDBAppender
{
    private readonly DuckDBAppender _appender;
    private IDuckDBAppenderRow? _currentRow;
    private bool _disposed;

    public AndroidDuckDBAppender(DuckDBAppender appender)
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

    public async Task FlushAsync()
    {
        // DuckDBAppender doesn't have an async Flush method - data is committed on Close
        await Task.CompletedTask;
    }

    public async Task CloseAsync()
    {
        _appender.Close();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _appender?.Close();
        _appender?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        Dispose();
    }
}
