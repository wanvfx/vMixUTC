using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace vMixController.Extensions
{
    public static class GridColumnMargin
    {
        public static readonly DependencyProperty ColumnMarginProperty =
            DependencyProperty.RegisterAttached(
                "ColumnMargin",
                typeof(Thickness),
                typeof(GridColumnMargin),
                new FrameworkPropertyMetadata(new Thickness(0), FrameworkPropertyMetadataOptions.AffectsMeasure, OnColumnMarginChanged));

        public static Thickness GetColumnMargin(DependencyObject obj)
        {
            return (Thickness)obj.GetValue(ColumnMarginProperty);
        }

        public static void SetColumnMargin(DependencyObject obj, Thickness value)
        {
            obj.SetValue(ColumnMarginProperty, value);
        }

        private static void OnColumnMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Grid grid)
            {
                ApplyColumnMarginToChildren(grid);
                grid.Loaded += (sender, args) => ApplyColumnMarginToChildren(grid); // Ensure it's applied after layout
            }
        }

        private static void ApplyColumnMarginToChildren(Grid grid)
        {
            Thickness margin = GetColumnMargin(grid);
            grid.Margin = new Thickness(-margin.Left, -margin.Top, -margin.Right, -margin.Bottom);
            foreach (UIElement child in LogicalTreeHelper.GetChildren(grid).OfType<UIElement>())
            {
                if (child is FrameworkElement element)
                {
                    int column = Grid.GetColumn(element);
                    

                    // Modify the margin based on the column.  This is where your logic goes.
                    Thickness newMargin = new Thickness(
                        margin.Left + (column == 0 ? 0 : margin.Left), // Example: Add left margin if not first column
                        margin.Top,
                        margin.Right + (column == grid.ColumnDefinitions.Count - 1 ? 0 : margin.Right), //Example:  Add right margin if not last column
                        margin.Bottom
                    );

                    element.Margin = newMargin;
                }
            }
        }
    }
}
