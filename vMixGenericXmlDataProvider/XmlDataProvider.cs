// Требуется добавить ссылку на System.Net.Http
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using vMixControllerDataProvider;
using vMixControllerSkin;

namespace XmlDataProviderNs
{
    public class XmlDataProvider : DependencyObject, IvMixDataProviderTextInput, INotifyPropertyChanged
    {
        #region Private Fields

        // 1. Используем HttpClient. Один статический экземпляр рекомендуется для переиспользования.
        private static readonly HttpClient _httpClient = new HttpClient();
        // 2. ConcurrentDictionary для потокобезопасного кэша без ручных блокировок.
        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();

        // 3. SemaphoreSlim для предотвращения одновременной загрузки одного и того же ресурса.
        private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);

        private List<string> _data = new List<string>();

        #endregion

        #region Properties & Commands

        public System.Windows.UIElement CustomUI { get; }
        public bool IsProvidingCustomProperties => true;
        public int Period { get; set; } = 1000; // По умолчанию 1 секунда

        public string[] Values
        {
            get
            {
                // Запускаем асинхронную загрузку данных, не блокируя UI поток.
                // Метод сам проверит, нужно ли обновлять данные.
#pragma warning disable CS4014
                Dispatcher.Invoke(() => LoadDataIfNeededAsync());
#pragma warning restore CS4014
                return _data.ToArray();
            }
        }

        public List<string> Data
        {
            get => _data;
            private set // Сеттер сделан приватным, чтобы данные менялись только внутри класса
            {
                // Проверяем, действительно ли данные изменились, чтобы избежать лишних обновлений
                if (!EqualityComparer<List<string>>.Default.Equals(_data, value))
                {
                    _data = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
                }
            }
        }

        public object PreviewKeyUp { get; set; }
        public object GotFocus { get; set; }
        public object LostFocus { get; set; }

        // 4. Используем expression-bodied members для лаконичности
        private RelayCommand<KeyEventArgs> _previewKeyUpCommand;
        public RelayCommand<KeyEventArgs> PreviewKeyUpCommand => _previewKeyUpCommand ?? (_previewKeyUpCommand = new RelayCommand<KeyEventArgs>(p =>
        {
            if (!(p.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && p.Key == Key.Return))
            {
                ((RelayCommand<KeyEventArgs>)PreviewKeyUp)?.Execute(p);
            }
        }));

        private RelayCommand<KeyEventArgs> _previewKeyDownCommand;
        public RelayCommand<KeyEventArgs> PreviewKeyDownCommand => _previewKeyDownCommand ?? (_previewKeyDownCommand = new RelayCommand<KeyEventArgs>(p =>
        {
            if (p.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && p.Key == Key.Return)
            {
                p.Handled = true;
                if (p.Source is TextBox sender)
                {
                    int lastLocation = sender.SelectionStart;
                    sender.Text = sender.Text.Insert(lastLocation, Environment.NewLine);
                    sender.SelectionStart = lastLocation + Environment.NewLine.Length;
                }
            }
            else if (p.Key == Key.Return)
            {
                p.Handled = true;
            }
        }));

        private RelayCommand _showRowsCommand;
        public RelayCommand ShowRowsCommand => _showRowsCommand ?? (_showRowsCommand = new RelayCommand(() => new RowsViewer().Bind(this, nameof(Data))));


        private RelayCommand _reloadCommand;
        public RelayCommand ReloadCommand => _reloadCommand ?? (_reloadCommand = new RelayCommand(() =>
        {
            Dispatcher.Invoke(() => LoadDataIfNeededAsync());
        }));
        #endregion

        #region Async Data Loading

        private async Task LoadDataIfNeededAsync()
        {
            var url = Url; // Получаем значение из DependencyProperty
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(XPath))
            {
                Data = new List<string>();
                return;
            }

            // Проверка кэша
            if (_cache.TryGetValue(url, out var cacheEntry) && (DateTime.UtcNow - cacheEntry.LastUpdated).TotalMilliseconds < Period)
            {
                // Если данные в кэше актуальны, просто обновим текущий экземпляр из них
                UpdateDataFromCache(cacheEntry.Document);
                return;
            }

            // Входим в семафор, чтобы только один поток мог загружать данные
            await _asyncLock.WaitAsync();
            try
            {
                // Повторная проверка кэша после входа в семафор.
                // Возможно, другой поток уже обновил данные, пока мы ждали.
                if (_cache.TryGetValue(url, out cacheEntry) && (DateTime.UtcNow - cacheEntry.LastUpdated).TotalMilliseconds < Period)
                {
                    UpdateDataFromCache(cacheEntry.Document);
                    return;
                }

                // Загрузка и обработка данных
                var doc = await FetchXmlAsync(url);
                if (doc != null)
                {
                    _cache[url] = new CacheEntry { Document = doc, LastUpdated = DateTime.UtcNow };
                    UpdateDataFromCache(doc);
                }
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        private async Task<XDocument> FetchXmlAsync(string url)
        {
            try
            {
                using (var response = await _httpClient.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        // 5. Асинхронная загрузка в XDocument
                        return XDocument.Load(stream, LoadOptions.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error retrieving XML data from {url}: {ex.Message}");
                return null;
            }
        }

        private void UpdateDataFromCache(XDocument doc)
        {
            if (doc == null) return;

            // Обновление должно происходить в UI потоке, так как оно меняет свойство Data,
            // на которое может быть подписан интерфейс.
            Application.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    var nsManager = new XmlNamespaceManager(new NameTable());
                    var namespaces = NameSpaces; // Получаем значение из DependencyProperty
                    if (!string.IsNullOrEmpty(namespaces))
                    {
                        foreach (var item in namespaces.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            try
                            {
                                var parts = item.Trim().Split(new[] { ' ' }, 2);
                                if (parts.Length == 2)
                                    nsManager.AddNamespace(parts[0], parts[1]);
                            }
                            catch (Exception ex)
                            {
                                Debug.Print($"Error adding namespace: {ex.Message}");
                            }
                        }
                    }

                    // 6. Используем XPathSelectElements из LINQ to XML
                    var nodes = (IEnumerable<object>)doc.XPathEvaluate(XPath, nsManager);
                    var groupBy = GroupBy;

                    var newData = nodes.OfType<XObject>().Select(node =>
                    {
                        switch (node.NodeType)
                        {
                            case XmlNodeType.Element:
                                return ((XElement)node).Value;
                            case XmlNodeType.Text:
                                return ((XText)node).Value;
                            case XmlNodeType.Attribute:
                                return ((XAttribute)node).Value;
                        }
                        return String.Empty;
                    }).Take(100 * (groupBy <= 0 ? 1 : groupBy)).ToList();

                    if (groupBy > 1)
                    {
                        // 7. Группировка с помощью LINQ - более кратко и понятно.
                        Data = newData.Select((item, index) => new { item, index })
                                      .GroupBy(x => x.index / groupBy)
                                      .Select(g => string.Join("|", g.Select(x => x.item)))
                                      .ToList();
                    }
                    else
                    {
                        Data = newData;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"Error updating data from XML: {ex.Message}");
                    Data = new List<string>();
                }
            });
        }


        #endregion

        #region Dependency Properties

        public string Url
        {
            get => (string)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }
        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register(nameof(Url), typeof(string), typeof(XmlDataProvider), new PropertyMetadata(""));

        public string XPath
        {
            get => (string)GetValue(XPathProperty);
            set => SetValue(XPathProperty, value);
        }
        public static readonly DependencyProperty XPathProperty =
            DependencyProperty.Register(nameof(XPath), typeof(string), typeof(XmlDataProvider), new PropertyMetadata(""));

        public string NameSpaces
        {
            get => (string)GetValue(NameSpacesProperty);
            set => SetValue(NameSpacesProperty, value);
        }
        public static readonly DependencyProperty NameSpacesProperty =
            DependencyProperty.Register(nameof(NameSpaces), typeof(string), typeof(XmlDataProvider), new PropertyMetadata(""));

        public int GroupBy
        {
            get => (int)GetValue(GroupByProperty);
            set => SetValue(GroupByProperty, value);
        }
        public static readonly DependencyProperty GroupByProperty =
            DependencyProperty.Register(nameof(GroupBy), typeof(int), typeof(XmlDataProvider), new PropertyMetadata(1));

        #endregion

        #region Interface Implementations & Constructor

        public List<object> GetProperties()
        {
            return new List<object> { Url, XPath, NameSpaces, GroupBy };
        }

        public void SetProperties(List<object> props)
        {
            if (props == null) return;

            Url = props.ElementAtOrDefault(0) as string;
            XPath = props.ElementAtOrDefault(1) as string;
            NameSpaces = props.ElementAtOrDefault(2) as string;
            GroupBy = (int)(props.ElementAtOrDefault(3) ?? 1);
        }

        public void ShowProperties(Window owner)
        {
            var properties = new PropertiesWindow { Owner = owner, DataContext = this };
            var previous = GetProperties();
            if (properties.ShowDialog() != true)
            {
                SetProperties(previous);
            }
        }

        public XmlDataProvider()
        {
            try
            {
                CustomUI = new OnWidgetUI { DataContext = this };
            }
            catch (Exception e)
            {
                CustomUI = new TextBox { Text = e.ToString(), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 256, FontWeight = FontWeights.Normal, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Вспомогательный класс для кэша
        private class CacheEntry
        {
            public XDocument Document { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        #endregion

        // Устаревшие поля и методы, которые больше не нужны
        // private static int _maxid = 0;
        // private readonly int _id = _maxid++;
        // private bool _retrievingData = false;
        // private string _url, _xpath, _namespaces;
        // private int _groupBy;
        // PropertyChangedCallback больше не нужен, т.к. мы читаем значения напрямую из DP.
    }
}
