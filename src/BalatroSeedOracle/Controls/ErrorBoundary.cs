using System;
using Avalonia;
using Avalonia.Controls;
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
    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<ErrorBoundary, bool>(nameof(HasError), defaultValue: false);

    public static readonly StyledProperty<string> ErrorMessageProperty =
        AvaloniaProperty.Register<ErrorBoundary, string>(nameof(ErrorMessage), defaultValue: "");

    private readonly RelayCommand _retryCommand;

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

    /// <summary>
    /// Command to clear error and retry (bound by template). Per docs: commands, not events.
    /// </summary>
    public RelayCommand RetryCommand => _retryCommand;

    public ErrorBoundary()
    {
        _retryCommand = new RelayCommand(ClearError);
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
