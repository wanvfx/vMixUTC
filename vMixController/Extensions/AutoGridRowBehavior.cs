using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace vMixController.Extensions
{
    public static class AutoGridRowBehavior
    {
        // Attached Property для установки отступов между элементами
        public static readonly DependencyProperty ItemMarginProperty =
            DependencyProperty.RegisterAttached(
                "ItemMargin",
                typeof(Thickness),
                typeof(AutoGridRowBehavior),
                new PropertyMetadata(new Thickness(0), OnItemMarginChanged));

        public static Thickness GetItemMargin(DependencyObject obj)
        {
            return (Thickness)obj.GetValue(ItemMarginProperty);
        }

        public static void SetItemMargin(DependencyObject obj, Thickness value)
        {
            obj.SetValue(ItemMarginProperty, value);
        }

        private static void OnItemMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Grid grid)
            {
                ApplyLayout(grid);
            }
        }

        // Attached Property для активации поведения
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(AutoGridRowBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Grid grid)
            {
                if ((bool)e.NewValue)
                {
                    // Прикрепляем обработчик к событию Loaded, чтобы гарантировать, что все дочерние элементы уже добавлены
                    // и ColumnDefinitions уже определены.
                    grid.Loaded += Grid_Loaded;
                }
                else
                {
                    grid.Loaded -= Grid_Loaded;
                }
            }
        }

        private static void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                grid.Loaded -= Grid_Loaded; // Отписываемся от события Loaded
                ApplyLayout(grid);
            }
        }

        private static void ApplyLayout(Grid grid)
        {
            // Если поведение не включено, или нет колонок, или нет дочерних элементов, ничего не делаем.
            if (!GetIsEnabled(grid) || grid.ColumnDefinitions.Count == 0 || grid.Children.Count == 0)
            {
                return;
            }

            int columns = grid.ColumnDefinitions.Count;
            Thickness itemMargin = GetItemMargin(grid);

            // Очищаем существующие определения строк
            grid.RowDefinitions.Clear();

            // Распределяем дочерние элементы
            int currentRow = 0;
            int currentColumn = 0;

            foreach (UIElement child in grid.Children)
            {
                // Если это первый элемент в новой строке, добавляем RowDefinition
                // Или если currentRow больше текущего количества RowDefinitions, добавляем новую строку.
                if (currentColumn == 0 || grid.RowDefinitions.Count <= currentRow)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                // Устанавливаем Grid.Row и Grid.Column для дочернего элемента
                Grid.SetRow(child, currentRow);
                Grid.SetColumn(child, currentColumn);

                // Устанавливаем Margin для дочернего элемента, если он является FrameworkElement
                if (child is FrameworkElement frameworkElement)
                {
                    frameworkElement.Margin = itemMargin;
                }

                currentColumn++;
                if (currentColumn >= columns)
                {
                    currentColumn = 0;
                    currentRow++;
                }
            }
        }

        // Дополнительный метод для принудительного обновления раскладки
        public static void UpdateLayout(Grid grid)
        {
            if (GetIsEnabled(grid))
            {
                ApplyLayout(grid);
            }
        }
    }
}
