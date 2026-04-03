// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Launcher.ViewModels;

namespace Launcher.Controls
{
    /// <summary>
    /// A custom Panel that arranges ParameterViewModel items in a WPF Grid layout
    /// based on their GridRow/GridColumn properties. Used for Freeform pages.
    /// 
    /// Layout modes:
    /// 1. Explicit: Children have Grid.Row/Grid.Column set via ItemContainerStyle
    /// 2. Auto-fill: No positions set — children fill left-to-right, top-to-bottom
    ///    using RequestedGridColumns from the GenericFormViewModel DataContext
    /// 3. Fallback: Single-column vertical stack when GridColumns = 0/1 and no positions
    /// </summary>
    public class FreeformGridPanel : Grid
    {
        private int _lastChildCount = -1;
        private int _lastGridColumns = -1;

        protected override Size MeasureOverride(Size constraint)
        {
            UpdateGridDefinitions();
            return base.MeasureOverride(constraint);
        }

        /// <summary>
        /// Gets the requested grid column count from the DataContext.
        /// Checks ParameterCategoryGroup first (tabbed layout), then GenericFormViewModel (non-tabbed).
        /// Returns 0 if not available.
        /// </summary>
        private int GetRequestedGridColumns()
        {
            // Walk up the visual tree to find a DataContext with RequestedGridColumns
            var fe = this as FrameworkElement;
            while (fe != null)
            {
                if (fe.DataContext is ParameterCategoryGroup catGroup && catGroup.RequestedGridColumns > 0)
                {
                    return catGroup.RequestedGridColumns;
                }
                if (fe.DataContext is GenericFormViewModel formVm)
                {
                    return formVm.RequestedGridColumns;
                }
                fe = fe.Parent as FrameworkElement;
            }
            return 0;
        }

        private void UpdateGridDefinitions()
        {
            if (Children.Count == 0) return;

            int gridColumns = GetRequestedGridColumns();

            // Check if anything changed to avoid unnecessary rebuilds
            if (_lastChildCount == Children.Count && _lastGridColumns == gridColumns)
                return;
            _lastChildCount = Children.Count;
            _lastGridColumns = gridColumns;

            // Scan children for explicit grid positions
            int maxRow = 0;
            int maxCol = 0;
            bool hasGridPositions = false;
            var unpositioned = new List<int>(); // indices of children without explicit positions

            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                int row = GetRow(child);
                int col = GetColumn(child);
                if (row > 0 || col > 0)
                {
                    hasGridPositions = true;
                    int rs = GetRowSpan(child);
                    int cs = GetColumnSpan(child);
                    int rowEnd = row + rs - 1;
                    int colEnd = col + cs - 1;
                    if (rowEnd > maxRow) maxRow = rowEnd;
                    if (colEnd > maxCol) maxCol = colEnd;
                }
                else
                {
                    unpositioned.Add(i);
                }
            }

            if (!hasGridPositions)
            {
                // No explicit positions — auto-fill mode
                int cols = gridColumns > 0 ? gridColumns : 1;

                if (cols <= 1)
                {
                    // Single-column vertical stack
                    RowDefinitions.Clear();
                    ColumnDefinitions.Clear();
                    ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    for (int i = 0; i < Children.Count; i++)
                    {
                        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        SetRow(Children[i], i);
                        SetColumn(Children[i], 0);
                    }
                }
                else
                {
                    // Multi-column auto-fill: left-to-right, top-to-bottom
                    int rows = (int)Math.Ceiling((double)Children.Count / cols);
                    RowDefinitions.Clear();
                    ColumnDefinitions.Clear();
                    for (int r = 0; r < rows; r++)
                        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    for (int c = 0; c < cols; c++)
                        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    for (int i = 0; i < Children.Count; i++)
                    {
                        int col = i % cols;
                        SetRow(Children[i], i / cols);
                        SetColumn(Children[i], col);
                        // Add horizontal gap between columns (16px right margin, except last column)
                        if (Children[i] is FrameworkElement fe)
                        {
                            var m = fe.Margin;
                            fe.Margin = new Thickness(col > 0 ? 8 : 0, m.Top, col < cols - 1 ? 8 : 0, m.Bottom);
                        }
                    }
                }
                return;
            }

            // Explicit positions mode — some or all children have Row/Column set
            int neededCols = Math.Max(maxCol + 1, gridColumns > 0 ? gridColumns : maxCol + 1);
            int neededRows = maxRow + 1;

            // Auto-assign unpositioned children to next available cells after the positioned ones
            if (unpositioned.Count > 0)
            {
                // Build occupancy grid
                var occupied = new HashSet<string>();
                for (int i = 0; i < Children.Count; i++)
                {
                    if (unpositioned.Contains(i)) continue;
                    var child = Children[i];
                    int r = GetRow(child);
                    int c = GetColumn(child);
                    int cs = GetColumnSpan(child);
                    int rs = GetRowSpan(child);
                    for (int dr = 0; dr < rs; dr++)
                        for (int dc = 0; dc < cs; dc++)
                            occupied.Add($"{r + dr},{c + dc}");
                }

                int nextRow = 0, nextCol = 0;
                foreach (int idx in unpositioned)
                {
                    // Find next unoccupied cell
                    while (occupied.Contains($"{nextRow},{nextCol}"))
                    {
                        nextCol++;
                        if (nextCol >= neededCols) { nextCol = 0; nextRow++; }
                    }
                    SetRow(Children[idx], nextRow);
                    SetColumn(Children[idx], nextCol);
                    occupied.Add($"{nextRow},{nextCol}");
                    if (nextRow + 1 > neededRows) neededRows = nextRow + 1;
                    nextCol++;
                    if (nextCol >= neededCols) { nextCol = 0; nextRow++; }
                }
            }

            // Create row/column definitions
            if (RowDefinitions.Count != neededRows)
            {
                RowDefinitions.Clear();
                for (int i = 0; i < neededRows; i++)
                    RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            if (ColumnDefinitions.Count != neededCols)
            {
                ColumnDefinitions.Clear();
                for (int i = 0; i < neededCols; i++)
                    ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
        }
    }
}
