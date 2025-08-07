// Требуется добавить ссылку на System.Net.Http
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
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

namespace FileSystemDataProviderNs
{
    public class FileSystemDataProvider : DependencyObject, IvMixDataProviderTextInput, INotifyPropertyChanged
    {
        #region Properties & Commands

        public System.Windows.UIElement CustomUI { get; }
        public bool IsProvidingCustomProperties => false;
        public int Period { get; set; } = 1000; // По умолчанию 1 секунда

        public bool _changing = false;

        public string[] Values
        {
            get
            {
                try
                {
                    if (!_changing)
                    {
                        _changing = true;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
                        _changing = false;
                    }
                    Dispatcher.Invoke(() =>
                    {
                        Error = "";
                    });
                    if (!string.IsNullOrWhiteSpace(_path) && Directory.Exists(_path))
                        return Directory.GetFiles(_path, _filter ?? "*.*", _includeSub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                    else
                        return Array.Empty<string>();
                } catch (Exception ex) 
                {
                    Dispatcher.Invoke(() =>
                    {
                        Error = ($"Error retrieving file list: {ex.Message}"); ;
                    });
                    return Array.Empty<string>();
                }
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

            if (e.Property == PathProperty)
                (d as FileSystemDataProvider)._path = (string)e.NewValue;
            else if (e.Property == FilterProperty)
                (d as FileSystemDataProvider)._filter = (string)e.NewValue;
            else
                (d as FileSystemDataProvider)._includeSub = (bool)e.NewValue;

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
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
