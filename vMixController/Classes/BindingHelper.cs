using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using vMixController.PropertiesControls;

namespace vMixController.Classes
{
    public static class BindingHelper
    {
        // Кэш для хранения уже найденных свойств для каждого типа.
        // Это КРАЙНЕ ВАЖНО для производительности!
        private static readonly Dictionary<Type, List<DependencyProperty>> PropertyCache = new Dictionary<Type, List<DependencyProperty>>();

        /// <summary>
        /// Находит все DependencyProperty для указанного типа с помощью рефлексии.
        /// </summary>
        private static List<DependencyProperty> GetDependencyProperties(Type type)
        {
            // Проверяем, есть ли результат в кэше
            if (PropertyCache.TryGetValue(type, out var cachedProperties))
            {
                return cachedProperties;
            }

            var properties = new List<DependencyProperty>();

            // BindingFlags.FlattenHierarchy позволяет найти статические поля из базовых классов
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(DependencyProperty))
                {
                    properties.Add((DependencyProperty)field.GetValue(null));
                }
            }

            // Сохраняем результат в кэш
            PropertyCache[type] = properties;
            return properties;
        }

        /// <summary>
        /// Рекурсивно обходит визуальное дерево, находит все DependencyProperty
        /// для каждого элемента и обновляет явные (Explicit) привязки.
        /// </summary>
        public static void UpdateAllExplicitSources(this DependencyObject parent)
        {
            if (parent == null) return;

            // Получаем все возможные DP для типа этого элемента
            var dependencyProperties = GetDependencyProperties(parent.GetType());

            foreach (var dp in dependencyProperties)
            {
                //Костыль для обновления коллекций
                if (dp.PropertyType == typeof(ObservableCollection<>))
                {
                    var value = parent.GetValue(dp);
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    MethodInfo method = dp.PropertyType.GetMethod(
                        "OnCollectionChanged",
                        BindingFlags.Instance | BindingFlags.NonPublic // Указываем, что метод непубличный и принадлежит экземпляру
                    );
                    method?.Invoke(value, new object[] { args });

                    //((IRefreshable)parent.GetValue(dp)).Refresh();
                }

                // Для каждого DP проверяем, есть ли на этом экземпляре элемента активная привязка
                var bindingExpression = BindingOperations.GetBindingExpression(parent, dp);

                if (bindingExpression != null && bindingExpression.ParentBinding.UpdateSourceTrigger == UpdateSourceTrigger.Explicit)
                {
                    bindingExpression.UpdateSource();
                }
                else
                {
                    // Также проверяем MultiBinding
                    var multiBindingExpression = BindingOperations.GetMultiBindingExpression(parent, dp);

                    if (multiBindingExpression != null && multiBindingExpression.ParentMultiBinding.UpdateSourceTrigger == UpdateSourceTrigger.Explicit)
                    {
                        multiBindingExpression.UpdateSource();
                    }
                }
            }

            // Рекурсивно спускаемся к дочерним элементам
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                if (VisualTreeHelper.GetChild(parent, i) is DependencyObject child)
                {
                    child.UpdateAllExplicitSources();
                }
            }
        }
    }
}
