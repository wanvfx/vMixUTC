using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using vMixController.Widgets;

namespace vMixController.Controls
{
    public class TypeTemplateSelector : DataTemplateSelector
    {
        private readonly Dictionary<Type, DataTemplate> _templateCache = new Dictionary<Type, DataTemplate>();
        private readonly HashSet<Type> _templateMissCache = new HashSet<Type>();

        public int Page { get; set; } = 0;
        public string Suffix { get; set; } = "";

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (item == null || element == null)
                return null;

            /*if (item is vMixControl widget && !widget.IsVisualReady)
                return element.TryFindResource("WidgetSkeletonTemplate") as DataTemplate;*/

            var itemType = item.GetType();
            if (_templateCache.TryGetValue(itemType, out var cachedTemplate))
                return cachedTemplate;

            if (_templateMissCache.Contains(itemType))
                return null;

            var template = element.TryFindResource(itemType.Name + Suffix) as DataTemplate;
            if (template != null)
            {
                _templateCache[itemType] = template;
                return template;
            }

            var baseType = itemType.BaseType;
            if (baseType == null)
            {
                _templateMissCache.Add(itemType);
                return null;
            }

            var baseTemplate = element.TryFindResource(baseType.Name + Suffix) as DataTemplate;
            if (baseTemplate != null)
                _templateCache[itemType] = baseTemplate;
            else
                _templateMissCache.Add(itemType);

            return baseTemplate;
        }
    }
}
