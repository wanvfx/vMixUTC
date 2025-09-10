using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading; // Для CancellationTokenSource
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using vMixControllerDataProvider;
using vMixControllerSkin;
using Json.Path;
using Json.More;
using System.Net.Http;
using System.IO;

namespace JsonDataProviderNs
{
    public class JsonDataProvider : IvMixDataProviderTextInput, INotifyPropertyChanged, IDisposable
    {
        // Лучшая практика: один экземпляр HttpClient на всё приложение
        private static readonly HttpClient _httpClient = new HttpClient();

        private JsonDocument _document;
        private DateTime _previousQuery;

        private List<string> _data = new List<string>();
        private volatile bool _retrievingData = false;
        private readonly object _retrievingDataLock = new object(); // Lock для управления флагом _retrievingData

        // Источник токенов для отмены предыдущего запроса
        private CancellationTokenSource _cancellationTokenSource;

        private string _url = "";
        private string _jsonPath = "";
        private string _headers = "";
        private string _error = "";
        private int _groupBy = 1;
        private bool _reload = false;
        private UIElement _ui;

        public event PropertyChangedEventHandler PropertyChanged;

        public object PreviewKeyUp { get; set; }
        public object GotFocus { get; set; }
        public object LostFocus { get; set; }
        public int Period { get; set; } = 5000; // Установим значение по умолчанию

        private RelayCommand<KeyEventArgs> _previewKeyUpCommand;
        public RelayCommand<KeyEventArgs> PreviewKeyUpCommand => _previewKeyUpCommand ?? (_previewKeyUpCommand = new RelayCommand<KeyEventArgs>(
            p =>
            {
                if (!(p.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && p.Key == Key.Return))
                    ((RelayCommand<KeyEventArgs>)PreviewKeyUp)?.Execute(p);
            }));

        private RelayCommand<KeyEventArgs> _previewKeyDownCommand;
        public RelayCommand<KeyEventArgs> PreviewKeyDownCommand => _previewKeyDownCommand ?? (_previewKeyDownCommand = new RelayCommand<KeyEventArgs>(
            p =>
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

        public bool IsProvidingCustomProperties => false;

        public string[] Values
        {
            get
            {
                // Проверяем, нужно ли обновить данные по таймеру
                if ((DateTime.Now - _previousQuery).TotalMilliseconds >= Period || _reload)
                {
                    _reload = false;
                    // Запускаем асинхронное получение данных, не блокируя UI
                    // Используем "fire and forget" с отловом ошибок внутри метода
                    _ = RetrieveDataAsync();
                }
                return Data.ToArray();
            }
        }

        public static void AddHeadersFromString(HttpClient client, string headersString)
        {
            if (string.IsNullOrWhiteSpace(headersString))
                return;

            var lines = headersString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim();

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
                    }
                }
            }
        }

        /// <summary>
        /// Асинхронно получает и обрабатывает JSON данные.
        /// Отменяет предыдущий выполняющийся запрос.
        /// </summary>
        private async Task RetrieveDataAsync()
        {
            Error = "";
            // Блокируем, чтобы проверить и установить флаг _retrievingData атомарно
            lock (_retrievingDataLock)
            {
                // Если уже идет процесс получения данных, выходим
                if (_retrievingData) return;
                _retrievingData = true;
            }

            _previousQuery = DateTime.Now;

            // Если URL невалидный, просто выходим
            if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri))
            {
                _retrievingData = false;
                return;
            }

            // Отменяем предыдущую операцию, если она была, и создаем новый CancellationTokenSource
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                JsonDocument newDocument;

                // БЫЛО (несовместимо со старыми .NET):
                // using (var stream = await _httpClient.GetStreamAsync(uri, token))

                // СТАЛО (совместимо и правильно):
                // 1. Выполняем GET запрос с токеном отмены
                if (uri.Scheme == Uri.UriSchemeFile)
                {
                    using (var stream = File.Open(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        newDocument = await JsonDocument.ParseAsync(stream, default, token);
                }
                else
                {

                    AddHeadersFromString(_httpClient, Headers);
                    using (var response = await _httpClient.GetAsync(uri, token))
                    {
                        // 2. Проверяем, что запрос успешен (статус 2xx)
                        response.EnsureSuccessStatusCode();

                        // 3. Получаем поток из контента ответа
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            // 4. Парсим JSON из потока, также передавая токен отмены
                            // (на случай, если парсинг очень большого документа тоже нужно прервать)
                            newDocument = await JsonDocument.ParseAsync(stream, default, token);

                        }
                    }
                }
                // После успешного получения и парсинга, обновляем данные в потоке UI
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    // Проверяем, не была ли операция отменена, пока мы ждали диспетчер
                    if (token.IsCancellationRequested) return;

                    _document?.Dispose(); // Освобождаем память от старого документа
                    _document = newDocument;
                    UpdateData();
                });

            }
            catch (OperationCanceledException)
            {
                // Это ожидаемое исключение при отмене запроса. Логируем для отладки.
                Error = ("JSON data request was cancelled.");
            }
            catch (HttpRequestException ex)
            {
                // Это исключение будет вызвано EnsureSuccessStatusCode при ошибке (напр. 404, 500)
                Error = ($"HTTP request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Логируем другие ошибки (сетевые, парсинга и т.д.)
                Error = ($"Error retrieving or parsing JSON data: {ex.Message}");
            }
            finally
            {
                // Вне зависимости от результата, сбрасываем флаг
                _retrievingData = false;
            }
        }


        private void UpdateData()
        {
            Error = "";
            if (_document == null) return;
            
            try
            {
                var path = Json.Path.JsonPath.Parse(JsonPath.Replace("\r", "").Replace("\n", ""));
                var results = path.Evaluate(_document.RootElement.AsNode()).Matches.Take(100 * (_groupBy <= 0 ? 1 : _groupBy)).Select(x => x.Value.ToString()).ToList();

                if (_groupBy > 1)
                {
                    var groupedData = new List<string>();
                    var grouped = new StringBuilder();
                    for (int i = 0; i < results.Count; i++)
                    {
                        if (i > 0 && i % _groupBy == 0)
                        {
                            groupedData.Add(grouped.ToString().TrimEnd('|'));
                            grouped.Clear();
                        }
                        grouped.Append(results[i]).Append("|");
                    }
                    if (grouped.Length > 0)
                    {
                        groupedData.Add(grouped.ToString().TrimEnd('|'));
                    }
                    Data = groupedData;
                }
                else
                {
                    Data = results;
                }
            }
            catch (Exception ex)
            {
                Error = ($"Error updating data with JSONPath: {ex.Message}");
            }
        }

        public UIElement CustomUI => _ui;

        public string Url
        {
            get => _url;
            set
            {
                if (_url == value) return; // Не делаем ничего, если URL не изменился
                _url = value;
                OnPropertyChanged(nameof(Url));
                // Немедленно запускаем обновление данных с новым URL
                _ = RetrieveDataAsync();
            }
        }

        public string JsonPath
        {
            get => _jsonPath;
            set
            {
                if (_jsonPath == value) return;
                _jsonPath = value;
                OnPropertyChanged(nameof(JsonPath));
                // Если документ уже загружен, просто перепарсим его с новым путем
                UpdateData();
            }
        }

        public string Headers
        {
            get => _headers;
            set
            {
                if (_headers == value) return;
                _headers = value;
                OnPropertyChanged(nameof(Headers));
                // Если документ уже загружен, просто перепарсим его с новым путем
                UpdateData();
            }
        }

        public string Error
        {
            get => _error;
            set
            {
                if (_error == value) return;
                _error = value;
                OnPropertyChanged(nameof(Error));
            }
        }

        public List<string> Data
        {
            get => _data;
            set
            {
                _data = value;
                OnPropertyChanged(nameof(Data));
            }
        }

        public int GroupBy
        {
            get => _groupBy;
            set
            {
                if (_groupBy == value) return;
                _groupBy = value;
                OnPropertyChanged(nameof(GroupBy));
                // Если документ уже загружен, перегруппируем данные
                UpdateData();
            }
        }

        public List<object> GetProperties()
        {
            return new List<object> { Url, JsonPath, GroupBy, Headers };
        }

        public void SetProperties(List<object> props)
        {
            Url = (string)(props?.ElementAtOrDefault(0) ?? "");
            JsonPath = (string)(props?.ElementAtOrDefault(1) ?? "");
            GroupBy = (int)(props?.ElementAtOrDefault(2) ?? 1);
            Headers = (string)(props?.ElementAtOrDefault(3) ?? "");
        }

        private RelayCommand _showRowsCommand;
        public RelayCommand ShowRowsCommand => _showRowsCommand ?? (_showRowsCommand = new RelayCommand(
            () =>
            {
                new RowsViewer().Bind(this, "Data");
            }));


        private RelayCommand _reloadCommand;
        public RelayCommand ReloadCommand => _reloadCommand ?? (_reloadCommand = new RelayCommand(
            () =>
            {
                _reload = true;
                _cancellationTokenSource?.Cancel();
                _ = RetrieveDataAsync();
            }));

        public JsonDataProvider()
        {
            try
            {
                _ui = new OnWidgetUI { DataContext = this };
            }
            catch (Exception e)
            {
                _ui = new TextBox { Text = e.ToString(), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 256, FontWeight = FontWeights.Normal, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            }
        }

        public void ShowProperties(Window owner)
        {
            // Implementation for showing properties window
            // For now, it's not implemented
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            // Реализуем IDisposable для корректной очистки ресурсов
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _document?.Dispose();
        }
    }
}
