using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Serialization;
using vMixAPI;
using vMixController.Classes;

namespace vMixController.Widgets
{
    [Serializable]
    public class vMixControlPlaylist : vMixControl
    {
        private DateTime _pause = DateTime.Now;

        public override string Type => "Playlist";

        private ObservableCollection<string> _items = new ObservableCollection<string>();

        /// <summary>
        /// Sets and gets the Items property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<string> Items
        {
            get
            {
                return _items;
            }

            set
            {
                if (_items == value)
                {
                    return;
                }

                _items = value;
                RaisePropertyChanged(nameof(Items));
            }
        }

        private int _selectedIndex = 0;

        /// <summary>
        /// Sets and gets the SelectedIndex property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }

            set
            {
                if (_selectedIndex == value)
                {
                    return;
                }

                _pause = DateTime.Now;

                if (value >= 0)
                {
                    State?.SendFunction("Function", "SelectIndex",
                        "Value", (value + 1).ToString(),
                        "Input", InputKey);
                }
                _selectedIndex = value;
                RaisePropertyChanged(nameof(SelectedIndex));
            }
        }

        private string _inputKey = "";

        /// <summary>
        /// Sets and gets the InputKey property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string InputKey
        {
            get
            {
                return _inputKey;
            }

            set
            {
                if (_inputKey == value)
                {
                    return;
                }

                _inputKey = value;
                RaisePropertyChanged(nameof(InputKey));
            }
        }

        public List<Input> Inputs { get => _internalState?.Inputs; }

        public vMixControlPlaylist()
        {
            XmlDocumentMessenger.OnDocumentDownloaded += XmlDocumentMessenger_OnDocumentDownloaded;
            Height = 128;
        }

        public override Hotkey[] GetHotkeys()
        {
            return new Classes.Hotkey[] {
                new Classes.Hotkey { Name = "Previous Item" },
                new Classes.Hotkey { Name = "Shuffle" },
                new Classes.Hotkey { Name = "PlayOut" },
                new Classes.Hotkey { Name = "Play/Pause" },
                new Classes.Hotkey { Name = "Next Item" }
            };
        }

        public override void ExecuteHotkey(int index)
        {
            switch (index)
            {
                case 0: PrevItemCommand.Execute(null); break;
                case 1: ShuffleCommand.Execute(null); break;
                case 2: PlayOutCommand.Execute(null); break;
                case 3: PlayCommand.Execute(null); break;
                case 4: NextItemCommand.Execute(null); break;
                default:
                    break;
            }
        }

        private void XmlDocumentMessenger_OnDocumentDownloaded(XmlDocument doc, DateTime timestamp)
        {
            if ((timestamp - _pause).TotalSeconds < 1) return;

            var node = doc?.SelectSingleNode(string.Format("//vmix/inputs/input[@key=\"{0}\"]/list", InputKey));
            Dispatcher.Invoke(() =>
            {
                if (node != null)
                {
                    int index = 0;
                    //_updating = true;
                    while (Items.Count > node.ChildNodes.Count)
                        Items.RemoveAt(Items.Count - 1);
                    while (Items.Count < node.ChildNodes.Count)
                        Items.Add(null);

                    foreach (XmlElement item in node.ChildNodes)
                    {
                        Items[index] = Path.GetFileName(item.InnerText);
                        if (item.Attributes["selected"]?.Value.ToLowerInvariant() == "true" && SelectedIndex != index)
                            _selectedIndex = index;
                        index++;
                    }

                    //_updating = false;
                    RaisePropertyChanged(nameof(Items));
                    RaisePropertyChanged(nameof(SelectedIndex));
                }
                else
                    Items.Clear();
            });

        }

        public override void BeforePropertiesChanged()
        {
            base.BeforePropertiesChanged();
        }

        public override void AfterPropertiesChanged()
        {
            base.AfterPropertiesChanged();
        }

        protected override void Dispose(bool managed)
        {
            if (_disposed) return;
            XmlDocumentMessenger.OnDocumentDownloaded -= XmlDocumentMessenger_OnDocumentDownloaded;
            base.Dispose(managed);
        }

        [XmlIgnore]
        private RelayCommand<string> _removeItemCommand;

        /// <summary>
        /// Gets the RemoveItemCommand.
        /// </summary>
        public RelayCommand<string> RemoveItemCommand
        {
            get
            {
                return _removeItemCommand
                    ?? (_removeItemCommand = new RelayCommand<string>(
                    p =>
                    {
                        State?.SendFunction("Function", "ListRemove",
                    "Value", (Items.IndexOf(p) + 1).ToString(),
                    "Input", InputKey);
                    }));
            }
        }

        [XmlIgnore]
        private RelayCommand _addItemCommand;

        /// <summary>
        /// Gets the AddItemCommand.
        /// </summary>
        public RelayCommand AddItemCommand
        {
            get
            {
                return _addItemCommand
                    ?? (_addItemCommand = new RelayCommand(
                    () =>
                    {
                        Ookii.Dialogs.Wpf.VistaOpenFileDialog opendlg = new Ookii.Dialogs.Wpf.VistaOpenFileDialog
                        {
                            Filter = "Any File|*.*"
                        };
                        var result = opendlg.ShowDialog(App.Current.Windows.OfType<vMixWidgetSettingsView>().FirstOrDefault());
                        if (result.HasValue && result.Value)
                        {
                            var fn = opendlg.FileName;
                            State?.SendFunction("Function", "ListAdd",
                    "Value", fn,
                    "Input", InputKey);
                        }

                    }));
            }
        }

        private RelayCommand _nextItemCommand;

        /// <summary>
        /// Gets the NextItemCommand.
        /// </summary>
        public RelayCommand NextItemCommand
        {
            get
            {
                return _nextItemCommand
                    ?? (_nextItemCommand = new RelayCommand(
                    () =>
                    {
                        /*State?.SendFunction("Function", "NextItem",
                "Input", InputKey);*/
                        if (SelectedIndex + 1 < Items.Count)
                            SelectedIndex++;
                    }));
            }
        }

        private RelayCommand _prevItemCommand;

        /// <summary>
        /// Gets the PrevItemCommand.
        /// </summary>
        public RelayCommand PrevItemCommand
        {
            get
            {
                return _prevItemCommand
                    ?? (_prevItemCommand = new RelayCommand(
                    () =>
                    {
                        /*State?.SendFunction("Function", "PreviousItem",
                    "Input", InputKey);*/
                        if (SelectedIndex - 1 >= 0)
                            SelectedIndex--;
                    }));
            }
        }

        private RelayCommand _shuffleCommand;

        /// <summary>
        /// Gets the ShuffleCommand.
        /// </summary>
        public RelayCommand ShuffleCommand
        {
            get
            {
                return _shuffleCommand
                    ?? (_shuffleCommand = new RelayCommand(
                    () =>
                    {
                        State?.SendFunction("Function", "ListShuffle",
                    "Input", InputKey);
                    }));
            }
        }

        private RelayCommand _playOutCommand;

        /// <summary>
        /// Gets the PlayOutCommand.
        /// </summary>
        public RelayCommand PlayOutCommand
        {
            get
            {
                return _playOutCommand
                    ?? (_playOutCommand = new RelayCommand(
                    () =>
                    {
                        State?.SendFunction("Function", "ListPlayOut",
                    "Input", InputKey);
                    }));
            }
        }

        private RelayCommand _playCommand;

        /// <summary>
        /// Gets the PlayOutCommand.
        /// </summary>
        public RelayCommand PlayCommand
        {
            get
            {
                return _playCommand
                    ?? (_playCommand = new RelayCommand(
                    () =>
                    {
                        State?.SendFunction("Function", "PlayPause",
                    "Input", InputKey);
                    }));
            }
        }
    }
}
