using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using BalatroSeedOracle.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.Controls;

/// <summary>
/// Error boundary control per Avalonia best practices: Control with StyledProperties and default template.
/// Catches layout/render exceptions from child content and shows a fallback UI (template-driven).
/// Minimal code-behind: only catch, set state, invalidate. Visual defined in App.axaml template.
/// </summary>
public class ErrorBoundary : ContentControl
{
    public static readonly StyledProperty<bool> HasErrorProperty = AvaloniaProperty.Register<
        ErrorBoundary,
        bool
    >(nameof(HasError), defaultValue: false);

    public static readonly StyledProperty<string> ErrorMessageProperty = AvaloniaProperty.Register<
        ErrorBoundary,
        string
    >(nameof(ErrorMessage), defaultValue: "");

    // Computed inverse of HasError, exposed so the control template can drive
    // ContentPresenter.IsVisible with a plain {TemplateBinding} — no negation,
    // no RelativeSource, no compiled-binding x:DataType ceremony.
    public static readonly DirectProperty<ErrorBoundary, bool> IsContentVisibleProperty =
        AvaloniaProperty.RegisterDirect<ErrorBoundary, bool>(
            nameof(IsContentVisible),
            o => o.IsContentVisible
        );

    // Read-only command exposed as DirectProperty so the template can bind to it
    // via {TemplateBinding RetryCommand} (TemplateBinding requires AvaloniaProperty).
    public static readonly DirectProperty<ErrorBoundary, System.Windows.Input.ICommand?> RetryCommandProperty =
        AvaloniaProperty.RegisterDirect<ErrorBoundary, System.Windows.Input.ICommand?>(
            nameof(RetryCommand),
            o => o.RetryCommand
        );

    public static readonly DirectProperty<ErrorBoundary, System.Windows.Input.ICommand?> CopyCommandProperty =
        AvaloniaProperty.RegisterDirect<ErrorBoundary, System.Windows.Input.ICommand?>(
            nameof(CopyCommand),
            o => o.CopyCommand
        );

    private readonly RelayCommand _retryCommand;
    private readonly RelayCommand _copyCommand;

    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    public string ErrorMessage
    {
        get => GetValue(ErrorMessageProperty) ?? "";
        set => SetValue(ErrorMessageProperty, value);
    }

    public bool IsContentVisible => !HasError;

    static ErrorBoundary()
    {
        // Re-raise IsContentVisible whenever HasError changes so the template binding updates.
        HasErrorProperty.Changed.AddClassHandler<ErrorBoundary>(
            (b, _) => b.RaisePropertyChanged(IsContentVisibleProperty, !b.IsContentVisible, b.IsContentVisible)
        );
    }

    /// <summary>
    /// Command to clear error and retry (bound by template). Per docs: commands, not events.
    /// </summary>
    public RelayCommand RetryCommand => _retryCommand;

    /// <summary>
    /// Command to copy the full error message to the clipboard.
    /// </summary>
    public RelayCommand CopyCommand => _copyCommand;

    public ErrorBoundary()
    {
        _retryCommand = new RelayCommand(ClearError);
        _copyCommand = new RelayCommand(CopyError);
    }

    private void CopyError()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        _ = clipboard?.SetTextAsync(ErrorMessage ?? "");
    }

    private void ClearError()
    {
        HasError = false;
        ErrorMessage = "";
        InvalidateMeasure();
        InvalidateArrange();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (HasError)
            return base.MeasureOverride(availableSize);

        try
        {
            return base.MeasureOverride(availableSize);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("ErrorBoundary", $"Caught in MeasureOverride: {ex.Message}");
            HasError = true;
            ErrorMessage = ex.Message;
            InvalidateMeasure();
            return new Size(400, 200);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (HasError)
            return base.ArrangeOverride(finalSize);

        try
        {
            return base.ArrangeOverride(finalSize);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("ErrorBoundary", $"Caught in ArrangeOverride: {ex.Message}");
            HasError = true;
            ErrorMessage = ex.Message;
            InvalidateArrange();
            return finalSize;
        }
    }
}
