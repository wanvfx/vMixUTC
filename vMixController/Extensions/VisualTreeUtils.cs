using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace vMixController.Extensions
{
    public static class VisualTreeUtils
    {
        /// <summary>
        /// Looks for a child control within a parent by type
        /// </summary>
        public static List<T> FindChild<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            var foundChildren = new List<T>();
            if (parent == null)
                return foundChildren;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var childrenCount = VisualTreeHelper.GetChildrenCount(node);
                for (var i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(node, i);
                    if (child is T typedChild)
                        foundChildren.Add(typedChild);

                    queue.Enqueue(child);
                }
            }

            return foundChildren;
        }

        /// <summary>
        /// Рекурсивно находит все дочерние элементы, у которых есть свойство "IsCancelled",
        /// и устанавливает его в 'true'.
        /// </summary>
        /// <param name="parent">Родительский элемент, с которого начинается поиск (например, Window).</param>
        public static void SetIsCancelledOnAllChildren(this DependencyObject parent)
        {
            if (parent == null) return;

            // Обходим всех прямых потомков в визуальном дереве
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                // 1. Проверяем и устанавливаем свойство для текущего дочернего элемента
                TrySetIsCancelled(child);

                // 2. Рекурсивно вызываем этот же метод для потомков дочернего элемента
                SetIsCancelledOnAllChildren(child);
            }
        }

        /// <summary>
        /// Пытается установить свойство "IsCancelled" в 'true' для одного элемента.
        /// </summary>
        /// <param name="element">Элемент для проверки.</param>
        private static void TrySetIsCancelled(DependencyObject element)
        {
            if (element == null) return;

            var type = element.GetType();

            // Ищем свойство с именем "IsCancelled"
            var propertyInfo = type.GetProperty("IsCancelled");

            // Проверяем, что свойство:
            // 1. Существует (propertyInfo != null)
            // 2. Доступно для записи (CanWrite)
            // 3. Имеет тип bool (чтобы мы могли присвоить ему true)
            if (propertyInfo != null && propertyInfo.CanWrite && propertyInfo.PropertyType == typeof(bool))
            {
                try
                {
                    // Устанавливаем значение свойства в 'true'
                    propertyInfo.SetValue(element, true);
                }
                catch
                {
                    // Можно добавить логирование ошибок, если установка не удалась
                }
            }
        }
    }
}
