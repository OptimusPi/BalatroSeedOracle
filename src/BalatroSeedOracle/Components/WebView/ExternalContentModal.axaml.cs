#if !BROWSER
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Components.WebView
{
    public partial class ExternalContentModal : UserControl
    {
        private Control? _webView;
        private TextBox? _urlTextBox;
        private Button? _backButton;
        private Button? _goButton;

        public string? InitialUrl { get; set; }

        public ExternalContentModal()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            _webView = this.FindControl<Control>("WebViewControl");
            _urlTextBox = this.FindControl<TextBox>("UrlTextBox");
            _backButton = this.FindControl<Button>("BackButton");
            _goButton = this.FindControl<Button>("GoButton");

            if (_urlTextBox != null)
            {
                _urlTextBox.KeyDown += OnUrlTextBoxKeyDown;
            }

            if (_goButton != null)
            {
                _goButton.Click += OnGoButtonClick;
            }

            if (_backButton != null)
            {
                _backButton.Click += OnBackButtonClick;
            }

            // Load initial URL if provided
            if (!string.IsNullOrEmpty(InitialUrl))
            {
                NavigateToUrl(InitialUrl);
            }
        }

        private void OnUrlTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToCurrentUrl();
            }
        }

        private void OnGoButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            NavigateToCurrentUrl();
        }

        private void OnBackButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // WebView GoBack functionality would need to be implemented via reflection
            // or proper WebView package integration
            DebugLogger.Log("ExternalContentModal", "Go back requested");
        }

        private void NavigateToCurrentUrl()
        {
            if (_urlTextBox != null)
            {
                var url = _urlTextBox.Text?.Trim();
                if (!string.IsNullOrEmpty(url))
                {
                    NavigateToUrl(url);
                }
            }
        }

        public void NavigateToUrl(string url)
        {
            try
            {
                // Ensure URL has protocol
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                // WebView navigation would need to be implemented via reflection
                // or proper WebView package integration
                if (_webView != null)
                {
                    var sourceProperty = _webView.GetType().GetProperty("Source");
                    sourceProperty?.SetValue(_webView, new Uri(url));
                }

                if (_urlTextBox != null && _urlTextBox.Text != url)
                {
                    _urlTextBox.Text = url;
                }

                DebugLogger.Log("ExternalContentModal", $"Navigating to: {url}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ExternalContentModal", $"Failed to navigate to {url}: {ex.Message}");
            }
        }
    }
}
#endif
