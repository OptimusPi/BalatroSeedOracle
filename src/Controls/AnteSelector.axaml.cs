using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BalatroSeedOracle.Controls
{
    public partial class AnteSelector : UserControl
    {
        private readonly List<CheckBox> _anteCheckBoxes = new();
        
        public event EventHandler<int[]>? SelectedAntesChanged;
        
        public AnteSelector()
        {
            InitializeComponent();
            InitializeAnteCheckBoxes();
            UpdateSummary();
        }
        
        private void InitializeAnteCheckBoxes()
        {
            // Find all ante checkboxes and add them to our list
            _anteCheckBoxes.Add(this.FindControl<CheckBox>("Ante1CheckBox")!);
            _anteCheckBoxes.Add(this.FindControl<CheckBox>("Ante2CheckBox")!);
            _anteCheckBoxes.Add(this.FindControl<CheckBox>("Ante3CheckBox")!);
            _anteCheckBoxes.Add(this.FindControl<CheckBox>("Ante4CheckBox")!);
            _anteCheckBoxes.Add(this.FindControl<CheckBox>("Ante5CheckBox")!);
            _anteCheckBoxes.Add(this.FindControl<CheckBox>("Ante6CheckBox")!);
            _anteCheckBoxes.Add(this.FindControl<CheckBox>("Ante7CheckBox")!);
            _anteCheckBoxes.Add(this.FindControl<CheckBox>("Ante8CheckBox")!);
            
            // Subscribe to check/uncheck events
            foreach (var checkBox in _anteCheckBoxes)
            {
                checkBox.IsCheckedChanged += (s, e) =>
                {
                    UpdateSummary();
                    FireSelectionChanged();
                };
            }
        }
        
        public int[] GetSelectedAntes()
        {
            var selectedAntes = new List<int>();
            for (int i = 0; i < _anteCheckBoxes.Count; i++)
            {
                if (_anteCheckBoxes[i].IsChecked == true)
                {
                    selectedAntes.Add(i + 1); // Antes are 1-indexed
                }
            }
            return selectedAntes.ToArray();
        }
        
        public void SetSelectedAntes(int[] antes)
        {
            // Clear all first
            foreach (var checkBox in _anteCheckBoxes)
            {
                checkBox.IsChecked = false;
            }
            
            // Set selected ones
            foreach (var ante in antes)
            {
                if (ante >= 1 && ante <= 8)
                {
                    _anteCheckBoxes[ante - 1].IsChecked = true;
                }
            }
            
            UpdateSummary();
        }
        
        private void UpdateSummary()
        {
            var selectedAntes = GetSelectedAntes();
            var summaryTextBlock = this.FindControl<TextBlock>("SummaryTextBlock")!;
            
            if (selectedAntes.Length == 0)
            {
                summaryTextBlock.Text = "Selected: None";
                summaryTextBlock.Foreground = Avalonia.Media.Brush.Parse("#FF4444");
            }
            else if (selectedAntes.Length == 8)
            {
                summaryTextBlock.Text = "Selected: All Antes";
                summaryTextBlock.Foreground = Avalonia.Media.Brush.Parse("#00FF88");
            }
            else
            {
                var anteList = string.Join(", ", selectedAntes);
                summaryTextBlock.Text = $"Selected: Antes {anteList}";
                summaryTextBlock.Foreground = Avalonia.Media.Brush.Parse("#00FF88");
            }
        }
        
        private void FireSelectionChanged()
        {
            SelectedAntesChanged?.Invoke(this, GetSelectedAntes());
        }
        
        // Event handlers for quick select buttons
        private void SelectAll_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var checkBox in _anteCheckBoxes)
            {
                checkBox.IsChecked = true;
            }
        }
        
        private void SelectNone_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var checkBox in _anteCheckBoxes)
            {
                checkBox.IsChecked = false;
            }
        }
        
        private void SelectEarly_Click(object? sender, RoutedEventArgs e)
        {
            // Clear all first
            foreach (var checkBox in _anteCheckBoxes)
            {
                checkBox.IsChecked = false;
            }
            
            // Select antes 1-3
            for (int i = 0; i < 3 && i < _anteCheckBoxes.Count; i++)
            {
                _anteCheckBoxes[i].IsChecked = true;
            }
        }
        
        private void SelectLate_Click(object? sender, RoutedEventArgs e)
        {
            // Clear all first
            foreach (var checkBox in _anteCheckBoxes)
            {
                checkBox.IsChecked = false;
            }
            
            // Select antes 4-8
            for (int i = 3; i < _anteCheckBoxes.Count; i++)
            {
                _anteCheckBoxes[i].IsChecked = true;
            }
        }
    }
}
