using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Oracle.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oracle.Components
{
    public partial class ResponsiveGrid : UserControl
    {
        private Grid _mainGrid;
        private readonly List<Control> _children = new();

        // Breakpoint definitions
        private const int XtraXtraSmallBreakpoint = 525;
        private const int XtraSmallBreakpoint = 750;
        private const int SmallBreakpoint = 900;
        private const int MediumBreakpoint = 1050;
        private const int LargeBreakpoint = 1200;
        private const int XtraLargeBreakpoint = 1350;
        private const int XtraXtraLargeBreakpoint = 1575;
        private const string XtraXtraSmallBreakpointClass = "xxs";
        private const string XtraSmallBreakpointClass = "xs";
        private const string SmallBreakpointClass = "s";
        private const string MediumBreakpointClass = "md";
        private const string LargeBreakpointClass = "lg";
        private const string XtraLargeBreakpointClass = "xl";
        private const string XtraXtraLargeBreakpointClass = "xxl";

        private string _currentBreakpoint = LargeBreakpointClass;

        public ResponsiveGrid()
        {
            InitializeComponent();
            _mainGrid = this.FindControl<Grid>("MainGrid")!;

            // Listen for size changes
            this.SizeChanged += OnSizeChanged;

            // Set initial breakpoint
            UpdateBreakpoint(LargeBreakpoint);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void AddChild(Control child)
        {
            _children.Add(child);
            ArrangeChildren();
        }

        public void AddChildren(IEnumerable<Control> children)
        {
            _children.AddRange(children);
            ArrangeChildren();
        }

        public void ClearChildren()
        {
            _children.Clear();
            _mainGrid.Children.Clear();
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateBreakpoint(e.NewSize.Width);
        }

        private void UpdateBreakpoint(double width)
        {
            string newBreakpoint = width switch
            {
                < XtraXtraSmallBreakpoint => XtraXtraSmallBreakpointClass,
                < XtraSmallBreakpoint => XtraSmallBreakpointClass,
                < SmallBreakpoint => SmallBreakpointClass,
                < MediumBreakpoint => MediumBreakpointClass,
                < LargeBreakpoint => LargeBreakpointClass,
                < XtraLargeBreakpoint => XtraLargeBreakpointClass,
                < XtraXtraLargeBreakpoint => XtraXtraLargeBreakpointClass,
                _ => XtraLargeBreakpointClass
            };

            if (newBreakpoint != _currentBreakpoint)
            {
                // Remove old breakpoint class
                this.Classes.Remove(_currentBreakpoint);

                // Add new breakpoint class
                _currentBreakpoint = newBreakpoint;
                this.Classes.Add(_currentBreakpoint);

                // Rearrange children
                ArrangeChildren();
            }
        }

        private void ArrangeChildren()
        {
            _mainGrid.Children.Clear();
            _mainGrid.RowDefinitions.Clear();

            if (!_children.Any()) return;

            int columnsPerRow = GetColumnsPerRow();
            int totalRows = (int)Math.Ceiling((double)_children.Count / columnsPerRow);

            // Create row definitions
            for (int i = 0; i < totalRows; i++)
            {
                _mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            // Update column definitions based on breakpoint
            UpdateColumnDefinitions(columnsPerRow);

            // Place children in grid
            for (int i = 0; i < _children.Count; i++)
            {
                int row = i / columnsPerRow;
                int col = i % columnsPerRow;

                var child = _children[i];
                Grid.SetRow(child, row);
                Grid.SetColumn(child, col);

                _mainGrid.Children.Add(child);
            }
        }

        private int GetColumnsPerRow()
        {
            return _currentBreakpoint switch
            {
                XtraXtraSmallBreakpointClass => 4,
                XtraSmallBreakpointClass => 5,
                SmallBreakpointClass => 6,
                MediumBreakpointClass => 7,
                LargeBreakpointClass => 8,
                XtraLargeBreakpointClass => 9,
                XtraXtraLargeBreakpointClass => 8,
                _ => 6
            };
        }

        private void UpdateColumnDefinitions(int columnCount)
        {
            _mainGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < columnCount; i++)
            {
                _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }
        }
    }
}