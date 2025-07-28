using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using vMixController.Classes;
using vMixController.Messages;

namespace vMixController.ViewModel
{
    public class GlobalVariablesViewModel : ViewModelBase
    {

        public static ObservableCollection<Pair<string, string>> _variables = new ObservableCollection<Pair<string, string>>();

        /// <summary>
        /// Sets and gets the Variables property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<Pair<string, string>> Variables
        {
            get
            {
                return _variables;
            }

            set
            {
                if (_variables == value)
                {
                    return;
                }

                _variables.CollectionChanged -= VariablesCollectionChanged;
                foreach (var item in _variables)
                    item.PropertyChanged -= GlobalVariableChanged;
                _variables = value;
                _variables.CollectionChanged += VariablesCollectionChanged;
                foreach (var item in _variables)
                    item.PropertyChanged += GlobalVariableChanged;
                RaisePropertyChanged(nameof(Variables));
                Messenger.Default.Send(new FillGlobalVariables() { });
            }
        }

        public GlobalVariablesViewModel()
        {

            Messenger.Default.Register<SetGlobalVariable>(this, (t) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (t.Index == -1)
                    {
                        var v = Variables.Where(x => x.A == t.Name).FirstOrDefault();
                        if (v != null)
                            v.B = t.Value;
                    }
                    else if (Variables.Count > t.Index)
                        Variables[t.Index].B = t.Value;
                }));

            });

            Messenger.Default.Register<FillGlobalVariables>(this, (t) =>
            {
                Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Clear });
                foreach (var v in Variables)
                    Messenger.Default.Send(new UpdateGlobalVariable() { Name = v.A, Value = v.B, State = VariableState.Add });
            });

            _variables.CollectionChanged += VariablesCollectionChanged;
            foreach (var item in _variables)
                item.PropertyChanged += GlobalVariableChanged;

            /*Variables.Add(new Pair<string, string>("test", "test"));
            Variables.Add(new Pair<string, string>("test1", "test5"));
            Variables.Add(new Pair<string, string>("test2", "test6"));
            Variables.Add(new Pair<string, string>("test3", "test7"));
            Variables.Add(new Pair<string, string>("test4", "test8"));*/
        }

        private void VariablesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var v in e.OldItems.OfType<Pair<string, string>>())
                    {
                        Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Delete, Name = v.A });
                        v.PropertyChanged -= GlobalVariableChanged;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Clear });
                    /*foreach (var v in e.OldItems.OfType<Pair<string, string>>())
                        v.PropertyChanged -= GlobalVariableChanged;*/
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var v in e.NewItems.OfType<Pair<string, string>>())
                    {
                        Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Add, Name = v.A, Value = v.B });
                        v.PropertyChanged += GlobalVariableChanged;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    foreach (var v in e.OldItems.OfType<Pair<string, string>>())
                    {
                        Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Delete, Name = v.A });
                        v.PropertyChanged -= GlobalVariableChanged;
                    }
                    foreach (var v in e.NewItems.OfType<Pair<string, string>>())
                    {
                        Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Add, Name = v.A, Value = v.B });
                        v.PropertyChanged += GlobalVariableChanged;
                    }
                    break;
            }
        }

        private void GlobalVariableChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            var obj = (Pair<string, string>)sender;
            if (e.Property == Pair<string, string>.AProperty)
            {
                Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Delete, Name = (string)e.OldValue });
                Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Add, Name = (string)e.NewValue, Value = obj.B });
            }
            else
                Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Add, Name = obj.A, Value = (string)e.NewValue });
            //Messenger.Default.Send(new UpdateGlobalVariable() { State = VariableState.Added, Name = e., Value = v.B });
        }

        private RelayCommand<Pair<string, string>> _removeItemCommand;

        /// <summary>
        /// Gets the RemoveItemCommand.
        /// </summary>
        public RelayCommand<Pair<string, string>> RemoveItemCommand
        {
            get
            {
                return _removeItemCommand
                    ?? (_removeItemCommand = new RelayCommand<Pair<string, string>>(
                    p =>
                    {
                        Variables.Remove(p);
                    }));
            }
        }

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
                        Variables.Add(new Pair<string, string>("", ""));
                    }));
            }
        }


        private RelayCommand _okCommand;

        /// <summary>
        /// Gets the OkCommand.
        /// </summary>
        public RelayCommand OkCommand
        {
            get
            {
                return _okCommand
                    ?? (_okCommand = new RelayCommand(
                    () =>
                    {
                        MessengerInstance.Send(true);
                    }));
            }
        }
    }
}
