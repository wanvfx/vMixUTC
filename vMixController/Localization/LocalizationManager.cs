using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace vMixController.Localization
{
    internal sealed class LocalizationManager : INotifyPropertyChanged
    {
        private static readonly Lazy<LocalizationManager> _instance = new Lazy<LocalizationManager>(() => new LocalizationManager());
        public static CultureInfo[] Locales { get; private set; }

        private readonly ResourceManager _resourceManager;
        private CultureInfo _culture;
        private static string _defaultLocale = "en-US";

        private static Dictionary<string, Dictionary<string, string>> _userLocales = new Dictionary<string, Dictionary<string, string>>();

        private LocalizationManager()
        {
            _resourceManager = new ResourceManager("vMixController.Properties.Strings", typeof(LocalizationManager).Assembly);

            var culture = new CultureInfo(_defaultLocale);

            var resourceSet = _resourceManager.GetResourceSet(culture, true, true);


            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var userLocales = Path.Combine(baseDir, "UserLocales");
            if (!Directory.Exists(userLocales))
            {
                Directory.CreateDirectory(userLocales);
                Dictionary<string, string> enLocale = new Dictionary<string, string>();
                foreach (DictionaryEntry entry in resourceSet)
                {
                    enLocale.Add((string)entry.Key, (string)entry.Value);
                }
                File.WriteAllText(Path.Combine(userLocales, _defaultLocale + ".json"), JsonSerializer.Serialize(enLocale, new JsonSerializerOptions() { WriteIndented = true }));
            }

            Locales = GetAvailableCultures().ToArray();
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
                if (string.IsNullOrEmpty(value))
                {
                    Dictionary<string, string> locale = null;
                    if (_userLocales.TryGetValue(Culture.Name, out locale))
                        locale.TryGetValue(key, out value);
                }
                var valueEn = _resourceManager.GetString(key, CultureInfo.GetCultureInfo(_defaultLocale));
                return (string.IsNullOrEmpty(value) ? valueEn : value) ?? $"!{key}!";
            }
        }

        public string GetKey(string value, string keypart = "")
        {
            foreach (DictionaryEntry entry in _resourceManager.GetResourceSet(Culture, false, true))
                if (entry.Value?.ToString() == value && entry.Key.ToString().StartsWith(keypart))
                    return value;
            return null;
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

        public static List<CultureInfo> GetAvailableCultures()
        {
            var result = new List<CultureInfo>();

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            //Default culture
            result.Add(CultureInfo.GetCultureInfo(_defaultLocale));

            foreach (var dir in Directory.GetDirectories(baseDir))
            {
                var name = Path.GetFileName(dir);

                try
                {
                    var culture = CultureInfo.GetCultureInfo(name);

                    var resourceFile = Path.Combine(dir, "vMixController.resources.dll");
                    if (File.Exists(resourceFile))
                    {
                        result.Add(culture);
                    }
                }
                catch (CultureNotFoundException)
                {
                    // не culture Ч игнорируем
                }
            }

            var userLocales = Path.Combine(baseDir, "UserLocales");
            if (Directory.Exists(userLocales))
                foreach (var file in Directory.GetFiles(userLocales, "*.json"))
                {
                    var customLocale = Path.GetFileNameWithoutExtension(file);
                    if (customLocale != _defaultLocale)
                    {
                        result.Add(CultureInfo.GetCultureInfo(customLocale));
                        _userLocales.Add(customLocale, JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)));
                    }
                }


            return result;
        }
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
