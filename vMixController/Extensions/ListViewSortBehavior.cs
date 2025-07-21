using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace vMixController.Extensions
{

    public static class ListViewSortBehavior
    {
        private static readonly ConditionalWeakTable<ListView, SortState> SortStates = new ConditionalWeakTable<ListView, SortState>();

        #region Attached Properties

        public static readonly DependencyProperty IsSortableProperty =
            DependencyProperty.RegisterAttached("IsSortable", typeof(bool), typeof(ListViewSortBehavior), new PropertyMetadata(false, OnIsSortableChanged));

        public static void SetIsSortable(ListView obj, bool value) { obj.SetValue(IsSortableProperty, value); }
        public static bool GetIsSortable(ListView obj) { return (bool)obj.GetValue(IsSortableProperty); }

        public static readonly DependencyProperty SortableItemsSourceProperty =
            DependencyProperty.RegisterAttached("SortableItemsSource", typeof(IEnumerable), typeof(ListViewSortBehavior), new PropertyMetadata(null, OnSortableItemsSourceChanged));

        public static void SetSortableItemsSource(ListView obj, IEnumerable value) { obj.SetValue(SortableItemsSourceProperty, value); }
        public static IEnumerable GetSortableItemsSource(ListView obj) { return (IEnumerable)obj.GetValue(SortableItemsSourceProperty); }

        public static readonly DependencyProperty DefaultSortPropertyProperty =
            DependencyProperty.RegisterAttached("DefaultSortProperty", typeof(string), typeof(ListViewSortBehavior), new PropertyMetadata(null));

        public static void SetDefaultSortProperty(ListView obj, string value) { obj.SetValue(DefaultSortPropertyProperty, value); }
        public static string GetDefaultSortProperty(ListView obj) { return (string)obj.GetValue(DefaultSortPropertyProperty); }

        public static readonly DependencyProperty DefaultSortDirectionProperty =
            DependencyProperty.RegisterAttached("DefaultSortDirection", typeof(ListSortDirection), typeof(ListViewSortBehavior), new PropertyMetadata(ListSortDirection.Ascending));

        public static void SetDefaultSortDirection(ListView obj, ListSortDirection value) { obj.SetValue(DefaultSortDirectionProperty, value); }
        public static ListSortDirection GetDefaultSortDirection(ListView obj) { return (ListSortDirection)obj.GetValue(DefaultSortDirectionProperty); }

        #endregion

        #region Callbacks and Logic

        private static void OnIsSortableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Используем классическую проверку типа
            ListView listView = d as ListView;
            if (listView == null) return;

            listView.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
            if ((bool)e.NewValue)
            {
                listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
            }
        }

        private static void OnSortableItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ListView listView = d as ListView;
            if (listView == null) return;

            var newItemsSource = e.NewValue as IEnumerable;
            if (newItemsSource == null)
            {
                listView.ItemsSource = null;
                return;
            }

            var view = CollectionViewSource.GetDefaultView(newItemsSource);

            // .NET Framework 4.7.2 COMPATIBILITY: Ручная реализация GetOrCreateValue
            SortState state;
            if (!SortStates.TryGetValue(listView, out state))
            {
                state = new SortState();
                SortStates.Add(listView, state);
            }
            state.View = view;

            listView.ItemsSource = view;

            var defaultSortProperty = GetDefaultSortProperty(listView);
            if (!string.IsNullOrEmpty(defaultSortProperty))
            {
                var defaultDirection = GetDefaultSortDirection(listView);
                state.LastSortProperty = defaultSortProperty;
                state.LastSortDirection = defaultDirection;

                ApplySort(state);
                UpdateSortAdorner(listView, state);
            }

            EnableLiveSorting(listView, state.View);
        }

        private static void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            // Используем классическую проверку типа
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Tag == null) return;

            var listView = (ListView)sender;

            // .NET Framework 4.7.2 COMPATIBILITY: Ручная реализация GetOrCreateValue
            SortState state;
            if (!SortStates.TryGetValue(listView, out state))
            {
                state = new SortState();
                SortStates.Add(listView, state);
            }

            if (state.View == null) return;

            string propertyName = headerClicked.Tag.ToString();

            if (propertyName == state.LastSortProperty)
            {
                state.LastSortDirection = state.LastSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                state.LastSortProperty = propertyName;
                state.LastSortDirection = ListSortDirection.Ascending;
            }

            ApplySort(state);
            UpdateSortAdorner(listView, state);
        }

        private static void ApplySort(SortState state)
        {
            state.View.SortDescriptions.Clear();
            if (!string.IsNullOrEmpty(state.LastSortProperty))
            {
                state.View.SortDescriptions.Add(new SortDescription(state.LastSortProperty, state.LastSortDirection));
            }
        }

        private static void EnableLiveSorting(ListView listView, ICollectionView view)
        {
            // Пытаемся получить интерфейс ICollectionViewLiveShaping
            ICollectionViewLiveShaping liveView = view as ICollectionViewLiveShaping;
            if (liveView == null || !liveView.CanChangeLiveSorting)
            {
                return; // Этот view не поддерживает Live Sorting
            }

            // Включаем Live Sorting
            liveView.IsLiveSorting = true;

            // Получаем список свойств для отслеживания из нашего нового attached property
            string propertiesToWatch = GetDefaultSortProperty(listView);
            if (string.IsNullOrEmpty(propertiesToWatch))
            {
                return;
            }

            // Очищаем старые и добавляем новые свойства
            liveView.LiveSortingProperties.Clear();
            foreach (var prop in propertiesToWatch.Split(','))
            {
                var trimmedProp = prop.Trim();
                if (!string.IsNullOrEmpty(trimmedProp))
                {
                    liveView.LiveSortingProperties.Add(trimmedProp);
                }
            }
        }

        private static void UpdateSortAdorner(ListView listView, SortState state)
        {
            var newHeader = FindColumnHeaderByTag(listView, state.LastSortProperty);

            if (state.LastHeaderClicked != null && state.LastHeaderClicked != newHeader)
            {
                state.LastHeaderClicked.Column.HeaderTemplate = null;
            }

            if (newHeader != null)
            {
                string templateKey = state.LastSortDirection == ListSortDirection.Ascending ? "HeaderTemplateArrowUp" : "HeaderTemplateArrowDown";
                newHeader.Column.HeaderTemplate = listView.TryFindResource(templateKey) as DataTemplate;
                state.LastHeaderClicked = newHeader;
            }
        }

        private static GridViewColumnHeader FindColumnHeaderByTag(ListView listView, string tag)
        {
            GridView gridView = listView.View as GridView;
            if (gridView == null) return null;

            return gridView.Columns
                .Select(c => c.Header as GridViewColumnHeader)
                .FirstOrDefault(h => h != null && h.Tag != null && h.Tag.ToString() == tag);
        }

        #endregion

        private class SortState
        {
            public ICollectionView View { get; set; }
            public string LastSortProperty { get; set; }
            public ListSortDirection LastSortDirection { get; set; }
            public GridViewColumnHeader LastHeaderClicked { get; set; }
        }
    }
}