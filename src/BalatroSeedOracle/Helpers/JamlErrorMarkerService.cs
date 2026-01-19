using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Service for marking errors in JAML editor with squiggly underlines
    /// </summary>
    public class JamlErrorMarkerService : IBackgroundRenderer
    {
        private readonly TextEditor _editor;
        private readonly List<ErrorMarker> _markers = new();

        public JamlErrorMarkerService(TextEditor editor)
        {
            _editor = editor;
        }

        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView.VisualLines == null || _markers.Count == 0)
                return;

            foreach (var marker in _markers)
            {
                var visualLine = textView.VisualLines.FirstOrDefault(vl =>
                    vl.FirstDocumentLine.LineNumber == marker.Line
                );

                if (visualLine == null)
                    continue;

                var startOffset = marker.StartOffset;
                var endOffset = marker.EndOffset;

                // Find the visual positions
                var startPos = visualLine.GetVisualPosition(
                    startOffset - visualLine.FirstDocumentLine.Offset,
                    VisualYPosition.TextBottom
                );
                var endPos = visualLine.GetVisualPosition(
                    endOffset - visualLine.FirstDocumentLine.Offset,
                    VisualYPosition.TextBottom
                );

                // Draw squiggly underline
                DrawSquigglyLine(
                    drawingContext,
                    new Point(startPos.X, startPos.Y),
                    new Point(endPos.X, endPos.Y),
                    marker.Color
                );
            }
        }

        private void DrawSquigglyLine(DrawingContext drawingContext, Point start, Point end, Color color)
        {
            var pen = new Pen(new SolidColorBrush(color), 1.0);
            var width = end.X - start.X;
            var segments = Math.Max(1, (int)(width / 4));

            var points = new List<Point>();
            for (int i = 0; i <= segments; i++)
            {
                var x = start.X + (width * i / segments);
                var y = start.Y + (i % 2 == 0 ? 0 : 2);
                points.Add(new Point(x, y));
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                drawingContext.DrawLine(pen, points[i], points[i + 1]);
            }
        }

        public void AddError(
            int line,
            int startColumn,
            int endColumn,
            string message,
            ErrorSeverity severity = ErrorSeverity.Error
        )
        {
            if (_editor.Document == null)
                return;

            var documentLine = _editor.Document.GetLineByNumber(line);
            var startOffset = documentLine.Offset + Math.Min(startColumn, documentLine.Length);
            var endOffset = documentLine.Offset + Math.Min(endColumn, documentLine.Length);

            var color = severity switch
            {
                ErrorSeverity.Error => Colors.Red,
                ErrorSeverity.Warning => Colors.Orange,
                ErrorSeverity.Info => Colors.Blue,
                _ => Colors.Red,
            };

            var marker = new ErrorMarker
            {
                Line = line,
                StartOffset = startOffset,
                EndOffset = endOffset,
                Message = message,
                Severity = severity,
                Color = color,
            };

            _markers.Add(marker);
        }

        public void ClearErrors()
        {
            _markers.Clear();
            _editor.TextArea.TextView.InvalidateVisual();
        }

        public void UpdateErrors()
        {
            _editor.TextArea.TextView.InvalidateVisual();
        }

        public List<ErrorMarker> GetErrors() => _markers.ToList();

        public class ErrorMarker
        {
            public int Line { get; set; }
            public int StartOffset { get; set; }
            public int EndOffset { get; set; }
            public string Message { get; set; } = "";
            public ErrorSeverity Severity { get; set; }
            public Color Color { get; set; }
        }

        public enum ErrorSeverity
        {
            Error,
            Warning,
            Info,
        }
    }
}
