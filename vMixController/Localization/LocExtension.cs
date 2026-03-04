using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace vMixController.Localization
{
	[MarkupExtensionReturnType(typeof(string))]
	internal sealed class LocExtension : MarkupExtension
	{
		public LocExtension()		{
		}

		public LocExtension(string key)
		{
			Key = key;
		}

		public string Key { get; set; }

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (string.IsNullOrEmpty(Key))
				return string.Empty;

			var targetProvider = serviceProvider?.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
			if (targetProvider?.TargetObject is Setter)
				return LocalizationManager.Instance[Key] ?? Key;

			if (targetProvider?.TargetProperty is DependencyProperty)
			{
				var binding = new Binding($"[{Key}]")
				{
					Source = LocalizationManager.Instance,
					Mode = BindingMode.OneWay
				};

				return binding.ProvideValue(serviceProvider);
			}

			return LocalizationManager.Instance[Key] ?? Key;
		}
	}
}
