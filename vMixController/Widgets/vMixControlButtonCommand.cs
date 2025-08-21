using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using vMixController.Classes;
using vMixController.ViewModel;

namespace vMixController.Widgets
{
    [Serializable]
    public class vMixControlButtonCommand : ObservableObject, ICloneable
    {
        private vMixFunctionReference _action = null;

        /// <summary>
        /// Sets and gets the Action property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public vMixFunctionReference Action
        {
            get
            {
                return _action;
            }

            set
            {
                if (_action == value)
                {
                    return;
                }

                _action = value;
                RaisePropertyChanged(nameof(Action));
            }
        }

        private string _parameter = "-1";

        /// <summary>
        /// Sets and gets the Parameter property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Parameter
        {
            get
            {
                return _parameter;
            }

            set
            {
                if (_parameter == value)
                {
                    return;
                }

                _parameter = value;
                RaisePropertyChanged(nameof(Parameter));
            }
        }

        private int _input = -1;

        /// <summary>
        /// Sets and gets the Input property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int Input
        {
            get
            {
                return _input;
            }

            set
            {
                if (_input == value)
                {
                    return;
                }

                _input = value;
                RaisePropertyChanged(nameof(Input));
            }
        }

        private string _inputKey = null;

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

        private string _stringParameter = "";

        /// <summary>
        /// Sets and gets the StringParameter property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string StringParameter
        {
            get
            {
                return _stringParameter;
            }

            set
            {
                if (_stringParameter == value)
                {
                    return;
                }

                _stringParameter = value;
                RaisePropertyChanged(nameof(StringParameter));
            }
        }

        private List<One<string>> _additionalParameters = new List<One<string>>();

        /// <summary>
        /// Sets and gets the AdditionalParameters property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public List<One<string>> AdditionalParameters
        {
            get
            {
                return _additionalParameters;
            }

            set
            {
                if (_additionalParameters == value)
                {
                    return;
                }

                _additionalParameters = value;
                RaisePropertyChanged(nameof(AdditionalParameters));
            }
        }

        private bool _collapsed = false;

        /// <summary>
        /// Sets and gets the Collapsed property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool Collapsed
        {
            get
            {
                return _collapsed;
            }

            set
            {
                if (_collapsed == value)
                {
                    return;
                }

                _collapsed = value;
                RaisePropertyChanged(nameof(Collapsed));
                RaisePropertyChanged(nameof(AdditionalParameters));
            }
        }

        private bool _noInputAssigned = false;

        /// <summary>
        /// Sets and gets the NoInputAssigned property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool NoInputAssigned
        {
            get
            {
                return _noInputAssigned;
            }

            set
            {
                if (_noInputAssigned == value)
                {
                    return;
                }

                _noInputAssigned = value;
                RaisePropertyChanged(nameof(NoInputAssigned));
            }
        }

        [NonSerialized]
        private Thickness _ident = new Thickness(0);

        /// <summary>
        /// Sets and gets the Collapsed property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Thickness Ident
        {
            get
            {
                return _ident;
            }

            set
            {
                if (_ident == value)
                {
                    return;
                }

                _ident = value;
                RaisePropertyChanged(nameof(Ident));
            }
        }

        private bool _useInActiveState = true;

        /// <summary>
        /// Sets and gets the UseInActiveState property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool UseInActiveState
        {
            get
            {
                return _useInActiveState;
            }

            set
            {
                if (_useInActiveState == value)
                {
                    return;
                }

                _useInActiveState = value;
                RaisePropertyChanged(nameof(UseInActiveState));
            }
        }

        private bool _isExecutable = true;

        /// <summary>
        /// Sets and gets the IsExecutable property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsExecutable
        {
            get
            {
                return _isExecutable;
            }

            set
            {
                if (_isExecutable == value)
                {
                    return;
                }

                _isExecutable = value;
                RaisePropertyChanged(nameof(IsExecutable));
            }
        }

        /// <summary>
        /// Сериализует команду в строку, учитывая сигнатуру функции для включения только необходимых параметров.
        /// Формат: [атрибуты] Функция(параметры)
        /// </summary>
        public override string ToString()
        {
            if (Action == null || string.IsNullOrWhiteSpace(Action.Function))
                return string.Empty;

            var sb = new StringBuilder();

            // 1. Атрибуты (добавляем только отличающиеся от дефолтных)
            if (Collapsed) sb.Append("[C] ");
            if (!IsExecutable) sb.Append("[!E] ");
            if (!UseInActiveState) sb.Append("[!S] ");

            // 2. Имя функции
            sb.Append(Action.Function);
            sb.Append("(");

            // 3. Параметры (добавляем только те, что определены в сигнатуре Action)
            var parameters = new List<string>();

            if (Action.HasInputProperty && !NoInputAssigned)
            {
                // Предпочитаем InputKey, если он задан, иначе используем Input
                parameters.Add(!string.IsNullOrEmpty(InputKey) ? Escape(InputKey) : Input.ToString());
            }
            if (Action.HasIntProperty)
            {
                parameters.Add(Escape(Parameter));
            }
            if (Action.HasStringProperty)
            {
                parameters.Add(Escape(StringParameter));
            }
            if (Action.AdditionalCount > 0 && AdditionalParameters != null)
            {
                parameters.AddRange(AdditionalParameters.Take(Action.AdditionalCount).Select(p => Escape(p.A)));
            }

            sb.Append(string.Join(",", parameters));
            sb.Append(")");

            return sb.ToString();
        }

        /// <summary>
        /// Создает объект vMixControlButtonCommand из строки, используя сигнатуру функции для корректного парсинга параметров.
        /// </summary>
        public static vMixControlButtonCommand FromString(string commandString)
        {
            var allFunctions = SimpleIoc.Default.GetInstance<MainViewModel>().Functions;
            if (string.IsNullOrWhiteSpace(commandString))
                return null;

            var cmd = new vMixControlButtonCommand();
            var remainingString = commandString.Trim();

            // 1. Парсинг атрибутов
            bool attributesParsed = true;
            while (attributesParsed)
            {
                attributesParsed = false;
                if (remainingString.StartsWith("[C] ")) { cmd.Collapsed = true; remainingString = remainingString.Substring(4); attributesParsed = true; }
                if (remainingString.StartsWith("[!E] ")) { cmd.IsExecutable = false; remainingString = remainingString.Substring(5); attributesParsed = true; }
                if (remainingString.StartsWith("[!S] ")) { cmd.UseInActiveState = false; remainingString = remainingString.Substring(5); attributesParsed = true; }
            }

            // 2. Парсинг имени функции и поиск Action
            var openParenIndex = remainingString.IndexOf('(');
            var closeParenIndex = remainingString.LastIndexOf(')');
            if (openParenIndex == -1 || closeParenIndex == -1 || closeParenIndex < openParenIndex)
                return null; // Некорректный формат

            var functionName = remainingString.Substring(0, openParenIndex);
            cmd.Action = allFunctions.FirstOrDefault(f => f.Function.Equals(functionName, StringComparison.OrdinalIgnoreCase));
            if (cmd.Action == null)
                return null; // Функция не найдена

            // 3. Парсинг параметров
            var paramsString = remainingString.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
            List<string> parameters = new List<string>();
            if (!string.IsNullOrEmpty(paramsString))
            {
                parameters = Regex.Matches(paramsString, @"(""[^""\\]*(?:\\.[^""\\]*)*""|[^,]+)")
                                  .Cast<Match>()
                                  .Select(m => m.Value.Trim())
                                  .ToList();
            }

            int currentParamIndex = 0;

            // 4. Распределение параметров по свойствам согласно сигнатуре Action
            if (cmd.Action.HasInputProperty && !cmd.NoInputAssigned)
            {
                if (currentParamIndex < parameters.Count)
                {
                    var inputParam = Unescape(parameters[currentParamIndex]);
                    // Если параметр - число без кавычек, считаем его Input, иначе - InputKey
                    if (int.TryParse(inputParam, out int inputNum) && parameters[currentParamIndex].Trim() == inputParam)
                    {
                        cmd.Input = inputNum;
                        cmd.InputKey = null;
                    }
                    else
                    {
                        cmd.InputKey = inputParam;
                        // Можно установить Input в 0 или -1 как индикатор, что используется ключ
                        cmd.Input = 0;
                    }
                    currentParamIndex++;
                }
            }

            if (cmd.Action.HasIntProperty)
            {
                if (currentParamIndex < parameters.Count)
                    cmd.Parameter = Unescape(parameters[currentParamIndex++]);
            }

            if (cmd.Action.HasStringProperty)
            {
                if (currentParamIndex < parameters.Count)
                    cmd.StringParameter = Unescape(parameters[currentParamIndex++]);
            }

            if (cmd.Action.AdditionalCount > 0)
            {
                cmd.AdditionalParameters = new List<One<string>>();
                for (int i = 0; i < cmd.Action.AdditionalCount; i++)
                {
                    if (currentParamIndex < parameters.Count)
                        cmd.AdditionalParameters.Add(new One<string>() { A = Unescape(parameters[currentParamIndex++]) });
                    else
                        break; // Параметров в строке меньше, чем ожидает функция
                }
            }

            while (cmd.AdditionalParameters.Count < 10)
                cmd.AdditionalParameters.Add(new One<string>() { A = "" });

            return cmd;
        }

        private static string Escape(string s)
        {
            if (s == null) return "\"\"";
            // Оборачиваем в кавычки, если содержит запятую, пробел или уже является строкой в кавычках
            if (s.Contains(",") || s.Contains(" ") || s.StartsWith("\"") || s.Length == 0)
                return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            return s; // Числа и простые строки можно не оборачивать
        }

        private static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.StartsWith("\"") && s.EndsWith("\""))
            {
                string inner = s.Substring(1, s.Length - 2);
                return inner.Replace("\\\"", "\"").Replace("\\\\", "\\");
            }
            return s; // Возвращаем как есть, если это неэкранированная строка (например, число)
        }

        public object Clone()
        {
            return vMixControlButtonCommand.FromString(this.ToString());
        }

        public vMixControlButtonCommand()
        {
            _action = new vMixFunctionReference();

            /*if (_additionalParameters.Count < 10)
                for (int i = 0; i < 10; i++)
                    _additionalParameters.Add("");*/
        }

    }
}
