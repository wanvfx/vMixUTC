using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace vMixController.Localization
{
	internal sealed class LocalizationManager : INotifyPropertyChanged
	{
		private static readonly Lazy<LocalizationManager> _instance = new Lazy<LocalizationManager>(() => new LocalizationManager());

		private readonly ResourceManager _resourceManager;
		private CultureInfo _culture;

		private LocalizationManager()
		{
			_resourceManager = new ResourceManager("vMixController.Properties.Strings", typeof(LocalizationManager).Assembly);
		}

		public static LocalizationManager Instance => _instance.Value;

		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler CultureChanged;

		public CultureInfo Culture
		{
			get => _culture ?? CultureInfo.CurrentUICulture;
			set
			{
				if (Equals(_culture, value))
					return;

				_culture = value;
				ApplyCulture(_culture);

				OnPropertyChanged(nameof(Culture));
				OnPropertyChanged("Item[]");
				CultureChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public string this[string key]
		{
			get
			{
				if (string.IsNullOrEmpty(key))
					return string.Empty;

				var value = _resourceManager.GetString(key, Culture);
				return value ?? $"!{key}!";
			}
		}

		public void InitializeFromSettings()
		{
			var name = Properties.Settings.Default.UiCulture;
			if (!string.IsNullOrWhiteSpace(name))
			{
				SetCulture(name, persist: false);
				return;
			}

			ApplyCulture(CultureInfo.CurrentUICulture);
			OnPropertyChanged("Item[]");
		}

		public void SetCulture(string cultureName, bool persist = true)
		{
			if (string.IsNullOrWhiteSpace(cultureName))
				return;

			Culture = CultureInfo.GetCultureInfo(cultureName);

			if (persist)
			{
				Properties.Settings.Default.UiCulture = cultureName;
				Properties.Settings.Default.Save();
			}
		}

		private static void ApplyCulture(CultureInfo culture)
		{
			if (culture == null)
				return;

			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;

			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;
		}

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
