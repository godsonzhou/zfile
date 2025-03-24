using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
// 表达式求值器 doubao
class ExpressionEvaluator
{
	public static object EvaluateExpr(string expr, Dictionary<string, string> parameters)
	{
		// 替换参数
		foreach (var param in parameters)
		{
			expr = expr.Replace(param.Key, param.Value);
		}

		// 移除多余的引号
		expr = Regex.Replace(expr, @"^""|""$", "");

		return EvaluateInternal(expr);
	}

	private static object EvaluateInternal(string expr)
	{
		// 处理括号
		while (expr.Contains("("))
		{
			int startIndex = expr.LastIndexOf("(");
			int endIndex = expr.IndexOf(")", startIndex);
			string subExpr = expr.Substring(startIndex + 1, endIndex - startIndex - 1);
			object subResult = EvaluateInternal(subExpr);
			expr = expr.Remove(startIndex, endIndex - startIndex + 1).Insert(startIndex, subResult.ToString());
		}

		// 处理指数运算
		while (expr.Contains("^"))
		{
			Match match = Regex.Match(expr, @"([\d.]+)\^([\d.]+)");
			if (match.Success)
			{
				double left = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
				double right = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
				double result = Math.Pow(left, right);
				expr = expr.Replace(match.Value, result.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				break;
			}
		}

		// 处理乘除运算
		while (expr.Contains("*") || expr.Contains("/"))
		{
			Match match = Regex.Match(expr, @"([\d.]+)([*/])([\d.]+)");
			if (match.Success)
			{
				double left = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
				string op = match.Groups[2].Value;
				double right = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
				double result;
				if (op == "*")
				{
					result = left * right;
				}
				else
				{
					result = left / right;
				}
				expr = expr.Replace(match.Value, result.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				break;
			}
		}

		// 处理加减运算
		while (expr.Contains("+") || expr.Contains("-"))
		{
			Match match = Regex.Match(expr, @"([\d.]+)([+\-])([\d.]+)");
			if (match.Success)
			{
				double left = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
				string op = match.Groups[2].Value;
				double right = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
				double result;
				if (op == "+")
				{
					result = left + right;
				}
				else
				{
					result = left - right;
				}
				expr = expr.Replace(match.Value, result.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				break;
			}
		}

		// 处理字符串连接
		if (expr.Contains("+") && expr.Contains("'"))
		{
			string[] parts = expr.Split('+');
			string result = "";
			foreach (string part in parts)
			{
				result += part.Trim('\'');
			}
			return result;
		}

		// 处理比较运算
		if (expr.Contains(">") || expr.Contains("<") || expr.Contains("=") || expr.Contains(">=") || expr.Contains("<=") || expr.Contains("!="))
		{
			if (expr.Contains("=") && expr.Contains("'"))
			{
				string[] parts = expr.Split('=');
				string left = parts[0].Trim('\'');
				string right = parts[1].Trim('\'');
				return left == right ? ".true." : ".false.";
			}
			else
			{
				Match match = Regex.Match(expr, @"([\d.]+)([><=!]+)([\d.]+)");
				if (match.Success)
				{
					double left = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
					string op = match.Groups[2].Value;
					double right = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
					bool result;
					switch (op)
					{
						case ">":
							result = left > right;
							break;
						case "<":
							result = left < right;
							break;
						case "=":
							result = left == right;
							break;
						case ">=":
							result = left >= right;
							break;
						case "<=":
							result = left <= right;
							break;
						case "!=":
							result = left != right;
							break;
						default:
							throw new ArgumentException("Unsupported operator: " + op);
					}
					return result ? ".true." : ".false.";
				}
			}
		}

		// 处理逻辑运算
		if (expr.Contains("&") || expr.Contains("|") || expr.Contains("!"))
		{
			if (expr.Contains("&"))
			{
				string[] parts = expr.Split('&');
				bool left = parts[0] == ".true.";
				bool right = parts[1] == ".true.";
				return (left && right) ? ".true." : ".false.";
			}
			else if (expr.Contains("|"))
			{
				string[] parts = expr.Split('|');
				bool left = parts[0] == ".true.";
				bool right = parts[1] == ".true.";
				return (left || right) ? ".true." : ".false.";
			}
			else if (expr.StartsWith("!"))
			{
				string operand = expr.Substring(1);
				bool value = operand == ".true.";
				return (!value) ? ".true." : ".false.";
			}
		}

		// 处理字符串操作
		if (expr.Contains("["))
		{
			Match match = Regex.Match(expr, @"('([^']*)')\[(\d+)\]");
			if (match.Success)
			{
				string str = match.Groups[2].Value;
				int index = int.Parse(match.Groups[3].Value);
				if (index < str.Length)
				{
					return str[index].ToString();
				}
			}
		}

		// 尝试解析为数字
		if (double.TryParse(expr, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
		{
			return number;
		}

		return expr;
	}
}

