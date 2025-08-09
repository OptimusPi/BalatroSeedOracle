using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Oracle.Services;

namespace Oracle.Controls
{
    /// <summary>
    /// Base class for all item configuration popups
    /// </summary>
    public abstract class ItemConfigPopupBase : UserControl
    {
        // Events
        public event EventHandler<ItemConfigEventArgs>? ConfigApplied;
        public event EventHandler? DeleteRequested;
        public event EventHandler? Cancelled;

        // Protected fields for derived classes
        protected string _itemKey = "";
        protected string _itemName = "";
        protected string _itemCategory = "";
        
        // Public property to get the item key
        public string ItemKey => _itemKey;

        // Common UI elements
        protected TextBlock? ItemNameText;
        protected Button? ApplyButton;
        protected Button? DeleteButton;
        protected Button? CancelButton;

        protected ItemConfigPopupBase()
        {
            // Base initialization will be called by derived classes
        }

        /// <summary>
        /// Initialize the item with its key and name
        /// </summary>
        public virtual void SetItem(string itemKey, string itemName, ItemConfig? existingConfig = null)
        {
            _itemKey = itemKey;
            _itemName = itemName;

            // Extract category from key
            var parts = itemKey.Split(':');
            if (parts.Length >= 1)
            {
                _itemCategory = parts[0];
            }

            // Update item name display
            if (ItemNameText != null)
            {
                ItemNameText.Text = itemName;
            }

            // Let derived class handle specific configuration
            LoadConfiguration(existingConfig);
        }

        /// <summary>
        /// Load existing configuration - to be implemented by derived classes
        /// </summary>
        protected abstract void LoadConfiguration(ItemConfig? config);

        /// <summary>
        /// Build configuration from current UI state - to be implemented by derived classes
        /// </summary>
        protected abstract ItemConfig BuildConfiguration();

        /// <summary>
        /// Common Apply button handler
        /// </summary>
        protected virtual void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            var config = BuildConfiguration();
            config.ItemKey = _itemKey;
            ConfigApplied?.Invoke(this, new ItemConfigEventArgs { Config = config });
        }

        /// <summary>
        /// Common Delete button handler
        /// </summary>
        protected virtual void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Common Cancel button handler
        /// </summary>
        protected virtual void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Create the popup container with common styling
        /// </summary>
        protected Border CreatePopupContainer(Control content)
        {
            return new Border
            {
                MinWidth = 282,
                MaxWidth = 322,
                Background = Application.Current?.FindResource("ItemConfigMediumBg") as IBrush ?? new SolidColorBrush(Color.Parse("#2a2a2a")),
                BorderBrush = Application.Current?.FindResource("ItemConfigDarkBg") as IBrush ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Child = content
            };
        }

        /// <summary>
        /// Create the header with item name
        /// </summary>
        protected Border CreateHeader()
        {
            ItemNameText = new TextBlock
            {
                FontFamily = Application.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                FontSize = 16,

                Foreground = Application.Current?.FindResource("Gold") as IBrush ?? Brushes.Gold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            return new Border
            {
                Background = Application.Current?.FindResource("ItemConfigDarkBg") as IBrush ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6),
                Margin = new Thickness(-16, -16, -16, 0),
                Child = ItemNameText
            };
        }

        /// <summary>
        /// Create the button bar with Apply/Delete/Cancel
        /// </summary>
        protected Grid CreateButtonBar()
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 6, 0, 0),
                ColumnDefinitions = new ColumnDefinitions("*,*,*")
            };

            // Apply button
            ApplyButton = CreateStyledButton("APPLY", "#1a5f1a", "#4CAF50", 0);
            ApplyButton.Click += OnApplyClick;

            // Delete button
            DeleteButton = CreateStyledButton("DELETE", "#5f1a1a", "#f44336", 1);
            DeleteButton.Click += OnDeleteClick;

            // Cancel button
            CancelButton = CreateStyledButton("CANCEL", "#1a1a1a", "#9E9E9E", 2);
            CancelButton.Click += OnCancelClick;

            grid.Children.Add(ApplyButton);
            grid.Children.Add(DeleteButton);
            grid.Children.Add(CancelButton);

            return grid;
        }

        private Button CreateStyledButton(string text, string bgColor, string borderColor, int column)
        {
            var button = new Button
            {
                Height = 32,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Margin = column == 0 ? new Thickness(0, 0, 4, 0) :
                         column == 1 ? new Thickness(2, 0) :
                         new Thickness(4, 0, 0, 0)
            };

            button.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((btn, scope) =>
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.Parse(bgColor)),
                    BorderBrush = new SolidColorBrush(Color.Parse(borderColor)),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(4),
                    Child = new TextBlock
                    {
                        Text = text,
                        FontFamily = Application.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                        FontSize = 12,
        
                        Foreground = new SolidColorBrush(Color.Parse(borderColor)),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    }
                };
                return border;
            });

            Grid.SetColumn(button, column);
            return button;
        }
    }
}
