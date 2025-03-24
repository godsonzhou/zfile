using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

public class ExpressionEvaluatorDS
{
	private enum TokenType { Number, Boolean, String, Operator, Punctuation, Identifier, Function }
	private record Token(TokenType Type, string Value);

	// 运算符优先级定义
	private static readonly Dictionary<string, int> Precedence = new()
	{
		{ "!", 7 }, { "^", 6 },
		{ "*", 5 }, { "/", 5 }, { "%", 5 },
		{ "+", 4 }, { "-", 4 },
		{ ">", 3 }, { "<", 3 }, { ">=", 3 }, { "<=", 3 }, { "=", 3 }, {"!=", 3 },
		{ "&", 2 }, { "|", 1 }
	};

	public static object EvalExpr(string expr, Dictionary<string, string> parameters)
	{
		var parsedParams = ParseParameters(parameters);
		var processedExpr = ReplaceVariables(expr, parsedParams);
		return EvaluateExpression(processedExpr);
	}

	private static Dictionary<string, object> ParseParameters(Dictionary<string, string> parameters)
	{
		var result = new Dictionary<string, object>();
		foreach (var kvp in parameters)
		{
			result[kvp.Key] = ParseValue(kvp.Value);
		}
		return result;
	}

	private static object ParseValue(string value)
	{
		if (value.StartsWith("'") && value.EndsWith("'"))
			return value.Substring(1, value.Length - 2);

		if (value == ".true.") return true;
		if (value == ".false.") return false;

		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
			return num;

		throw new ArgumentException($"Invalid parameter value: {value}");
	}

	private static string ReplaceVariables(string expr, Dictionary<string, object> parameters)
	{
		return Regex.Replace(expr, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", match =>
		{
			var varName = match.Groups[1].Value;
			if (parameters.TryGetValue(varName, out object value))
			{
				return value switch
				{
					string s => $"'{s}'",
					bool b => b ? ".true." : ".false.",
					_ => value.ToString()
				};
			}
			throw new KeyNotFoundException($"Variable '{varName}' not found");
		});
	}

	private static object EvaluateExpression(string expr)
	{
		var tokens = Tokenize(expr);
		var rpn = ConvertToRPN(tokens);
		return EvaluateRPN(rpn);
	}

	private static List<Token> Tokenize(string expr)
	{
		var tokenDefinitions = new[]
		{
			new { Pattern = @"\d+\.?\d*", Type = TokenType.Number },
			new { Pattern = @"\.true\.|\.false\.", Type = TokenType.Boolean },
			new { Pattern = @"'[^']*'", Type = TokenType.String },
			new { Pattern = @">=|<=|!=|=|>|<", Type = TokenType.Operator },
			new { Pattern = @"\^|\+|-|\*|/|%|!|&|\|", Type = TokenType.Operator },
			new { Pattern = @"\[|\]|\(|\)|,", Type = TokenType.Punctuation },
			new { Pattern = @"[a-zA-Z_][a-zA-Z0-9_]*", Type = TokenType.Identifier }
		};

		var tokens = new List<Token>();
		int pos = 0;

		while (pos < expr.Length)
		{
			if (char.IsWhiteSpace(expr[pos]))
			{
				pos++;
				continue;
			}

			bool matched = false;
			foreach (var def in tokenDefinitions)
			{
				var match = Regex.Match(expr.Substring(pos), $"^{def.Pattern}");
				if (match.Success)
				{
					tokens.Add(new Token(def.Type, match.Value));
					pos += match.Length;
					matched = true;
					break;
				}
			}

			if (!matched) throw new FormatException($"Invalid character at position {pos}");
		}

		return tokens;
	}
	private static List<Token> ConvertToRPN(List<Token> tokens)
	{
		// Shunting-yard algorithm implementation
		// (处理运算符优先级和函数调用)
		var output = new List<Token>();
		var stack = new Stack<Token>();
		// 扩展运算符优先级和结合性定义
		var precedence = new Dictionary<string, (int prec, bool rightAssoc)>()
		{
			{ "!", (8, true) },    // 逻辑非（右结合）
			{ "^", (7, true) },    // 指数（右结合）
			{ "*", (6, false) }, { "/", (6, false) }, { "%", (6, false) },
			{ "+", (5, false) }, { "-", (5, false) },
			{ ">", (4, false) }, { "<", (4, false) }, { ">=", (4, false) },
			{ "<=", (4, false) }, { "=", (4, false) }, { "!=", (4, false) },
			{ "&", (3, false) },   // 逻辑与
			{ "|", (2, false) },   // 逻辑或
			{ "[", (9, false) },   // 索引运算符
			{ "(", (0, false) }, { ")", (0, false) }, { ",", (0, false) }
		};

		for (int i = 0; i < tokens.Count; i++)
		{
			var token = tokens[i];
			switch (token.Type)
			{
				case TokenType.Number:
				case TokenType.Boolean:
				case TokenType.String:
					output.Add(token);
					break;

				case TokenType.Identifier:
					// 处理函数调用
					if (i + 1 < tokens.Count && tokens[i + 1].Value == "(")
					{
						stack.Push(new Token(TokenType.Function, token.Value));
						i++; // 跳过左括号
					}
					else
					{
						output.Add(token);
					}
					break;

				case TokenType.Punctuation:
					switch (token.Value)
					{
						case "(":
							stack.Push(token);
							break;

						case ")":
							while (stack.Count > 0 && stack.Peek().Value != "(")
							{
								output.Add(stack.Pop());
							}
							stack.Pop(); // 弹出左括号
							if (stack.Count > 0 && stack.Peek().Type == TokenType.Function)
							{
								var func = stack.Pop();
								output.Add(new Token(TokenType.Function, func.Value));
							}
							break;

						case ",":
							while (stack.Count > 0 && stack.Peek().Value != "(")
							{
								output.Add(stack.Pop());
							}
							break;

						case "[":
							stack.Push(token);
							break;

						case "]":
							while (stack.Count > 0 && stack.Peek().Value != "[")
							{
								output.Add(stack.Pop());
							}
							stack.Pop(); // 弹出左方括号
							output.Add(new Token(TokenType.Operator, "[]")); // 索引操作符
							break;
					}
					break;

				case TokenType.Operator:
					while (stack.Count > 0 && stack.Peek().Type == TokenType.Operator &&
						(precedence[token.Value].prec < precedence[stack.Peek().Value].prec ||
						(precedence[token.Value].prec == precedence[stack.Peek().Value].prec &&
						!precedence[token.Value].rightAssoc)))
					{
						output.Add(stack.Pop());
					}
					stack.Push(token);
					break;
			}
		}

		while (stack.Count > 0)
		{
			var op = stack.Pop();
			if (op.Value == "(" || op.Value == ")")
				throw new ArgumentException("Mismatched parentheses");
			output.Add(op);
		}

		return output;
	}

	private static object EvaluateRPN(List<Token> rpn)
	{
		// RPN求值实现
		// (处理不同类型操作和函数调用)
		var stack = new Stack<object>();

		foreach (var token in rpn)
		{
			switch (token.Type)
			{
				case TokenType.Number:
					if (double.TryParse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
					{
						stack.Push(num);
					}
					else
					{
						throw new ArgumentException($"Invalid number format: {token.Value}");
					}
					break;

				case TokenType.Boolean:
					stack.Push(token.Value == ".true.");
					break;

				case TokenType.String:
					stack.Push(token.Value.Substring(1, token.Value.Length - 2));
					break;

				case TokenType.Identifier:
					throw new InvalidOperationException($"Unresolved identifier: {token.Value}");

				case TokenType.Operator:
					if (token.Value == "[]") // 索引操作
					{
						if (stack.Count < 2) throw new ArgumentException("Not enough operands for index");
						var index = Convert.ToInt32(stack.Pop());
						var target = stack.Pop();
						if (target is string str)
						{
							if (index < 0 || index >= str.Length)
								throw new IndexOutOfRangeException($"Index {index} out of range");
							stack.Push(str[index].ToString());
						}
						else
						{
							throw new ArgumentException($"Index operator [] cannot be applied to {target.GetType()}");
						}
					}
					else
					{
						if (stack.Count < 2) throw new ArgumentException("Not enough operands");
						var right = stack.Pop();
						var left = stack.Pop();
						stack.Push(EvaluateOperator(token.Value, left, right));
					}
					break;

				case TokenType.Function:
					var funcName = token.Value.ToLower();
					int argCount = funcName switch
					{
						"sin" or "cos" or "tan" or "abs" or "floor" or "ceil" or "round" => 1,
						"max" or "min" or "mod" => 2,
						_ => throw new ArgumentException($"Unknown function: {funcName}")
					};

					var args = new List<object>();
					for (int i = 0; i < argCount; i++)
					{
						if (stack.Count == 0) throw new ArgumentException("Not enough arguments");
						args.Insert(0, stack.Pop());
					}
					stack.Push(ExecuteFunction(funcName, args));
					break;
			}
		}

		return stack.Pop();
	}

	private static object EvaluateOperator(string op, object left, object right)
	{
		try
		{
			switch (op)
			{
				// 算术运算
				case "+":
					if (left is double d1 && right is double d2) return d1 + d2;
					if (left is string s1 && right is string s2) return s1 + s2;
					break;

				case "-": return Convert.ToDouble(left) - Convert.ToDouble(right);
				case "*": return Convert.ToDouble(left) * Convert.ToDouble(right);
				case "/": return Convert.ToDouble(left) / Convert.ToDouble(right);
				case "%": return Convert.ToDouble(left) % Convert.ToDouble(right);
				case "^": return Math.Pow(Convert.ToDouble(left), Convert.ToDouble(right));

				// 比较运算
				case ">": return Compare(left, right) > 0;
				case "<": return Compare(left, right) < 0;
				case ">=": return Compare(left, right) >= 0;
				case "<=": return Compare(left, right) <= 0;
				case "=": return Compare(left, right) == 0;
				case "!=": return Compare(left, right) != 0;

				// 逻辑运算
				case "&": return Convert.ToBoolean(left) && Convert.ToBoolean(right);
				case "|": return Convert.ToBoolean(left) || Convert.ToBoolean(right);
			}
		}
		catch (InvalidCastException)
		{
			throw new ArgumentException(
				$"Operator '{op}' cannot be applied to operands of type {left.GetType()} and {right.GetType()}");
		}

		throw new ArgumentException($"Unsupported operator: {op}");
	}

	private static int Compare(object a, object b)
	{
		if (a is bool ba && b is bool bb) return ba.CompareTo(bb);
		if (a is string sa && b is string sb) return string.Compare(sa, sb, StringComparison.Ordinal);
		return Convert.ToDouble(a).CompareTo(Convert.ToDouble(b));
	}

	private static object ExecuteFunction(string funcName, List<object> args)
	{
		try
		{
			return funcName switch
			{
				"sin" => Math.Sin(Convert.ToDouble(args[0])),
				"cos" => Math.Cos(Convert.ToDouble(args[0])),
				"tan" => Math.Tan(Convert.ToDouble(args[0])),
				"abs" => Math.Abs(Convert.ToDouble(args[0])),
				"floor" => Math.Floor(Convert.ToDouble(args[0])),
				"ceil" => Math.Ceiling(Convert.ToDouble(args[0])),
				"round" => Math.Round(Convert.ToDouble(args[0])),
				"max" => Math.Max(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])),
				"min" => Math.Min(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])),
				"mod" => Convert.ToDouble(args[0]) % Convert.ToDouble(args[1]),
				_ => throw new ArgumentException($"Unknown function: {funcName}")
			};
		}
		catch (InvalidCastException)
		{
			throw new ArgumentException(
				$"Function {funcName} requires numeric arguments");
		}
	}

}