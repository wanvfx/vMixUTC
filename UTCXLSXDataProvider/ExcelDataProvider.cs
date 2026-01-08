using ExcelDataReader;
using ExcelDataReader.Exceptions;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using vMixControllerSkin;

namespace UTCGoogleSheetsDataProvider
{
    public class ExcelDataProvider : DependencyObject, vMixControllerDataProvider.IvMixDataProviderTextInput, INotifyPropertyChanged, IDataErrorInfo
    {
        private DateTime _lastModified = DateTime.MinValue;
        private string[] _cached = Array.Empty<string>();
        private bool _hasError = false;
        private System.Windows.UIElement _customUI;
        private CancellationTokenSource _updateCancellationTokenSource; // Для отмены предыдущих задач обновления
        private Task _currentUpdateTask; // Для отслеживания текущей задачи обновления

        public object PreviewKeyUp { get; set; }
        public object GotFocus { get; set; }
        public object LostFocus { get; set; }
        public int Period { get; set; }
        public bool IsProvidingCustomProperties => false;

        private int ParseExcelColumn(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return -1;

            input = input.Trim();

            // Если входная строка состоит только из цифр - это номер столбца
            if (int.TryParse(input, out int number))
            {
                return number;
            }

            // Если есть буквы - это буквенное обозначение
            input = input.ToUpper();

            if (!System.Text.RegularExpressions.Regex.IsMatch(input, "^[A-Z]+$"))
                return -1;

            int result = 0;

            foreach (char c in input)
            {
                result = result * 26 + (c - 'A' + 1);
            }

            return result - 1;
        }

        public string[] Values
        {
            get
            {
                // При обращении к геттеру Values, запускаем обновление в фоне
                // Если обновление уже запущено и не завершено, ничего не делаем.
                // Если обновление завершено или не было запущено, запускаем новое.
                if (_currentUpdateTask == null || _currentUpdateTask.IsCompleted || _currentUpdateTask.IsCanceled || _currentUpdateTask.IsFaulted)
                {
                    StartCacheUpdate();
                }

                // Всегда возвращаем кэшированные данные немедленно
                return _cached;
            }
        }

        public System.Windows.UIElement CustomUI => _customUI;

        public ExcelDataProvider()
        {
            try
            {
                _customUI = new OnWidgetUI() { DataContext = this };
                _updateCancellationTokenSource = new CancellationTokenSource();
            }
            catch (Exception e)
            {
                _customUI = new TextBox() { Text = e.ToString(), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 256, FontWeight = FontWeights.Normal, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            }
        }

        // Метод для запуска обновления кэша в фоне
        private void StartCacheUpdate()
        {
            // Отменяем предыдущую задачу обновления, если она еще выполняется
            _updateCancellationTokenSource?.Cancel();
            _updateCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _updateCancellationTokenSource.Token;

            _currentUpdateTask = Task.Run(async () =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                _hasError = false;
                string actualFilePath = FilePath;
                string currentTempFilePath = null; // Локальная переменная для временного файла в этой задаче

                try
                {
                    // 1. Проверка на URL и загрузка во временный файл
                    if (Uri.TryCreate(FilePath, UriKind.Absolute, out Uri uriResult) &&
                        (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    {
                        try
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                Debug.Print($"Downloading file from URL: {FilePath}");
                                HttpResponseMessage response = await client.GetAsync(FilePath, cancellationToken);
                                response.EnsureSuccessStatusCode();

                                // Создаем новый временный файл для каждой загрузки
                                if (string.IsNullOrWhiteSpace(currentTempFilePath) || !File.Exists(currentTempFilePath))
                                    currentTempFilePath = Path.GetTempFileName();
                                using (var fileStream = new FileStream(currentTempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    await response.Content.CopyToAsync(fileStream);
                                }
                                Debug.Print($"File downloaded to temporary location: {currentTempFilePath}");
                                actualFilePath = currentTempFilePath;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.Print("URL download was cancelled.");
                            return; // Выходим, если операция отменена
                        }
                        catch (HttpRequestException ex)
                        {
                            _hasError = true;
                            RowsCount = 0;
                            Debug.Print($"Error downloading file from URL: {ex.Message}");
                            UpdateCachedValues(Array.Empty<string>());
                            return;
                        }
                        catch (Exception ex)
                        {
                            _hasError = true;
                            RowsCount = 0;
                            Debug.Print($"Unexpected error during URL download: {ex.Message}");
                            UpdateCachedValues(Array.Empty<string>());
                            return;
                        }
                    }

                    if (cancellationToken.IsCancellationRequested) return;

                    if (File.Exists(actualFilePath))
                    {
                        var fileInfo = new FileInfo(actualFilePath);
                        // Проверяем, изменился ли файл или это первая загрузка
                        if (fileInfo.LastWriteTimeUtc > _lastModified || _lastModified == DateTime.MinValue)
                        {
                            _lastModified = fileInfo.LastWriteTimeUtc;

                            using (var xls = new FileStream(actualFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                try
                                {
                                    using (var reader = ExcelReaderFactory.CreateReader(xls))
                                    {
                                        List<string> results = new List<string>();
                                        int row = 0;
                                        int sheet = 0;
                                        int startColIndex = ParseExcelColumn(StartCol);
                                        int endColIndex = ParseExcelColumn(EndCol);

                                        do
                                        {
                                            if (cancellationToken.IsCancellationRequested) return;

                                            int sheetIndex = 0;
                                            bool sheetMatch = false;
                                            if (int.TryParse(SheetIndex, out sheetIndex) && sheet == sheetIndex)
                                            {
                                                sheetMatch = true;
                                            }
                                            else if (reader.Name == SheetIndex)
                                            {
                                                sheetMatch = true;
                                            }

                                            if (sheetMatch)
                                            {
                                                while (reader.Read())
                                                {
                                                    if (cancellationToken.IsCancellationRequested) return;

                                                    if (row >= StartRow)
                                                    {
                                                        string line = "";
                                                        // Обработка случая, когда EndCol не указан или некорректен
                                                        int actualEndColIndex = endColIndex >= 0 ? Math.Min(reader.FieldCount, endColIndex + 1) : reader.FieldCount;

                                                        for (int i = startColIndex; i < actualEndColIndex; i++)
                                                        {
                                                            if (IsTable)
                                                                line += "|" + (reader.GetValue(i)?.ToString() ?? "");
                                                            else
                                                                results.Add((reader.GetValue(i)?.ToString() ?? ""));
                                                        }
                                                        if (IsTable && !string.IsNullOrEmpty(line))
                                                            results.Add(line.Substring(1)); // Удаляем первый разделитель
                                                    }
                                                    row++;
                                                    if (EndRow >= 0 && row >= EndRow)
                                                        break; // Достигнута конечная строка
                                                }
                                            }
                                            sheet++;
                                        }
                                        while (reader.NextResult());

                                        UpdateCachedValues(results.ToArray());
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    Debug.Print("Excel file reading was cancelled.");
                                    return; // Выходим, если операция отменена
                                }
                                catch (ExcelReaderException ex)
                                {
                                    _hasError = true;
                                    RowsCount = 0;
                                    Debug.Print($"Error reading Excel file: {ex.Message}");
                                    UpdateCachedValues(Array.Empty<string>());
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    _hasError = true;
                                    RowsCount = 0;
                                    Debug.Print($"Unexpected error during Excel processing: {ex.Message}");
                                    UpdateCachedValues(Array.Empty<string>());
                                    return;
                                }
                            }
                        }
                        // Если файл не изменился, просто возвращаем текущий кэш
                        // (но в данном случае мы уже в фоне, так что просто не обновляем кэш)
                    }
                    else
                    {
                        _hasError = true;
                        RowsCount = 0;
                        Debug.Print("File not found.");
                        UpdateCachedValues(Array.Empty<string>());
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.Print("Cache update operation was cancelled.");
                    return; // Выходим, если операция отменена
                }
                catch (Exception ex)
                {
                    _hasError = true;
                    RowsCount = 0;
                    Debug.Print($"An error occurred during cache update: {ex.Message}");
                    UpdateCachedValues(Array.Empty<string>());
                }
                finally
                {
                    // Удаляем временный файл, если он был создан
                    if (!string.IsNullOrWhiteSpace(currentTempFilePath) && File.Exists(currentTempFilePath))
                    {
                        try
                        {
                            File.Delete(currentTempFilePath);
                            Debug.Print($"Temporary file deleted: {currentTempFilePath}");
                        }
                        catch (Exception ex)
                        {
                            Debug.Print($"Error deleting temporary file {currentTempFilePath}: {ex.Message}");
                        }
                    }
                }
            }, cancellationToken);
        }

        // Метод для безопасного обновления кэша и связанных свойств из фонового потока
        private void UpdateCachedValues(string[] newValues)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Cached = newValues;
                RowsCount = Cached.Length;
                RaisePropertyChanged(nameof(FilePath)); // Чтобы обновить индикацию ошибки, если она была
                RaisePropertyChanged(nameof(Values)); // Уведомляем об изменении Values
            });
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath == value)
                {
                    return;
                }

                _filePath = value;
                _lastModified = DateTime.MinValue; // Сбрасываем, чтобы принудительно обновить кэш
                RaisePropertyChanged(nameof(FilePath));
                // Обновление теперь инициируется геттером Values, поэтому здесь не нужно вызывать StartCacheUpdate()
            }
        }
        private string _filePath = "";

        public int StartRow
        {
            get => _startRow;
            set
            {
                if (_startRow == value)
                {
                    return;
                }

                _startRow = value;
                _lastModified = DateTime.MinValue;
                RaisePropertyChanged(nameof(StartRow));
            }
        }
        private int _startRow = 0;

        public int EndRow
        {
            get => _endRow;
            set
            {
                if (_endRow == value)
                {
                    return;
                }

                _endRow = value;
                _lastModified = DateTime.MinValue;
                RaisePropertyChanged(nameof(EndRow));
            }
        }
        private int _endRow = -1;

        public string StartCol
        {
            get => _startCol;
            set
            {
                if (_startCol == value)
                {
                    return;
                }

                _startCol = value;
                _lastModified = DateTime.MinValue;
                RaisePropertyChanged(nameof(StartCol));
            }
        }
        private string _startCol = "0";

        public string EndCol
        {
            get => _endCol;
            set
            {
                if (_endCol == value)
                {
                    return;
                }

                _endCol = value;
                _lastModified = DateTime.MinValue;
                RaisePropertyChanged(nameof(EndCol));
            }
        }
        private string _endCol = "-1";

        public string SheetIndex
        {
            get => _sheet;
            set
            {
                if (_sheet == value)
                {
                    return;
                }

                _sheet = value;
                _lastModified = DateTime.MinValue;
                RaisePropertyChanged(nameof(SheetIndex));
            }
        }
        private string _sheet = "0";

        public bool IsTable
        {
            get => _isTable;
            set
            {
                if (_isTable == value)
                {
                    return;
                }

                _isTable = value;
                _lastModified = DateTime.MinValue;
                RaisePropertyChanged(nameof(IsTable));
            }
        }
        private bool _isTable = true;

        public int RowsCount
        {
            get => _rowsCount;
            set
            {
                if (_rowsCount == value)
                {
                    return;
                }

                _rowsCount = value;
                RaisePropertyChanged(nameof(RowsCount));
            }
        }
        private int _rowsCount = 0;

        public string Error => null; // Not implemented, no general error for the entire object

        private RelayCommand _showRowsCommand;

        public RelayCommand ShowRowsCommand => _showRowsCommand ?? (_showRowsCommand = new RelayCommand(() => new RowsViewer().Bind(this, nameof(Cached))));

        public string[] Cached
        {
            get => _cached;
            set
            {
                _cached = value;
                RaisePropertyChanged(nameof(Cached));
            }
        }

        public string this[string columnName]
        {
            get
            {
                string error = string.Empty;
                switch (columnName)
                {
                    case nameof(FilePath):
                        if (_hasError)
                            error = "File not found or is not a valid excel file!";
                        break;
                    case nameof(StartCol):
                        // Проверяем, чтобы StartCol был либо числом, либо буквенным обозначением
                        if (!int.TryParse(StartCol, out _) && !System.Text.RegularExpressions.Regex.IsMatch(StartCol, "^[A-Z]+$"))
                            error = "Start column must be a number or a letter (e.g., '0' or 'A').";
                        break;
                    case nameof(EndCol):
                        // Проверяем, чтобы EndCol был либо числом, либо буквенным обозначением
                        if (!int.TryParse(EndCol, out _) && !System.Text.RegularExpressions.Regex.IsMatch(EndCol, "^[A-Z]+$"))
                            error = "End column must be a number or a letter (e.g., '-1' or 'B').";
                        break;
                }
                return error;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<object> GetProperties()
        {
            return new List<object> { FilePath, StartRow, EndRow, StartCol, EndCol, SheetIndex, IsTable };
        }

        public void SetProperties(List<object> props)
        {
            if (props == null) return;

            // Используем временные переменные для предотвращения многократного вызова StartCacheUpdate
            string newFilePath = props.ElementAtOrDefault(0) as string ?? "";
            int newStartRow = (int?)props.ElementAtOrDefault(1) ?? 0;
            int newEndRow = (int?)props.ElementAtOrDefault(2) ?? -1;
            string newStartCol;
            string newEndCol;
            string newSheetIndex;
            bool newIsTable = (bool?)props.ElementAtOrDefault(6) as bool? ?? true;

            if (props.ElementAtOrDefault(3) is int)
                newStartCol = ((int?)props.ElementAtOrDefault(3) ?? 0).ToString();
            else
                newStartCol = (string)props.ElementAtOrDefault(3) ?? "0";

            if (props.ElementAtOrDefault(4) is int)
                newEndCol = ((int?)props.ElementAtOrDefault(4) ?? -1).ToString();
            else
                newEndCol = (string)props.ElementAtOrDefault(4) ?? "-1";

            if (props.ElementAtOrDefault(5) is int)
                newSheetIndex = ((int?)props.ElementAtOrDefault(5) ?? 0).ToString();
            else
                newSheetIndex = (string)props.ElementAtOrDefault(5) ?? "0";

            // Применяем свойства только если они изменились
            if (FilePath != newFilePath) FilePath = newFilePath;
            if (StartRow != newStartRow) StartRow = newStartRow;
            if (EndRow != newEndRow) EndRow = newEndRow;
            if (StartCol != newStartCol) StartCol = newStartCol;
            if (EndCol != newEndCol) EndCol = newEndCol;
            if (SheetIndex != newSheetIndex) SheetIndex = newSheetIndex;
            if (IsTable != newIsTable) IsTable = newIsTable;
        }

        public void ShowProperties(System.Windows.Window owner)
        {
            // Consider implementing a custom properties window if needed.
        }
    }
}