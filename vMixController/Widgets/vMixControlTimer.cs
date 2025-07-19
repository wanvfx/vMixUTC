using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Linq;
using System.Windows.Threading;
using System.Xml.Serialization;
using vMixController.Classes;
using System.Windows.Controls;
using vMixController.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using GalaSoft.MvvmLight.Messaging;
using HighPrecisionTimer;
using System.Windows;

namespace vMixController.Widgets
{
    public static class GlobalTimer
    {
        static long _workingTimers = 0;
        static Stopwatch _stopwatch = new Stopwatch();
        static TimeSpan _second = TimeSpan.FromSeconds(1 / 10f);
        static double _tickSecond = 0;

        public static long WorkingTimers
        {
            get { return _workingTimers; }
            set
            {
                _workingTimers = value;
                if (_workingTimers <= 0)
                {
                    if (_mtimer.IsRunning)
                        _mtimer.Stop();

                    _stopwatch.Stop();
                    _stopwatch.Reset();
                    _workingTimers = 0;
                }
                else if (!_mtimer.IsRunning)
                {
                    _stopwatch.Start();
                    _mtimer.Start();
                }
            }
        }
        static MultimediaTimer _mtimer = new MultimediaTimer();
        static GlobalTimer()
        {

            _mtimer.Interval = (int)_second.TotalMilliseconds;
            _mtimer.Resolution = 10;
            _mtimer.Elapsed += _mtimer_Elapsed;
        }

        private static void _mtimer_Elapsed(object sender, EventArgs e)
        {
            _stopwatch.Stop();
            Messenger.Default.Send(_second);
            _tickSecond += _second.TotalMilliseconds;
            if (_tickSecond > 950)
            {
                Messenger.Default.Send(TimeSpan.FromMilliseconds(_tickSecond));
                _tickSecond = 0;
            }

            //Debug.WriteLine(_stopwatch.Elapsed);
            _stopwatch.Restart();
        }
    }
    [Serializable]
    public class vMixControlTimer : vMixControlTextField
    {
        bool _changingTime = false;
        public override string Type
        {
            get
            {
                return Extensions.LocalizationManager.Get("Timer");
            }
        }
        public vMixControlTimer()
        {
            Messenger.Default.Register<TimeSpan>(this, (t) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (IsHighPrecision)
                    {
                        if (t.TotalMilliseconds < 900)
                            Tick(t);
                    }
                    else if (t.TotalMilliseconds > 950)
                        Tick(t);

                });

            });

            _width = 256;
        }

        public override Hotkey[] GetHotkeys()
        {
            return new Hotkey[] { new Classes.Hotkey() { Name = "Start" },
            new Classes.Hotkey() { Name = "Pause" },
            new Classes.Hotkey() { Name = "Stop" },
            new Classes.Hotkey() { Name = "+1 Hour" },
            new Classes.Hotkey() { Name = "+1 Minute" },
            new Classes.Hotkey() { Name = "+1 Second" },
            new Classes.Hotkey() { Name = "-1 Hour" },
            new Classes.Hotkey() { Name = "-1 Minute" },
            new Classes.Hotkey() { Name = "-1 Second" }};
        }

        public override UserControl[] GetPropertiesControls()
        {
            return base.GetPropertiesControls();
        }

        private void Tick(TimeSpan e)
        {
            if (!Active) return;

            if (!Reverse)
            {
                var t = Time.Add(e);
                if (t < DefaultTime)
                    Time = t;
                else
                {
                    Time = DefaultTime;
                    Paused = false;
                    Active = false;
                    GlobalTimer.WorkingTimers--;
                    if (!string.IsNullOrWhiteSpace(Links[2]))
                        Messenger.Default.Send(new Pair<string, object>(Links[2], null));
                    if (!string.IsNullOrWhiteSpace(Links[3]))
                        Messenger.Default.Send(new Pair<string, object>(Links[3], null));
                }
            }
            else
            {
                var t = Time.Subtract(e);
                if (t > TimeSpan.FromSeconds(0))
                    Time = t;
                else
                {
                    Time = TimeSpan.Zero;
                    Paused = false;
                    Active = false;
                    GlobalTimer.WorkingTimers--;
                    if (!string.IsNullOrWhiteSpace(Links[2]))
                        Messenger.Default.Send(new Pair<string, object>(Links[2], null));
                    if (!string.IsNullOrWhiteSpace(Links[3]))
                        Messenger.Default.Send(new Pair<string, object>(Links[3], null));
                }
            }
            if (!string.IsNullOrWhiteSpace(Links[4]))
                Messenger.Default.Send<Pair<string, object>>(new Pair<string, object>(Links[4], null));
        }



        private void UpdateTimer()
        {
            //Назад
            if (!Paused)
            {
                Time = TimeSpan.Zero;//TimeSpan.FromMilliseconds(Time.Milliseconds);
                if (Reverse)
                    Time = DefaultTime.Add(Time);
            }
        }

        private bool _splitText = false;

        /// <summary>
        /// Sets and gets the SplitText property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool SplitText
        {
            get
            {
                return _splitText;
            }

            set
            {
                if (_splitText == value)
                {
                    return;
                }

                _splitText = value;
                RaisePropertyChanged(nameof(SplitText));
            }
        }

        private bool _isHighPrecision = false;

        /// <summary>
        /// Sets and gets the IsHighPrecision property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsHighPrecision
        {
            get
            {
                return _isHighPrecision;
            }

            set
            {
                if (_isHighPrecision == value)
                {
                    return;
                }

                _isHighPrecision = value;
                RaisePropertyChanged(nameof(IsHighPrecision));
            }
        }

        private string _format = @"hh\:mm\:ss";

        /// <summary>
        /// Sets and gets the Format property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Format
        {
            get
            {
                return _format;
            }

            set
            {
                if (_format == value)
                {
                    return;
                }

                _format = value;
                RaisePropertyChanged(nameof(Format));
            }
        }

        private string[] _links = new string[] { "", "", "", "", "" };

        /// <summary>
        /// Sets and gets the Links property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string[] Links
        {
            get
            {
                return _links;
            }

            set
            {
                if (_links == value)
                {
                    return;
                }

                _links = value;
                RaisePropertyChanged(nameof(Links));
            }
        }

        private bool _reverse = false;

        /// <summary>
        /// Sets and gets the Reverse property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool Reverse
        {
            get
            {
                return _reverse;
            }

            set
            {
                if (_reverse == value)
                {
                    return;
                }

                _reverse = value;

                UpdateTimer();

                RaisePropertyChanged(nameof(Reverse));
            }
        }

        [NonSerialized]
        private bool _active = false;

        /// <summary>
        /// Sets and gets the Active property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore]
        public bool Active
        {
            get
            {
                return _active;
            }

            set
            {
                if (_active == value)
                {
                    return;
                }

                _active = value;
                RaisePropertyChanged(nameof(Active));
            }
        }

        [NonSerialized]
        private bool _paused = false;

        /// <summary>
        /// Sets and gets the Paused property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore]
        public bool Paused
        {
            get
            {
                return _paused;
            }

            set
            {
                if (_paused == value)
                {
                    return;
                }

                _paused = value;
                RaisePropertyChanged(nameof(Paused));
            }
        }

        private TimeSpan _time = TimeSpan.Zero;

        /// <summary>
        /// Sets and gets the Time property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore()]
        public TimeSpan Time
        {
            get
            {
                return _time;
            }

            set
            {
                if (_time == value)
                {
                    return;
                }

                _time = value;

                try
                {
                    var _text = _time.ToString(Format);
                    if (_time.Hours > 0 && Format.StartsWith("mm"))
                    {
                        var t = _text.Split(':');
                        t[0] = string.Format("{0:00}", _time.Hours * 60 + _time.Minutes);
                        _text = t.Aggregate((a, b) => a + ":" + b);
                    }
                    _changingTime = true;
                    if (SplitText)
                        Text = _text.Select(x => x.ToString()).Aggregate((x, y) => x + "|" + y);
                    else
                        Text = _text;
                    _changingTime = false;
                }
                catch (Exception)
                {
                    Text = "Wrong Format";
                }

                RaisePropertyChanged(nameof(Time));
            }
        }

        //Timer recovering
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == TextProperty)
            {
                if (!_changingTime)
                {
                    TimeSpan parsed = TimeSpan.Zero;
                    if (TimeSpan.TryParse((string)e.NewValue, out parsed))
                    {
                        _time = parsed;
                        RaisePropertyChanged(nameof(Time));
                        if (_time != TimeSpan.Zero)
                            Paused = true;
                    }
                }
            }
        }

        private long _timeTicks = 0;

        /// <summary>
        /// Sets and gets the TimeTicks property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [Browsable(false)]
        public long TimeTicks
        {
            get
            {
                return _time.Ticks;
            }

            set
            {
                if (_timeTicks == value)
                {
                    return;
                }

                _timeTicks = value;
                Time = new TimeSpan(value);
                RaisePropertyChanged(nameof(TimeTicks));
            }
        }

        private TimeSpan _defaultTime = TimeSpan.Zero;

        /// <summary>
        /// Sets and gets the DefaultTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore()]
        public TimeSpan DefaultTime
        {
            get
            {
                return _defaultTime;
            }

            set
            {
                if (_defaultTime == value)
                {
                    return;
                }

                _defaultTime = value;
                UpdateTimer();
                RaisePropertyChanged(nameof(DefaultTime));
            }
        }

        private long _defaultTimeTicks = 0;

        /// <summary>
        /// Sets and gets the DefaultTimeTicks property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [Browsable(false)]
        public long DefaultTimeTicks
        {
            get
            {
                return _defaultTime.Ticks;
            }

            set
            {
                if (_defaultTimeTicks == value)
                {
                    return;
                }

                _defaultTimeTicks = value;
                DefaultTime = new TimeSpan(value);
                RaisePropertyChanged(nameof(DefaultTimeTicks));
            }
        }

        public override void ExecuteHotkey(int index)
        {
            TimerCommand.Execute(Hotkey[index].Name);
            //base.ExecuteHotkey(index);
        }

        [NonSerialized]
        private RelayCommand<string> _timerCommand;

        /// <summary>
        /// Gets the TimerCommand.
        /// </summary>
        public RelayCommand<string> TimerCommand
        {
            get
            {
                return _timerCommand
                    ?? (_timerCommand = new RelayCommand<string>(
                    p =>
                    {
                        switch (p)
                        {
                            case "Start":
                                if (!Paused)
                                    UpdateTimer();

                                Paused = false;
                                Active = true;
                                GlobalTimer.WorkingTimers++;
                                if (!string.IsNullOrWhiteSpace(Links[0]))
                                    Messenger.Default.Send<Pair<string, object>>(new Pair<string, object>(Links[0], null));
                                break;
                            case "Pause":

                                if (!Paused)
                                {
                                    if (Active)
                                    {
                                        Paused = true;
                                        Active = false;
                                        GlobalTimer.WorkingTimers--;
                                        if (!string.IsNullOrWhiteSpace(Links[1]))
                                            Messenger.Default.Send<Pair<string, object>>(new Pair<string, object>(Links[1], null));
                                    }

                                }
                                else
                                {
                                    Paused = false;
                                    Active = true;
                                    GlobalTimer.WorkingTimers++;
                                    if (!string.IsNullOrWhiteSpace(Links[0]))
                                        Messenger.Default.Send<Pair<string, object>>(new Pair<string, object>(Links[0], null));
                                }

                                break;
                            case "Stop":
                                if (Active)
                                {
                                    GlobalTimer.WorkingTimers--;
                                    Active = false;
                                }
                                Paused = false;
                                UpdateTimer();
                                if (!string.IsNullOrWhiteSpace(Links[2]))
                                    Messenger.Default.Send<Pair<string, object>>(new Pair<string, object>(Links[2], null));
                                break;
                            case "+1 Hour":
                                Time = Time.Add(TimeSpan.FromHours(1));
                                break;
                            case "-1 Hour":
                                Time = Time.Subtract(TimeSpan.FromHours(1));
                                break;
                            case "+1 Minute":
                                Time = Time.Add(TimeSpan.FromMinutes(1));
                                break;
                            case "-1 Minute":
                                Time = Time.Subtract(TimeSpan.FromMinutes(1));
                                break;
                            case "+1 Second":
                                Time = Time.Add(TimeSpan.FromSeconds(1));
                                break;
                            case "-1 Second":
                                Time = Time.Subtract(TimeSpan.FromSeconds(1));
                                break;

                        }
                    }));
            }
        }

        public override void SetProperties(vMixWidgetSettingsViewModel viewModel)
        {
            base.SetProperties(viewModel);

        }

        public override void SetProperties(UserControl[] _controls)
        {
            base.SetProperties(_controls);
        }

        protected override void Dispose(bool managed)
        {
            if (_disposed) return;

            if (managed)
            {
                GlobalTimer.WorkingTimers--;
                Messenger.Default.Unregister(this);
                base.Dispose(managed);
                GC.SuppressFinalize(this);
            }
        }

    }
}
