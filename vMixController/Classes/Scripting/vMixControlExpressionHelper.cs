using NCalc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace vMixController.Classes.Scripting
{
    public static class vMixControlExpressionHelper
    {
        public static bool CalculateExpression<T>(string expressionString, Action<Expression> populateVariables, EvaluateFunctionHandler evaluateFunctionHandler, out T result)
        {
            if (string.IsNullOrWhiteSpace(expressionString))
            {
                result = default(T);
                return false;
            }

            var expression = new Expression(expressionString);

            if (evaluateFunctionHandler != null)
            {
                expression.EvaluateFunction += evaluateFunctionHandler;
            }
            expression.EvaluateParameter += OnEvaluateParameter;

            try
            {
                populateVariables?.Invoke(expression);

                if (expression.HasErrors())
                {
                    result = ExpressionOrDefaultValue<T>(expressionString);
                    return true;
                }

                object rawResult = null;
                if (!expression.TryEvaluate(out rawResult, out _))
                {
                    result = ExpressionOrDefaultValue<T>(expressionString);
                    return true;
                }

                if (rawResult is T)
                {
                    result = (T)rawResult;
                    return false;
                }

                if (typeof(T) == typeof(int) && !int.TryParse(rawResult?.ToString(), out _))
                {
                    result = ExpressionOrDefaultValue<T>(expressionString);
                    return true;
                }

                result = (T)Convert.ChangeType(rawResult, typeof(T), CultureInfo.InvariantCulture);
                return false;
            }
            finally
            {
                if (evaluateFunctionHandler != null)
                {
                    expression.EvaluateFunction -= evaluateFunctionHandler;
                }
                expression.EvaluateParameter -= OnEvaluateParameter;
            }
        }

        public static void BuildInputMaps(XmlDocument doc, out Dictionary<string, int> inputNumberByKey, out Dictionary<int, string> inputKeyByNumber)
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

        public static string GetNodeValue(XmlDocument doc, string xpath)
        {
            XmlNode node = doc.SelectSingleNode(xpath);
            if (node == null) return null;

            XmlAttribute attr = node as XmlAttribute;
            return attr != null ? attr.Value : node.InnerText;
        }

        private static T ExpressionOrDefaultValue<T>(string expressionString)
        {
            if (typeof(T) == typeof(string) || typeof(T) == typeof(object))
                return (T)(object)expressionString;

            return default(T);
        }

        private static void OnEvaluateParameter(string name, ParameterArgs args)
        {
            args.Result = name;
        }
    }
}
