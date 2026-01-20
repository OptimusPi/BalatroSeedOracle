using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.DuckDB;

namespace BalatroSeedOracle.Browser.Services;

/// <summary>
/// Browser implementation of IDuckDBAppender using DuckDB-WASM via JavaScript interop
/// </summary>
public partial class BrowserDuckDBAppender : IDuckDBAppender
{
    private readonly int _appenderId;
    private readonly List<object?> _currentRow = new();
    private bool _disposed;

    public BrowserDuckDBAppender(int appenderId)
    {
        _appenderId = appenderId;
    }

    public IDuckDBAppender CreateRow()
    {
        _currentRow.Clear();
        return this;
    }

    public IDuckDBAppender AppendValue(string? value)
    {
        _currentRow.Add(value);
        return this;
    }

    public IDuckDBAppender AppendValue(int value)
    {
        _currentRow.Add(value);
        return this;
    }

    public IDuckDBAppender AppendValue(long value)
    {
        _currentRow.Add(value);
        return this;
    }

    public IDuckDBAppender AppendValue(double value)
    {
        _currentRow.Add(value);
        return this;
    }

    public IDuckDBAppender AppendValue(bool value)
    {
        _currentRow.Add(value);
        return this;
    }

    public IDuckDBAppender AppendNullValue()
    {
        _currentRow.Add(null);
        return this;
    }

    public IDuckDBAppender EndRow()
    {
        // Serialize the row as JSON array and send to JavaScript
        var json = JsonSerializer.Serialize(_currentRow);
        AppendRowInternal(_appenderId, json);
        _currentRow.Clear();
        return this;
    }

    public void Flush()
    {
        // Fire and forget - we'll handle async in Close
        _ = FlushAppenderAsync(_appenderId);
    }

    public void Close()
    {
        if (_disposed)
            return;
        _ = CloseAppenderAsync(_appenderId);
        _disposed = true;
    }

    public void Dispose()
    {
        Close();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        await CloseAppenderAsync(_appenderId);
        _disposed = true;
    }

    [JSImport("DuckDB.appendRow", "globalThis")]
    private static extern void AppendRowInternal(int appenderId, string valuesJson);

    [JSImport("DuckDB.flushAppender", "globalThis")]
    private static extern Task FlushAppenderAsync(int appenderId);

    [JSImport("DuckDB.closeAppender", "globalThis")]
    private static extern Task CloseAppenderAsync(int appenderId);
}
