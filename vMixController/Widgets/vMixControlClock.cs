using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization; // Добавляем для OnDeserialized
using System.Windows.Controls;
using System.Windows.Threading;
using vMixController.Classes;
using vMixController.Extensions;

namespace vMixController.Widgets
{

    [Serializable]
    public class vMixControlClock : vMixControl
    {
        public override string Type => "Clock";
        public override int MaxCount => 1;

        // --- Поля ---

        // Таймер остается прежним, но с интервалом в 1 секунду - чаще не нужно.
        [NonSerialized]
        private DispatcherTimer _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromSeconds(1) };

        // Храним отсортированную копию для быстрой обработки
        [NonSerialized]
        private List<ScheduledEvent> _sortedEvents = new List<ScheduledEvent>();

        // Используем HashSet для быстрой проверки сработавших событий (O(1) в среднем)
        [NonSerialized]
        private HashSet<ScheduledEvent> _firedEventsToday = new HashSet<ScheduledEvent>();

        [NonSerialized]
        private DateTime _lastTickDate = DateTime.MinValue;

        // --- Свойства MVVM ---

        /// <summary>
        /// Основная коллекция событий для привязки к UI.
        /// Используем новую, строго типизированную модель.
        /// </summary>
        public ObservableCollection<ScheduledEvent> Events { get; set; } = new ObservableCollection<ScheduledEvent>();

        private string _nextEventAt = "";

        /// <summary>
        /// Sets and gets the NextEvetnAt property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string NextEventAt
        {
            get
            {
                return _nextEventAt;
            }

            set
            {
                if (_nextEventAt == value)
                {
                    return;
                }

                _nextEventAt = value;
                RaisePropertyChanged(nameof(NextEventAt));
            }
        }


        // --- Конструктор и методы ---

        public vMixControlClock()
        {
            _timer.Tick += Timer_Tick;
            // Подписываемся на изменение коллекции, чтобы поддерживать _sortedEvents в актуальном состоянии
            Events.CollectionChanged += (s, e) => UpdateSortedEvents();
        }

        // Метод, вызываемый после десериализации
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Пересоздаем таймер, так как он [NonSerialized]
            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            // Пересоздаем HashSet, так как он [NonSerialized]
            _firedEventsToday = new HashSet<ScheduledEvent>();

            // Инициализируем _lastTickDate текущей датой, чтобы при первом тике не было ложного срабатывания "нового дня"
            // и чтобы события, уже прошедшие сегодня, не запускались
            _lastTickDate = DateTime.Now.Date;

            // Подписываемся на изменение коллекции (если она была десериализована)
            if (Events != null)
            {
                Events.CollectionChanged += (s, e) => UpdateSortedEvents();
            }
            else
            {
                // Если Events == null после десериализации (что маловероятно, если есть конструктор),
                // то инициализируем его
                Events = new ObservableCollection<ScheduledEvent>();
                Events.CollectionChanged += (s, e) => UpdateSortedEvents();
            }
            UpdateSortedEvents(); // Обновляем отсортированный список после десериализации
        }


        private void UpdateSortedEvents()
        {
            _sortedEvents = Events.OrderBy(x => x.TimeOfDay).ToList();
            // После изменения списка событий нужно пересчитать следующее событие
            UpdateNextEventDisplay();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;

            // 1. Проверяем, не наступил ли новый день
            // Если _lastTickDate.Date == DateTime.MinValue.Date, это означает первый тик после инициализации/десериализации.
            // В этом случае мы не очищаем _firedEventsToday, а инициализируем его.
            if (_lastTickDate.Date != now.Date)
            {
                _firedEventsToday.Clear();
                Debug.Print("New day detected. Fired events cleared.");
            }
            _lastTickDate = now; // Обновляем _lastTickDate на текущее время

            // 2. Определяем сегодняшний день недели
            var today = ToDaysOfWeek(now.DayOfWeek);

            // 3. Ищем и запускаем события, которые должны были сработать
            foreach (var ev in _sortedEvents)
            {
                // Условия срабатывания:
                // - Событие запланировано на сегодня
                // - Время события уже наступило
                // - Событие еще не срабатывало сегодня
                if (ev.Days.HasFlag(today) && now >= ev.TimeOfDay && !_firedEventsToday.Contains(ev))
                {
                    Messenger.Default.Send(new Pair<string, object>(ev.Command, null));
                    _firedEventsToday.Add(ev);
                    Debug.Print($"Event '{ev.Command}' at {ev.TimeOfDay} fired.");

                    // После срабатывания события, немедленно обновляем информацию о следующем
                    UpdateNextEventDisplay();
                }
            }

            // Обновляем отображение следующего события при каждом тике,
            // чтобы оно всегда было актуальным (например, если текущее "следующее" событие прошло)
            UpdateNextEventDisplay();
        }

        /// <summary>
        /// Находит следующее запланированное событие и обновляет свойство NextEventAt.
        /// </summary>
        private void UpdateNextEventDisplay()
        {
            var next = FindNextScheduledEvent();
            if (next?.Event != null)
            {
                // Используем now.Date для сравнения, чтобы корректно определить "Сегодня"
                string dayString = next?.Date.Date == DateTime.Now.Date ? "Today" : next?.Date.ToString("dddd", CultureInfo.InvariantCulture);
                NextEventAt = $"Next Event: <{next?.Event.Command}> at {next?.Event.TimeOfDay:HH\\:mm\\:ss} on {dayString}";
                // Локализация может быть добавлена здесь
                // NextEventAt = string.Format("{0}: <{1}> {2} {3:hh\\:mm\\:ss} {4} {5}", 
                //      LocalizationManager.Get("Next Event"), next.Event.Command, LocalizationManager.Get("at"), 
                //      next.Event.TimeOfDay, LocalizationManager.Get("on"), dayString);
            }
            else
            {
                NextEventAt = "No new events scheduled";
                // NextEventAt = LocalizationManager.Get("No new events scheduled");
            }
        }

        /// <summary>
        /// Ищет следующее по расписанию событие в течение ближайшей недели.
        /// </summary>
        private (ScheduledEvent Event, DateTime Date)? FindNextScheduledEvent()
        {
            if (_sortedEvents.Count == 0) return null;

            var now = DateTime.Now;

            // 1. Ищем событие сегодня, но позже текущего времени
            foreach (var ev in _sortedEvents)
            {
                if (ev.Days.HasFlag(ToDaysOfWeek(now.DayOfWeek)) && ev.TimeOfDay > now)
                    return (ev, now);
            }

            // 2. Если сегодня больше ничего нет, ищем в последующие 7 дней
            for (int i = 1; i <= 7; i++)
            {
                var nextDay = now.AddDays(i);
                var dayOfWeek = ToDaysOfWeek(nextDay.DayOfWeek);
                foreach (var ev in _sortedEvents)
                {
                    if (ev.Days.HasFlag(dayOfWeek))
                        // Возвращаем событие и дату, на которую оно приходится
                        return (ev, nextDay);
                }
            }

            return null; // Ничего не найдено в течение недели
        }

        // Вспомогательный метод для конвертации DayOfWeek в наш enum
        private static DaysOfWeek ToDaysOfWeek(DayOfWeek day)
        {
            // Здесь предполагается, что DaysOfWeek имеет битовые флаги,
            // где Monday = 1, Tuesday = 2, ..., Sunday = 64 (или 1 << 0, 1 << 1, ..., 1 << 6)
            // и DayOfWeek.Monday = 1, DayOfWeek.Tuesday = 2, ..., DayOfWeek.Sunday = 0
            // Поэтому нужно преобразование.
            // Пример: DayOfWeek.Sunday (0) -> DaysOfWeek.Sunday (1 << 6)
            //         DayOfWeek.Monday (1) -> DaysOfWeek.Monday (1 << 0)
            //         ...
            //         DayOfWeek.Saturday (6) -> DaysOfWeek.Saturday (1 << 5)

            // Более универсальный способ, если DaysOfWeek соответствует DayOfWeek напрямую:
            // return (DaysOfWeek)(1 << (int)day);
            // Но если DaysOfWeek начинается с Monday = 1, а DayOfWeek.Monday = 1, то:
            // return (DaysOfWeek)(1 << ((int)day == 0 ? 6 : (int)day - 1)); // Если DayOfWeek.Sunday = 0
            // Или, как у вас:
            return (DaysOfWeek)(1 << (((int)day + 6) % 7)); // Предполагает, что DayOfWeek.Monday = 1, а DaysOfWeek.Monday = 1 << 0
        }


        // --- Переопределенные методы базового класса ---

        public override void Update()
        {
            if (!_timer.IsEnabled)
            {
                // При первом запуске или после десериализации, инициализируем состояние
                // _firedEventsToday и _lastTickDate, чтобы избежать запуска уже прошедших событий.
                // Это должно быть сделано перед UpdateSortedEvents, если UpdateSortedEvents вызывает UpdateNextEventDisplay,
                // который может зависеть от корректного состояния.
                InitializeClockState();
                UpdateSortedEvents(); // Первоначальная сортировка и обновление NextEventAt
                _timer.Start();
            }
            base.Update();
        }

        /// <summary>
        /// Инициализирует внутреннее состояние часов, чтобы предотвратить запуск событий,
        /// которые уже прошли в текущем дне при загрузке.
        /// </summary>
        private void InitializeClockState()
        {
            _firedEventsToday.Clear(); // Очищаем на всякий случай
            _lastTickDate = DateTime.Now.Date; // Устанавливаем дату последнего тика на текущую дату

            var now = DateTime.Now;
            var today = ToDaysOfWeek(now.DayOfWeek);

            // Проходим по всем событиям и добавляем в _firedEventsToday те,
            // которые должны были сработать до текущего момента сегодня.
            foreach (var ev in _sortedEvents)
            {
                if (ev.Days.HasFlag(today) && ev.TimeOfDay <= now)
                {
                    _firedEventsToday.Add(ev);
                    Debug.Print($"Event '{ev.Command}' at {ev.TimeOfDay} marked as already fired for today.");
                }
            }
        }


        // Методы GetPropertiesControls и SetProperties потребуют адаптации под новую структуру ScheduledEvent.
        // Это зависит от реализации PropertiesControls.SchedulerControl.
        // Предположим, что он теперь работает с ObservableCollection<ScheduledEvent>.
        public override void BeforePropertiesChanged()
        {
            _timer.Stop();
            base.BeforePropertiesChanged();
        }

        public override void AfterPropertiesChanged()
        {
            base.AfterPropertiesChanged();
            UpdateSortedEvents(); // Обновляем отсортированный список
            // После изменения свойств, возможно, нужно переинициализировать состояние,
            // если пользователь изменил расписание.
            InitializeClockState();
            _timer.Start();
        }

        protected override void Dispose(bool managed)
        {
            if (_disposed) return;

            if (managed)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }
            base.Dispose(managed);
        }
    }
}