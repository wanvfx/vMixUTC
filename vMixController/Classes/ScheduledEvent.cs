using System;
using GalaSoft.MvvmLight;
using System.Runtime.Serialization; // <-- Важно добавить этот using

namespace vMixController.Classes
{
    /// <summary>
    /// Вспомогательный enum с флагами для дней недели.
    /// Атрибут Flags позволяет корректно работать с битовыми операциями (например, .HasFlag()).
    /// </summary>
    [Flags]
    [Serializable] // Явно помечаем enum как сериализуемый для ясности
    public enum DaysOfWeek
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64,
        Everyday = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
    }


    /// <summary>
    /// Класс для хранения информации о событии.
    /// Поддерживает уведомления об изменении свойств для MVVM и бинарную сериализацию.
    /// </summary>
    [Serializable]
    public class ScheduledEvent : ObservableObject, IComparable<ScheduledEvent>, ISerializable
    {
        private DateTime _timeOfDay;
        public DateTime TimeOfDay
        {
            get => _timeOfDay;
            set => Set(ref _timeOfDay, value);
        }

        private string _command;
        public string Command
        {
            get => _command;
            set => Set(ref _command, value);
        }

        private DaysOfWeek _days;
        public DaysOfWeek Days
        {
            get => _days;
            set => Set(ref _days, value);
        }

        /// <summary>
        /// Публичный конструктор без параметров необходим для
        /// создания новых экземпляров в UI и для некоторых видов сериализации.
        /// </summary>
        public ScheduledEvent() { }


        #region ISerializable Implementation

        /// <summary>
        /// Специальный конструктор, используемый для десериализации.
        /// Он необходим, потому что базовый класс ObservableObject не является сериализуемым.
        /// </summary>
        protected ScheduledEvent(SerializationInfo info, StreamingContext context)
        {
            // Восстанавливаем состояние объекта из потока
            TimeOfDay = (DateTime)info.GetValue("TimeOfDay", typeof(DateTime));
            Command = info.GetString("Command");
            Days = (DaysOfWeek)info.GetValue("Days", typeof(DaysOfWeek));
        }

        /// <summary>
        /// Метод для сериализации объекта.
        /// Мы явно указываем, какие данные нужно сохранить.
        /// </summary>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("TimeOfDay", TimeOfDay);
            info.AddValue("Command", Command);
            info.AddValue("Days", Days);
        }

        #endregion


        /// <summary>
        /// Для удобной сортировки по времени
        /// </summary>
        public int CompareTo(ScheduledEvent other)
        {
            if (other == null) return 1;
            return TimeOfDay.CompareTo(other.TimeOfDay);
        }
    }
}
