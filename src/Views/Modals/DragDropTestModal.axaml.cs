using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class DragDropTestModal : UserControl
    {
        private ObservableCollection<string> _droppedItems = new ObservableCollection<string>();

        public event EventHandler? BackRequested;

        public DragDropTestModal()
        {
            InitializeComponent();

            // Bind ItemsControl to the ObservableCollection
            if (DroppedItems != null)
            {
                DroppedItems.ItemsSource = _droppedItems;
            }

            if (DropZone != null)
            {
                DropZone.AddHandler(
                    DragDrop.DragOverEvent,
                    OnDropZoneDragOver,
                    RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
                    handledEventsToo: true
                );
                DropZone.AddHandler(
                    DragDrop.DropEvent,
                    OnDropZoneDrop,
                    RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
                    handledEventsToo: true
                );
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnBackClick(object? sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnDropZoneDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("balatro-item"))
                e.DragEffects &= DragDropEffects.Move;
            else
                e.DragEffects = DragDropEffects.None;

            e.Handled = true;
        }

        private void OnDropZoneDrop(object? sender, DragEventArgs e)
        {
            if (!e.Data.Contains("balatro-item"))
                return;

            var payload = e.Data.Get("balatro-item") as string;
            if (payload == null)
                return;

            _droppedItems.Add(payload);
            if (DropStatus != null)
            {
                DropStatus.Text = $"Dropped {payload}";
            }
            e.Handled = true;
        }
    }
}
