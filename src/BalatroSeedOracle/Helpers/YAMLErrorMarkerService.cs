using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Squiggly-underline renderer for JAML editor validation errors and anchor warnings.
/// TIER0-STUB: replace in Tier 1. Errors are collected and exposed for tooltips, but
/// <see cref="Draw"/> is a no-op until marker brushes/geometry are wired to the theme.
/// </summary>
public class YAMLErrorMarkerService : IBackgroundRenderer
{
    public enum ErrorSeverity
    {
        Error,
        Warning,
    }

    public sealed class ErrorMarker
    {
        public int Line { get; }
        public int StartColumn { get; }
        public int EndColumn { get; }
        public string Message { get; }
        public ErrorSeverity Severity { get; }

        public ErrorMarker(int line, int startColumn, int endColumn, string message, ErrorSeverity severity)
        {
            Line = line;
            StartColumn = startColumn;
            EndColumn = endColumn;
            Message = message;
            Severity = severity;
        }
    }

    private readonly TextEditor _editor;
    private readonly List<ErrorMarker> _markers = [];

    public YAMLErrorMarkerService(TextEditor editor)
    {
        _editor = editor;
    }

    /// <summary>Markers in the order they were added (tooltip + tests read these).</summary>
    public IReadOnlyList<ErrorMarker> Markers => _markers;

    public KnownLayer Layer => KnownLayer.Background;

    public void AddError(int line, int startColumn, int endColumn, string message, ErrorSeverity severity) =>
        _markers.Add(new ErrorMarker(line, startColumn, endColumn, message, severity));

    public void ClearErrors() => _markers.Clear();

    /// <summary>Repaint the markers ( Tier 0: nothing to redraw yet ).</summary>
    public void UpdateErrors()
    {
        _editor.TextArea?.TextView.InvalidateLayer(Layer);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        // TIER0-STUB: no visual markers yet.
    }
}
