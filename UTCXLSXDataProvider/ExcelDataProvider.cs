using ExcelDataReader;
using ExcelDataReader.Exceptions;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public object PreviewKeyUp { get; set; }
        public object GotFocus { get; set; }
        public object LostFocus { get; set; }
        public int Period { get; set; }
        public bool IsProvidingCustomProperties => false;

        public string[] Values
        {
            get
            {
                _hasError = false;
                try
                {
                    if (File.Exists(FilePath))
                    {
                        var fileInfo = new FileInfo(FilePath);
                        if (fileInfo.LastWriteTimeUtc > _lastModified)
                        {
                            _lastModified = fileInfo.LastWriteTimeUtc;

                            using (var xls = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                try
                                {
                                    using (var reader = ExcelReaderFactory.CreateReader(xls))
                                    {
                                        List<string> results = new List<string>();
                                        int row = 0;
                                        int sheet = 0;
                                        do
                                        {
                                            if (sheet == SheetIndex)
                                                while (reader.Read())
                                                {
                                                    if (row >= StartRow)
                                                    {
                                                        string line = "";
                                                        for (int i = StartCol; i < (EndCol >= 0 ? Math.Min(reader.FieldCount, EndCol) : reader.FieldCount); i++)
                                                            if (IsTable)
                                                                line += "|" + (reader.GetValue(i)?.ToString() ?? "");
                                                            else
                                                                results.Add((reader.GetValue(i)?.ToString() ?? ""));
                                                        if (IsTable)
                                                            results.Add(line.Substring(1));
                                                    }
                                                    row++;
                                                    if (EndRow >= 0 && row >= EndRow)
                                                        break;
                                                }
                                            sheet++;
                                        }
                                        while (reader.NextResult());

                                        Cached = results.ToArray();
                                        RowsCount = Cached.Length;
                                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
                                        return Cached;
                                    }
                                }
                                catch (ExcelReaderException ex)
                                {
                                    _hasError = true;
                                    RowsCount = 0;
                                    Debug.Print($"Error reading Excel file: {ex.Message}");
                                    return Array.Empty<string>();
                                }
                                catch (Exception ex)
                                {
                                    _hasError = true;
                                    RowsCount = 0;
                                    Debug.Print($"Unexpected error: {ex.Message}");
                                    return Array.Empty<string>();
                                }
                            }
                        }
                        else
                            return Cached;
                    }
                    else
                    {
                        _hasError = true;
                        RowsCount = 0;
                        Debug.Print("File not found.");
                        return Array.Empty<string>();
                    }
                }
                catch (Exception ex)
                {
                    _hasError = true;
                    RowsCount = 0;
                    Debug.Print($"An error occurred: {ex.Message}");
                    return Array.Empty<string>();
                }
            }
        }


        public System.Windows.UIElement CustomUI => _customUI;

        public ExcelDataProvider()
        {
            try
            {
                _customUI = new OnWidgetUI() { DataContext = this };
            }
            catch (Exception e)
            {
                _customUI = new TextBox() { Text = e.ToString(), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 256, FontWeight = FontWeights.Normal, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            }
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
                _lastModified = DateTime.MinValue;
                RaisePropertyChanged(nameof(FilePath));
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

        public int StartCol
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
        private int _startCol = 0;

        public int EndCol
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
        private int _endCol = -1;

        public int SheetIndex
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
        private int _sheet = 0;

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

            FilePath = props.ElementAtOrDefault(0) as string ?? "";
            StartRow = (int?)props.ElementAtOrDefault(1) ?? 0;
            EndRow = (int?)props.ElementAtOrDefault(2) ?? -1;
            StartCol = (int?)props.ElementAtOrDefault(3) ?? 0;
            EndCol = (int?)props.ElementAtOrDefault(4) ?? -1;
            SheetIndex = (int?)props.ElementAtOrDefault(5) ?? 0;
            IsTable = (bool?)props.ElementAtOrDefault(6) as bool? ?? true;
        }

        public void ShowProperties(System.Windows.Window owner)
        {
            // Consider implementing a custom properties window if needed.
        }
    }
}
