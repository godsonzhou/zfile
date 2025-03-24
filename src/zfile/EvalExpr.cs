using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zfile
{
	public class ExpressionEvaluator // 表达式求值器 claude 3.5 vs
	{
		private Dictionary<string, string> parameters;
		private int position;
		private string expression;

		public object EvalExpr(string expr, Dictionary<string, string> @params)
		{
			parameters = @params;
			expression = expr;
			position = 0;
			return ParseExpression();
		}

		private object ParseExpression()
		{
			var left = ParseLogicalTerm();

			while (position < expression.Length)
			{
				if (Match("|"))
				{
					var right = ParseLogicalTerm();
					left = ConvertToBoolean(left) || ConvertToBoolean(right);
				}
				else break;
			}

			return left;
		}

		private object ParseLogicalTerm()
		{
			var left = ParseComparison();

			while (position < expression.Length)
			{
				if (Match("&"))
				{
					var right = ParseComparison();
					left = ConvertToBoolean(left) && ConvertToBoolean(right);
				}
				else break;
			}

			return left;
		}

		private object ParseComparison()
		{
			var left = ParseAdditive();

			if (position < expression.Length)
			{
				if (Match("="))
				{
					var right = ParseAdditive();
					return AreEqual(left, right);
				}
				else if (Match("!="))
				{
					var right = ParseAdditive();
					return !AreEqual(left, right);
				}
				else if (Match(">="))
				{
					var right = ParseAdditive();
					return Convert.ToDouble(left) >= Convert.ToDouble(right);
				}
				else if (Match(">"))
				{
					var right = ParseAdditive();
					return Convert.ToDouble(left) > Convert.ToDouble(right);
				}
				else if (Match("<="))
				{
					var right = ParseAdditive();
					return Convert.ToDouble(left) <= Convert.ToDouble(right);
				}
				else if (Match("<"))
				{
					var right = ParseAdditive();
					return Convert.ToDouble(left) < Convert.ToDouble(right);
				}
			}

			return left;
		}

		private object ParseAdditive()
		{
			var left = ParseMultiplicative();

			while (position < expression.Length)
			{
				if (Match("+"))
				{
					var right = ParseMultiplicative();
					if (left is string || right is string)
					{
						left = left.ToString() + right.ToString();
					}
					else
					{
						left = Convert.ToDouble(left) + Convert.ToDouble(right);
					}
				}
				else if (Match("-"))
				{
					var right = ParseMultiplicative();
					left = Convert.ToDouble(left) - Convert.ToDouble(right);
				}
				else break;
			}

			return left;
		}

		private object ParseMultiplicative()
		{
			var left = ParsePower();

			while (position < expression.Length)
			{
				if (Match("*"))
				{
					var right = ParsePower();
					left = Convert.ToDouble(left) * Convert.ToDouble(right);
				}
				else if (Match("/"))
				{
					var right = ParsePower();
					left = Convert.ToDouble(left) / Convert.ToDouble(right);
				}
				else if (Match("%"))
				{
					var right = ParsePower();
					left = Convert.ToDouble(left) % Convert.ToDouble(right);
				}
				else break;
			}

			return left;
		}

		private object ParsePower()
		{
			var left = ParseUnary();

			while (position < expression.Length)
			{
				if (Match("^"))
				{
					var right = ParseUnary();
					left = Math.Pow(Convert.ToDouble(left), Convert.ToDouble(right));
				}
				else break;
			}

			return left;
		}

		private object ParseUnary()
		{
			if (Match("!"))
			{
				var value = ParseUnary();
				return !ConvertToBoolean(value);
			}
			else if (Match("-"))
			{
				var value = ParseUnary();
				return -Convert.ToDouble(value);
			}

			return ParseFunctionOrPrimary();
		}

		private object ParseFunctionOrPrimary()
		{
			SkipWhitespace();

			if (char.IsLetter(expression[position]))
			{
				string functionName = ParseIdentifier();

				if (Match("("))
				{
					object result = HandleFunction(functionName);
					Expect(")");
					return result;
				}
				else
				{
					// 变量处理
					if (parameters.TryGetValue(functionName, out string value))
					{
						if (Match("["))
						{
							var index = Convert.ToInt32(ParseExpression());
							Expect("]");
							return value[index].ToString();
						}
						return ProcessValue(value);
					}
					throw new Exception($"Undefined variable: {functionName}");
				}
			}

			return ParsePrimary();
		}

		private object ParsePrimary()
		{
			SkipWhitespace();

			if (Match("("))
			{
				var result = ParseExpression();
				Expect(")");
				return result;
			}
			else if (Match("'"))
			{
				return ParseString();
			}
			else if (Match(".true."))
			{
				return true;
			}
			else if (Match(".false."))
			{
				return false;
			}
			else if (char.IsDigit(expression[position]) || expression[position] == '.')
			{
				return ParseNumber();
			}

			throw new Exception($"Unexpected character at position {position}");
		}

		private object HandleFunction(string functionName)
		{
			switch (functionName.ToLower())
			{
				case "sin":
					return Math.Sin(Convert.ToDouble(ParseExpression()));
				case "cos":
					return Math.Cos(Convert.ToDouble(ParseExpression()));
				case "tan":
					return Math.Tan(Convert.ToDouble(ParseExpression()));
				case "asin":
					return Math.Asin(Convert.ToDouble(ParseExpression()));
				case "acos":
					return Math.Acos(Convert.ToDouble(ParseExpression()));
				case "atan":
					return Math.Atan(Convert.ToDouble(ParseExpression()));
				case "abs":
					return Math.Abs(Convert.ToDouble(ParseExpression()));
				case "sign":
					return Math.Sign(Convert.ToDouble(ParseExpression()));
				case "floor":
					return Math.Floor(Convert.ToDouble(ParseExpression()));
				case "ceil":
					return Math.Ceiling(Convert.ToDouble(ParseExpression()));
				case "round":
					return Math.Round(Convert.ToDouble(ParseExpression()));
				case "max":
					{
						var first = Convert.ToDouble(ParseExpression());
						Expect(",");
						var second = Convert.ToDouble(ParseExpression());
						return Math.Max(first, second);
					}
				case "min":
					{
						var first = Convert.ToDouble(ParseExpression());
						Expect(",");
						var second = Convert.ToDouble(ParseExpression());
						return Math.Min(first, second);
					}
				case "mod":
					{
						var first = Convert.ToDouble(ParseExpression());
						Expect(",");
						var second = Convert.ToDouble(ParseExpression());
						return first % second;
					}
				default:
					throw new Exception($"Unknown function: {functionName}");
			}
		}

		private string ParseIdentifier()
		{
			var start = position;
			while (position < expression.Length && (char.IsLetterOrDigit(expression[position]) || expression[position] == '_'))
			{
				position++;
			}
			return expression[start..position];
		}

		private string ParseString()
		{
			var result = new StringBuilder();
			while (position < expression.Length && expression[position] != '\'')
			{
				result.Append(expression[position++]);
			}
			if (position < expression.Length) position++; // Skip closing quote
			return result.ToString();
		}

		private double ParseNumber()
		{
			var start = position;
			while (position < expression.Length &&
				   (char.IsDigit(expression[position]) || expression[position] == '.'))
			{
				position++;
			}
			return double.Parse(expression[start..position]);
		}

		private void SkipWhitespace()
		{
			while (position < expression.Length && char.IsWhiteSpace(expression[position]))
			{
				position++;
			}
		}

		private bool Match(string pattern)
		{
			SkipWhitespace();
			if (position + pattern.Length > expression.Length) return false;

			if (expression.Substring(position, pattern.Length) == pattern)
			{
				position += pattern.Length;
				return true;
			}
			return false;
		}

		private void Expect(string pattern)
		{
			if (!Match(pattern))
			{
				throw new Exception($"Expected '{pattern}' at position {position}");
			}
		}

		private bool ConvertToBoolean(object value)
		{
			if (value is bool b) return b;
			throw new Exception("Cannot convert value to boolean");
		}

		private bool AreEqual(object left, object right)
		{
			if (left is string || right is string)
			{
				return left.ToString() == right.ToString();
			}
			return Convert.ToDouble(left) == Convert.ToDouble(right);
		}

		private object ProcessValue(string value)
		{
			if (value.StartsWith("'") && value.EndsWith("'"))
			{
				return value.Substring(1, value.Length - 2);
			}
			if (value == ".true.") return true;
			if (value == ".false.") return false;
			if (double.TryParse(value, out double number))
			{
				return number;
			}
			return value;
		}
	}

}
