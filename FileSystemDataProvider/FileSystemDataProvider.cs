// Требуется добавить ссылку на System.Net.Http
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using vMixControllerDataProvider;
using vMixControllerSkin;

namespace FileSystemDataProviderNs
{
    public class FileSystemDataProvider : DependencyObject, IvMixDataProviderTextInput, INotifyPropertyChanged
    {
        #region Properties & Commands

        public System.Windows.UIElement CustomUI { get; }
        public bool IsProvidingCustomProperties => false;
        public int Period
        {
            get => _period;
            set
            {
                var normalized = Math.Max(100, value);
                if (_period == normalized)
                {
                    return;
                }

                _period = normalized;
                if (_refreshTimer != null)
                {
                    _refreshTimer.Interval = TimeSpan.FromMilliseconds(_period);
                }
            }
        }

        private readonly object _valuesSync = new object();
        private string[] _cachedValues = Array.Empty<string>();
        private DateTime _lastRefreshUtc = DateTime.MinValue;
        private readonly DispatcherTimer _refreshTimer;
        private int _period = 1000;

        public string[] Values
        {
            get
            {
                RefreshValuesIfNeeded();
                return _cachedValues;
            }
        }

        public string Error
        {
            get => (string)GetValue(ErrorProperty);
            set => SetValue(ErrorProperty, value);
        }
        public static readonly DependencyProperty ErrorProperty =
            DependencyProperty.Register(nameof(Error), typeof(string), typeof(FileSystemDataProvider), new PropertyMetadata(""));

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
        private string _path;
        private string _filter = "*.*";
        private bool _includeSub;

        public RelayCommand ShowRowsCommand => _showRowsCommand ?? (_showRowsCommand = new RelayCommand(() => new RowsViewer().Bind(this, nameof(Values))));

        #endregion

        #region Dependency Properties

        public string Path
        {
            get => (string)GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }
        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register(nameof(Path), typeof(string), typeof(FileSystemDataProvider), new PropertyMetadata("", OnPropertyChanged));

        public string Filter
        {
            get => (string)GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register(nameof(Filter), typeof(string), typeof(FileSystemDataProvider), new PropertyMetadata("*.*", OnPropertyChanged));


        public bool IncludeSubDirectories
        {
            get => (bool)GetValue(IncludeSubDirectoriesProperty);
            set => SetValue(IncludeSubDirectoriesProperty, value);
        }
        public static readonly DependencyProperty IncludeSubDirectoriesProperty =
            DependencyProperty.Register(nameof(IncludeSubDirectories), typeof(bool), typeof(FileSystemDataProvider), new PropertyMetadata(false, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var provider = d as FileSystemDataProvider;
            if (provider == null) return;

            if (e.Property == PathProperty)
                provider._path = (string)e.NewValue;
            else if (e.Property == FilterProperty)
                provider._filter = (string)e.NewValue;
            else
                provider._includeSub = (bool)e.NewValue;

            provider.InvalidateValuesCache();
        }
        #endregion

        #region Interface Implementations & Constructor

        public List<object> GetProperties()
        {
            return new List<object> { Path };
        }

        public void SetProperties(List<object> props)
        {
            if (props == null) return;

            Path = props.ElementAtOrDefault(0) as string;

        }

        public void ShowProperties(Window owner)
        {
            //throw new NotImplementedException();
        }

        public FileSystemDataProvider()
        {
            try
            {
                CustomUI = new OnWidgetUI { DataContext = this };
            }
            catch (Exception e)
            {
                CustomUI = new TextBox { Text = e.ToString(), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 256, FontWeight = FontWeights.Normal, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            }

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(Period)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void RefreshValuesIfNeeded()
        {
            var refreshInterval = Math.Max(100, Period);
            if ((DateTime.UtcNow - _lastRefreshUtc).TotalMilliseconds < refreshInterval)
            {
                return;
            }

            lock (_valuesSync)
            {
                if ((DateTime.UtcNow - _lastRefreshUtc).TotalMilliseconds < refreshInterval)
                {
                    return;
                }

                try
                {
                    SetError(string.Empty);
                    var newValues = (!string.IsNullOrWhiteSpace(_path) && Directory.Exists(_path))
                        ? Directory.GetFiles(_path, _filter ?? "*.*", _includeSub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                        : Array.Empty<string>();

                    if (!newValues.SequenceEqual(_cachedValues))
                    {
                        _cachedValues = newValues;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
                    }
                }
                catch (Exception ex)
                {
                    SetError($"Error retrieving file list: {ex.Message}");
                    _cachedValues = Array.Empty<string>();
                }
                finally
                {
                    _lastRefreshUtc = DateTime.UtcNow;
                }
            }
        }

        private void InvalidateValuesCache()
        {
            lock (_valuesSync)
            {
                _lastRefreshUtc = DateTime.MinValue;
            }

            RefreshValuesIfNeeded();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
        }

        private void SetError(string error)
        {
            if (Dispatcher.CheckAccess())
            {
                Error = error;
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => Error = error));
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshValuesIfNeeded();
        }
    }
}
