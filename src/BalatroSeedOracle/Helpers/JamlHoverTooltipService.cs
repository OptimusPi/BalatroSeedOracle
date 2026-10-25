using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Hover tooltips in the JAML editor: what a clause key, filter key, clause property or item
/// name means (from the engine's vocabulary, via <see cref="JamlLanguage.Hover"/>), and the
/// message of any error/warning marker under the pointer.
/// </summary>
public class JamlHoverTooltipService
{
    private readonly TextEditor _editor;
    private string? _shown;

    public JamlHoverTooltipService(TextEditor editor)
    {
        _editor = editor;
    }

    public void Install()
    {
        var textView = _editor.TextArea.TextView;
        textView.PointerMoved += OnPointerMoved;
        textView.PointerExited += (_, _) => Hide();
        _editor.TextChanged += (_, _) => Hide();
        ToolTip.SetShowDelay(textView, 350);
        ToolTip.SetPlacement(textView, PlacementMode.Pointer);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var text = TooltipAt(e);
        if (text == _shown)
            return;
        if (text is null)
        {
            Hide();
            return;
        }
        var textView = _editor.TextArea.TextView;
        _shown = text;
        ToolTip.SetTip(textView, new TextBlock { Text = text, MaxWidth = 520, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        ToolTip.SetIsOpen(textView, false);
        ToolTip.SetIsOpen(textView, true);
    }

    private string? TooltipAt(PointerEventArgs e)
    {
        var document = _editor.Document;
        if (document is null)
            return null;
        var position = _editor.GetPositionFromPoint(e.GetPosition(_editor));
        if (position is not { } pos || pos.Line < 1 || pos.Line > document.LineCount)
            return null;
        var column = pos.Column - 1;
        var line = document.GetLineByNumber(pos.Line);
        if (column > line.Length)
            return null;

        var builder = new StringBuilder();
        var markers = _editor.TextArea.TextView.BackgroundRenderers.OfType<JamlErrorMarkerService>().FirstOrDefault();
        if (markers is not null)
            foreach (var marker in markers.MarkersAt(pos.Line, column))
            {
                if (builder.Length > 0)
                    builder.AppendLine();
                builder.Append(marker.Severity == JamlErrorMarkerService.ErrorSeverity.Error ? "Error: " : "Warning: ");
                builder.Append(marker.Message);
            }

        if (JamlLanguage.Hover(document.Text, pos.Line - 1, column) is { } info)
        {
            if (builder.Length > 0)
                builder.AppendLine().AppendLine();
            builder.Append(info);
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private void Hide()
    {
        if (_shown is null)
            return;
        _shown = null;
        var textView = _editor.TextArea.TextView;
        ToolTip.SetIsOpen(textView, false);
        ToolTip.SetTip(textView, null);
    }
}
