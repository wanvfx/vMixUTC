using NCalc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using vMixController.Classes;

namespace vMixController.Widgets.Button
{
    #region Вспомогательные классы для передачи данных

    /// <summary>
    /// Класс для хранения результата выполнения основной функции.
    /// Заменяет кортеж (bool, bool).
    /// </summary>
    public class XPathStateResult
    {
        public bool IsStateDependent { get; }
        public bool HasErrors { get; }

        public XPathStateResult(bool isStateDependent, bool hasErrors)
        {
            IsStateDependent = isStateDependent;
            HasErrors = hasErrors;
        }
    }

    /// <summary>
    /// Неизменяемый класс для передачи аргументов форматирования.
    /// Заменяет тип 'record'.
    /// </summary>
    public class XPathFormattingArgs
    {
        public string InputKey { get; }
        public int InputNumber { get; }
        public int IntParameter { get; }
        public string FloatParameter { get; }
        public object StringParameter { get; }
        public string KeyByInt { get; }
        public string KeyByString { get; }

        public XPathFormattingArgs(string inputKey, int inputNumber, int intParameter, object stringParameter, string keyByInt, string keyByString, string floatParameter)
        {
            InputKey = inputKey;
            InputNumber = inputNumber;
            IntParameter = intParameter;
            StringParameter = stringParameter;
            KeyByInt = keyByInt;
            KeyByString = keyByString;
            FloatParameter = floatParameter;
        }
    }

    public delegate void PopulateVariablesDelegate(NCalc.Expression expression);

    /// <summary>
    /// Класс для хранения результата подготовки аргументов.
    /// Заменяет кортеж (XPathFormattingArgs, bool).
    /// </summary>
    public class PrepareArgsResult
    {
        public XPathFormattingArgs Args { get; }
        public bool HasError { get; }

        public PrepareArgsResult(XPathFormattingArgs args, bool hasError)
        {
            Args = args;
            HasError = hasError;
        }
    }

    #endregion
    public static class vMixControlButtonHelper
    {
        /// <summary>
        /// Вычисляет строковое выражение и безопасно преобразует результат в указанный тип T.
        /// Этот метод заменяет CalculateExpression, CalculateBoolExpression и CalculateExpression<T>.
        /// </summary>
        /// <typeparam name="T">Целевой тип результата (например, bool, int, string, double).</typeparam>
        /// <param name="expressionString">Строка с выражением для вычисления.</param>
        /// <param name="populateVariables">Делегат для заполнения выражения переменными.</param>
        /// <param name="evaluateFunctionHandler">Обработчик для пользовательских функций в выражении.</param>
        /// <param name="result">Результат вычисления выражения.</param>
        /// <returns>Была ли ошибка при вычислении.</returns>
        public static bool CalculateExpression<T>(string expressionString, PopulateVariablesDelegate populateVariables, EvaluateFunctionHandler evaluateFunctionHandler, out T result)
        {
            if (string.IsNullOrWhiteSpace(expressionString))
            {
                // Для T=string вернет null, для T=bool вернет false, для T=int вернет 0.
                result = default(T);
                return false;
            }

            var expression = new Expression(expressionString);

            // Подписываемся на события. Отписка будет в блоке finally, что гарантирует ее выполнение.
            if (evaluateFunctionHandler != null)
            {
                expression.EvaluateFunction += evaluateFunctionHandler;
            }
            expression.EvaluateParameter += OnEvaluateParameter;

            try
            {
                if (populateVariables != null)
                {
                    populateVariables(expression);
                }

                if (expression.HasErrors())
                {
                    result = ExpressionOrDefaultValue<T>(expressionString);
                    return true;
                }

                object rawResult = null;
                if (expression.TryEvaluate(out rawResult, out _))
                {
                    // Если результат уже нужного типа, возвращаем его напрямую.
                    if (rawResult is T)
                    {
                        result = (T)rawResult;
                        return false;
                    }

                    //Обработка для случая возврата строк в числовом параметре
                    if (typeof(T) == typeof(int))
                    {
                        if (!int.TryParse(rawResult.ToString(), out var intResult))
                        {
                            result = ExpressionOrDefaultValue<T>(expressionString);
                            return true;
                        }
                    }
                    // Попытка универсального и безопасного преобразования типа.
                    // Convert.ChangeType хорошо работает с базовыми типами и Nullable<T>.
                    result = (T)Convert.ChangeType(rawResult, typeof(T), CultureInfo.InvariantCulture);
                    return false;
                }
                else
                {
                    result = ExpressionOrDefaultValue<T>(expressionString);
                    return true;
                }
            }
            finally
            {
                // Гарантированная отписка от событий, чтобы избежать утечек памяти.
                if (evaluateFunctionHandler != null)
                {
                    expression.EvaluateFunction -= evaluateFunctionHandler;
                }
                expression.EvaluateParameter -= OnEvaluateParameter;
            }
        }

        private static T ExpressionOrDefaultValue<T>(string expressionString)
        {
            T result;
            if (typeof(T) == typeof(string) || typeof(T) == typeof(object))
                result = (T)((object)expressionString);
            else
                result = default(T);
            return result;
        }

        private static void OnEvaluateParameter(string name, ParameterArgs args)
        {
            //Avoid non-defined parameters with their names
            args.Result = name;
        }

        /// <summary>
        /// Определяет, зависит ли состояние от XPath-выражений на основе набора команд.
        /// </summary>
        /// <param name="doc">XML-документ для проверки.</param>
        /// <param name="variableExpander">Функция для раскрытия переменных (например, "[VAR]").</param>
        /// <returns>Объект XPathStateResult, содержащий результат и флаг наличия ошибок.</returns>
        public static XPathStateResult CalculateStateDependency(this ObservableCollection<vMixControlButtonCommand> _commands, XmlDocument doc, Func<string, string> variableExpander, PopulateVariablesDelegate PopulateVariables, EvaluateFunctionHandler Exp_EvaluateFunction)
        {
            if (doc == null || variableExpander == null)
            {
                return new XPathStateResult(false, true); // Возвращаем ошибку, если входные данные неверны
            }

            bool isStateDependent = false;
            bool hasErrors = false;
            BuildInputMaps(doc, out var inputNumberByKey, out var inputKeyByNumber);

            foreach (var command in _commands)
            {
                if (!command.UseInActiveState)
                {
                    continue;
                }

                // 1. Подготовка всех необходимых аргументов
                PrepareArgsResult preparationResult = PrepareFormattingArgs(doc, command, variableExpander, PopulateVariables, Exp_EvaluateFunction, inputNumberByKey, inputKeyByNumber);
                if (preparationResult.HasError)
                {
                    hasErrors = true;
                    continue; // Пропускаем команду, если не удалось подготовить аргументы
                }
                XPathFormattingArgs args = preparationResult.Args;

                // 2. Получение и форматирование XPath-пути
                string xpath = GetEffectiveXPath(command.Action, args);
                if (string.IsNullOrWhiteSpace(xpath))
                {
                    continue;
                }

                // 3. Извлечение фактического значения из XML
                string actualValue = GetNodeValue(doc, xpath);
                if (actualValue == null) // Узел не найден
                {
                    continue;
                }

                // 4. Форматирование ожидаемого значения и сравнение
                string expectedValuePattern = string.Format(command.Action.ActiveStateValue,
                    args.InputNumber, args.IntParameter, args.StringParameter, args.IntParameter - 1,
                    args.InputNumber, "", args.KeyByInt, args.KeyByString);

                if (ValuesMatch(actualValue, expectedValuePattern))
                {
                    isStateDependent = true;
                }
            }

            return new XPathStateResult(isStateDependent, hasErrors);
        }

        /// <summary>
        /// Готовит и вычисляет все аргументы, необходимые для форматирования строк XPath и значений.
        /// </summary>
        private static PrepareArgsResult PrepareFormattingArgs(XmlDocument doc, vMixControlButtonCommand item, Func<string, string> variableExpander, PopulateVariablesDelegate PopulateVariables, EvaluateFunctionHandler Exp_EvaluateFunction, Dictionary<string, int> inputNumberByKey, Dictionary<int, string> inputKeyByNumber)
        {
            try
            {
                string expandedInputKey = variableExpander(item.InputKey);

                int inputNumber = inputNumberByKey.TryGetValue(expandedInputKey, out var numberByKey)
                    ? numberByKey
                    : -1;

                CalculateExpression<int>(item.Parameter, PopulateVariables, Exp_EvaluateFunction, out int intParam);
                CalculateExpression<object>(item.StringParameter, PopulateVariables, Exp_EvaluateFunction, out object strParam);
                CalculateExpression<float>(item.Parameter, PopulateVariables, Exp_EvaluateFunction, out float floatParam);

                int strParamAsInt;
                // Использование 'out var' доступно в C# 7.0, поддерживаемом .NET 4.7.2
                if (strParam == null || !int.TryParse(strParam.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out strParamAsInt))
                {
                    strParamAsInt = int.MinValue;
                }

                inputKeyByNumber.TryGetValue(intParam, out string keyByInt);
                inputKeyByNumber.TryGetValue(strParamAsInt, out string keyByString);

                var fps = floatParam.ToString(CultureInfo.InvariantCulture);

                var args = new XPathFormattingArgs(expandedInputKey, inputNumber, intParam, strParam, keyByInt, keyByString, item.Action.CommaFloatDelimiter ? fps.Replace('.', ',') : fps);
                return new PrepareArgsResult(args, false);
            }
            catch (Exception) // Ловим ошибки парсинга или вычислений
            {
                return new PrepareArgsResult(null, true);
            }
        }

        private static void BuildInputMaps(XmlDocument doc, out Dictionary<string, int> inputNumberByKey, out Dictionary<int, string> inputKeyByNumber)
        {
            inputNumberByKey = new Dictionary<string, int>(StringComparer.Ordinal);
            inputKeyByNumber = new Dictionary<int, string>();

            if (doc == null)
                return;

            var nodes = doc.SelectNodes("//inputs/input");
            if (nodes == null)
                return;

            foreach (XmlNode node in nodes)
            {
                var key = node.Attributes?["key"]?.Value;
                var numberStr = node.Attributes?["number"]?.Value;
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(numberStr))
                    continue;

                if (!int.TryParse(numberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                    continue;

                inputNumberByKey[key] = number;
                inputKeyByNumber[number] = key;
            }
        }

        /// <summary>
        /// Определяет и форматирует итоговый XPath-путь для выполнения.
        /// </summary>
        private static string GetEffectiveXPath(vMixFunctionReference action, XPathFormattingArgs args)
        {
            string pathTemplate = null;

            if (action == null) return null;

            if (action.ActiveStateXPathIntDependence != null && args.IntParameter >= 0 && args.IntParameter < action.ActiveStateXPathIntDependence.Length)
            {
                pathTemplate = action.ActiveStateXPathIntDependence[args.IntParameter];
            }
            else if (!string.IsNullOrWhiteSpace(action.ActiveStateXPath))
            {
                pathTemplate = action.ActiveStateXPath;
            }

            if (string.IsNullOrWhiteSpace(pathTemplate))
            {
                return null;
            }

            return string.Format(pathTemplate,
                args.InputKey, args.IntParameter, args.StringParameter, args.IntParameter - 1,
                args.InputNumber, "", args.KeyByInt, args.KeyByString, args.FloatParameter);
        }

        /// <summary>
        /// Безопасно извлекает значение из XML-узла по XPath.
        /// </summary>
        /// <returns>Строковое значение узла или null, если узел не найден.</returns>
        private static string GetNodeValue(XmlDocument doc, string xpath)
        {
            XmlNode node = doc.SelectSingleNode(xpath);
            if (node == null) return null;

            XmlAttribute attr = node as XmlAttribute;
            return attr != null ? attr.Value : node.InnerText;
        }

        /// <summary>
        /// Сравнивает фактическое значение с ожидаемым шаблоном, учитывая операторы.
        /// </summary>
        private static bool ValuesMatch(string actualValue, string expectedValuePattern)
        {
            if (expectedValuePattern == null) return false;

            char operatorChar = expectedValuePattern.Length > 0 ? expectedValuePattern[0] : ' ';
            bool isNegated = operatorChar == '!';

            if (isNegated)
            {
                expectedValuePattern = expectedValuePattern.Substring(1);
                operatorChar = expectedValuePattern.Length > 0 ? expectedValuePattern[0] : ' ';
            }

            string valueToCompare = expectedValuePattern;
            if (operatorChar == '~' || operatorChar == '`')
            {
                valueToCompare = expectedValuePattern.Substring(1);
            }

            bool result;
            switch (operatorChar)
            {
                case '~': // contains
                    result = actualValue.IndexOf(valueToCompare, StringComparison.Ordinal) >= 0;
                    break;
                case '`': // not contains
                    result = actualValue.IndexOf(valueToCompare, StringComparison.Ordinal) < 0;
                    break;
                default:
                    if (expectedValuePattern == "*") // wildcard
                        result = true;
                    else if (expectedValuePattern == "-") // check for null/whitespace
                        result = string.IsNullOrWhiteSpace(actualValue);
                    else // direct equality
                        result = actualValue == expectedValuePattern;
                    break;
            }

            return isNegated ? !result : result;
        }
    }
}
