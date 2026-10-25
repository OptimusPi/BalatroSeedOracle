using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Squiggly underlines in the JAML editor. The engine is the judge: on every edit the document
/// goes through <see cref="JamlLanguage.Diagnose"/> — the engine's own loader (its error lands on
/// the line and token it names: "JAML line 3: Requested value 'TheEyez' was not found." underlines
/// <c>TheEyez</c>), then the search plan, then <see cref="Motely.FilterCheck.Warnings"/> (a boss in
/// an ante it can never appear in) as amber warnings on the clause they are about.
/// Callers may add their own markers (<see cref="AddError"/>), e.g. undefined YAML anchors; their
/// load-error markers are superseded by the engine's, which carry exact positions.
/// </summary>
public class JamlErrorMarkerService : IBackgroundRenderer
{
    public enum ErrorSeverity
    {
        Error,
        Warning,
    }

    /// <summary>A marker on 1-based <see cref="Line"/>, 0-based end-exclusive columns.</summary>
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

        public static ErrorMarker FromDiagnostic(JamlDiagnostic diagnostic) =>
            new(
                diagnostic.Line,
                diagnostic.StartColumn,
                diagnostic.EndColumn,
                diagnostic.Message,
                diagnostic.Severity == JamlDiagnosticSeverity.Error ? ErrorSeverity.Error : ErrorSeverity.Warning
            );
    }

    private static readonly IPen ErrorPen = new Pen(new SolidColorBrush(Color.FromRgb(0xFF, 0x4C, 0x40)), 1.4);
    private static readonly IPen WarningPen = new Pen(new SolidColorBrush(Color.FromRgb(0xFF, 0xB0, 0x20)), 1.4);
    private static readonly IBrush ErrorTint = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0x4C, 0x40));
    private static readonly IBrush WarningTint = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xB0, 0x20));

    private readonly TextEditor _editor;
    private readonly List<ErrorMarker> _external = [];
    private IReadOnlyList<ErrorMarker> _engine = [];
    private string? _diagnosedText;

    public JamlErrorMarkerService(TextEditor editor)
    {
        _editor = editor;
        _editor.TextChanged += (_, _) => UpdateErrors();
        Refresh();
    }

    /// <summary>
    /// Engine markers, then caller warnings. Caller-added errors are load errors parsed out of the
    /// loader's message text; the engine's own markers already carry them at exact positions, so
    /// those are dropped rather than painted twice.
    /// </summary>
    public IReadOnlyList<ErrorMarker> Markers
    {
        get
        {
            Refresh();
            return [.. _engine, .. _external.Where(m => m.Severity == ErrorSeverity.Warning)];
        }
    }

    /// <summary>Markers covering 1-based <paramref name="line"/> at 0-based <paramref name="column"/>.</summary>
    public IEnumerable<ErrorMarker> MarkersAt(int line, int column) =>
        Markers.Where(m => m.Line == line && column >= m.StartColumn && column <= Math.Max(m.EndColumn, m.StartColumn + 1));

    public KnownLayer Layer => KnownLayer.Selection;

    public void AddError(int line, int startColumn, int endColumn, string message, ErrorSeverity severity) =>
        _external.Add(new ErrorMarker(line, startColumn, endColumn, message, severity));

    /// <summary>Drop caller-added markers; the engine's are recomputed from the text.</summary>
    public void ClearErrors() => _external.Clear();

    /// <summary>Re-run the engine over the current text (if it changed) and repaint.</summary>
    public void UpdateErrors()
    {
        Refresh();
        _editor.TextArea?.TextView.InvalidateLayer(Layer);
    }

    private void Refresh()
    {
        var text = _editor.Document?.Text ?? "";
        if (ReferenceEquals(text, _diagnosedText) || text == _diagnosedText)
            return;
        _diagnosedText = text;
        try
        {
            _engine = [.. JamlLanguage.Diagnose(text).Select(ErrorMarker.FromDiagnostic)];
        }
        catch (Exception ex)
        {
            // An engine bug must not take the editor down; say so on line 1 instead.
            _engine = [new ErrorMarker(1, 0, 1, $"Filter check failed: {ex.Message}", ErrorSeverity.Error)];
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var document = textView.Document;
        if (document is null)
            return;
        textView.EnsureVisualLines();
        if (!textView.VisualLinesValid)
            return;

        foreach (var marker in Markers)
        {
            if (marker.Line < 1 || marker.Line > document.LineCount)
                continue;
            var line = document.GetLineByNumber(marker.Line);
            var start = Math.Clamp(marker.StartColumn, 0, line.Length);
            var end = Math.Clamp(marker.EndColumn, start, line.Length);
            if (end == start)
            {
                // Nothing to underline (empty line, or the column ran past it): mark the line's text.
                start = 0;
                end = line.Length;
            }
            var segment = new TextSegment { StartOffset = line.Offset + start, EndOffset = line.Offset + end };
            var pen = marker.Severity == ErrorSeverity.Error ? ErrorPen : WarningPen;
            var tint = marker.Severity == ErrorSeverity.Error ? ErrorTint : WarningTint;

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                var width = Math.Max(rect.Width, 6);
                drawingContext.FillRectangle(tint, new Rect(rect.X, rect.Y, width, rect.Height));
                drawingContext.DrawGeometry(null, pen, Squiggle(rect.BottomLeft, width));
            }
        }
    }

    /// <summary>A zig-zag 3px tall, 4px per wave, just under the text.</summary>
    private static StreamGeometry Squiggle(Point bottomLeft, double width)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        const double Step = 2;
        var y = bottomLeft.Y - 1;
        context.BeginFigure(new Point(bottomLeft.X, y), false);
        var up = true;
        for (var x = bottomLeft.X + Step; x <= bottomLeft.X + width; x += Step)
        {
            context.LineTo(new Point(x, up ? y - 2 : y));
            up = !up;
        }
        context.EndFigure(false);
        return geometry;
    }
}
