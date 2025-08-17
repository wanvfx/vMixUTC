using NCalc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public object StringParameter { get; }
        public string KeyByInt { get; }
        public string KeyByString { get; }

        public XPathFormattingArgs(string inputKey, int inputNumber, int intParameter, object stringParameter, string keyByInt, string keyByString)
        {
            InputKey = inputKey;
            InputNumber = inputNumber;
            IntParameter = intParameter;
            StringParameter = stringParameter;
            KeyByInt = keyByInt;
            KeyByString = keyByString;
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

                object rawResult = expression.Evaluate();

                // Если результат уже нужного типа, возвращаем его напрямую.
                if (rawResult is T)
                {
                    result = (T) rawResult;
                    return false;
                }

                // Попытка универсального и безопасного преобразования типа.
                // Convert.ChangeType хорошо работает с базовыми типами и Nullable<T>.
                result = (T)Convert.ChangeType(rawResult, typeof(T), CultureInfo.InvariantCulture);
                return false;
            }
            catch (Exception)
            {
                // Ловим любые ошибки (при вычислении, приведении типов и т.д.)
                // и возвращаем значение по умолчанию для типа T.
                result = ExpressionOrDefaultValue<T>(expressionString);
                return true;
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

        private static void OnEvaluateParameter(string name, NCalc.ParameterArgs args)
        {
            //Avoid non-defined parameters with their names
            args.Result = name;
        }

        /*public static bool CalculateBoolExpression(string s, PopulateVariablesDelegate PopulateVariables, NCalc.EvaluateFunctionHandler Exp_EvaluateFunction)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            NCalc.Expression exp = new NCalc.Expression(s);
            PopulateVariables(exp);

            bool result = false;

            if (exp.HasErrors())
                return false;
            else
                try
                {

                    result = (bool)exp.Evaluate();
                    exp.EvaluateFunction -= Exp_EvaluateFunction;
                    exp.EvaluateParameter -= Exp_EvaluateParameter;
                    exp = null;
                    return result;
                }
                catch (Exception)
                {
                    exp.EvaluateFunction -= Exp_EvaluateFunction;
                    exp.EvaluateParameter -= Exp_EvaluateParameter;
                    exp = null;
                    return false;
                }

        }

        public static object CalculateExpression(string s, PopulateVariablesDelegate PopulateVariables, NCalc.EvaluateFunctionHandler Exp_EvaluateFunction)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            NCalc.Expression exp = new NCalc.Expression(s);
            exp.EvaluateParameter += Exp_EvaluateParameter;
            PopulateVariables(exp);
            object result = null;
            if (exp.HasErrors())
                return s;
            else
            {

                try
                {
                    result = exp.Evaluate();
                    exp.EvaluateFunction -= Exp_EvaluateFunction;
                    exp.EvaluateParameter -= Exp_EvaluateParameter;
                    exp = null;
                    return result;
                }
                catch (Exception)
                {
                    exp.EvaluateFunction -= Exp_EvaluateFunction;
                    exp.EvaluateParameter -= Exp_EvaluateParameter;
                    exp = null;
                    return s;
                }
            }
        }

        public static T CalculateExpression<T>(string s, PopulateVariablesDelegate PopulateVariables, NCalc.EvaluateFunctionHandler Exp_EvaluateFunction)
        {
            var result = CalculateExpression(s, PopulateVariables, Exp_EvaluateFunction);
            try
            {
                //If types are equal
                if (result is T) return (T)result;

                //Try convert
                MethodInfo parse = null;
                Type targetType = typeof(T);
                Type ut = typeof(T);
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    ut = Nullable.GetUnderlyingType(targetType);
                parse = ut.GetMethods(BindingFlags.Static | BindingFlags.Public).Where(x => x.Name == "TryParse" && x.DeclaringType == ut && x.GetParameters().FirstOrDefault()?.ParameterType == typeof(string)).FirstOrDefault();
                if (parse != null && result is string)
                {
                    object[] parameters = new object[] { result, null };
                    object parseResult = parse.Invoke(targetType, parameters);
                    if ((bool)parseResult)
                        return (T)parameters[1];
                    else
                        //return default value if parsing was failed
                        if (ut.IsValueType)
                        return (T)Activator.CreateInstance(ut);
                }

                //Try change type
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch
            {
                return default;
            }
        }*/

        /// <summary>
        /// Определяет, зависит ли состояние от XPath-выражений на основе набора команд.
        /// </summary>
        /// <param name="doc">XML-документ для проверки.</param>
        /// <param name="variableExpander">Функция для раскрытия переменных (например, "[VAR]").</param>
        /// <returns>Объект XPathStateResult, содержащий результат и флаг наличия ошибок.</returns>
        public static XPathStateResult CalculateStateDependency(this ObservableCollection<vMixControlButtonCommand> _commands, XmlDocument doc, Func<string, string> variableExpander, PopulateVariablesDelegate PopulateVariables, NCalc.EvaluateFunctionHandler Exp_EvaluateFunction)
        {
            if (doc == null || variableExpander == null)
            {
                return new XPathStateResult(false, true); // Возвращаем ошибку, если входные данные неверны
            }

            bool isStateDependent = false;
            bool hasErrors = false;

            foreach (var command in _commands)
            {
                if (!command.UseInActiveState)
                {
                    continue;
                }

                // 1. Подготовка всех необходимых аргументов
                PrepareArgsResult preparationResult = PrepareFormattingArgs(doc, command, variableExpander, PopulateVariables, Exp_EvaluateFunction);
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
        private static PrepareArgsResult PrepareFormattingArgs(XmlDocument doc, vMixControlButtonCommand item, Func<string, string> variableExpander, PopulateVariablesDelegate PopulateVariables, NCalc.EvaluateFunctionHandler Exp_EvaluateFunction)
        {
            try
            {
                string expandedInputKey = variableExpander(item.InputKey);

                string inputNumberStr = GetNodeValue(doc, string.Format(@"//inputs/input[@key='{0}']/@number", expandedInputKey)) ?? "-1";
                int inputNumber = Convert.ToInt32(inputNumberStr);

                CalculateExpression<int>(item.Parameter, PopulateVariables, Exp_EvaluateFunction, out int intParam);
                CalculateExpression<object>(item.StringParameter, PopulateVariables, Exp_EvaluateFunction, out object strParam);

                int strParamAsInt;
                // Использование 'out var' доступно в C# 7.0, поддерживаемом .NET 4.7.2
                if (strParam == null || !int.TryParse(strParam.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out strParamAsInt))
                {
                    strParamAsInt = int.MinValue;
                }

                string keyByInt = GetNodeValue(doc, string.Format(@"//inputs/input[@number='{0}']/@key", intParam));
                string keyByString = GetNodeValue(doc, string.Format(@"//inputs/input[@number='{0}']/@key", strParamAsInt));

                var args = new XPathFormattingArgs(expandedInputKey, inputNumber, intParam, strParam, keyByInt, keyByString);
                return new PrepareArgsResult(args, false);
            }
            catch (Exception) // Ловим ошибки парсинга или вычислений
            {
                return new PrepareArgsResult(null, true);
            }
        }

        /// <summary>
        /// Определяет и форматирует итоговый XPath-путь для выполнения.
        /// </summary>
        private static string GetEffectiveXPath(vMixFunctionReference action, XPathFormattingArgs args)
        {
            string pathTemplate = null;

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
                args.InputNumber, "", args.KeyByInt, args.KeyByString);
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
