using MCPSharp.Model;
using OpenQA.Selenium.DevTools.V131.Autofill;
using System;
using System.Collections.Generic;
using System.Linq;

// ================== 基础数据结构 ==================
public enum TokenType { 
	Identifier,   // 标识符
	Number,       // 数字
	String,       // 字符串
	Keyword,      // 关键字
	Operator,     // 运算符
	Punctuator,   // 标点符号
	EOF           // 文件结束
}

public class Token { 
	public TokenType Type { get; set; }
	public string Value { get; set; }
	public int Line { get; set; }
	public int Column { get; set; }

	public Token(TokenType type, string value, int line = 0, int column = 0)
	{
		Type = type;
		Value = value;
		Line = line;
		Column = column;
	}

	public override string ToString() => $"{Type}({Value})"; 
}

public class Lexer
{
	private readonly string source;
	private int position = 0;
	private int line = 1;
	private int column = 1;

	private static readonly HashSet<string> Keywords = new HashSet<string>
	{
		"if", "else", "while", "for", "break", "continue", "return",
		"function", "class", "extends", "new", "this", "super",
		"true", "false", "null", "const", "var", "let", "import"
	};

	public Lexer(string source)
	{
		this.source = source;
	}

	public List<Token> Tokenize()
	{
		var tokens = new List<Token>();
		Token token;
		do
		{
			token = NextToken();
			tokens.Add(token);
		} while (token.Type != TokenType.EOF);

		return tokens;
	}

	private Token NextToken()
	{
		SkipWhitespace();

		if (position >= source.Length)
			return new Token(TokenType.EOF, "", line, column);

		char c = source[position];

		// 标识符或关键字
		if (IsAlpha(c))
			return IdentifierOrKeyword();

		// 数字
		if (IsDigit(c))
			return Number();

		// 字符串
		if (c == '"' || c == '\'') 
			return String();

		// 注释
		if (c == '/' && position + 1 < source.Length)
		{
			if (source[position + 1] == '/') // 单行注释
			{
				SkipLineComment();
				return NextToken();
			}
			else if (source[position + 1] == '*') // 多行注释
			{
				SkipBlockComment();
				return NextToken();
			}
		}

		// 运算符和标点符号
		return OperatorOrPunctuator();
	}

	private Token IdentifierOrKeyword()
	{
		int startPos = position;
		int startLine = line;
		int startColumn = column;

		while (position < source.Length && (IsAlphaNumeric(source[position]) || source[position] == '_'))
		{
			position++;
			column++;
		}

		string value = source.Substring(startPos, position - startPos);

		if (Keywords.Contains(value))
			return new Token(TokenType.Keyword, value, startLine, startColumn);
		else
			return new Token(TokenType.Identifier, value, startLine, startColumn);
	}

	private Token Number()
	{
		int startPos = position;
		int startLine = line;
		int startColumn = column;
		bool hasDecimal = false;

		while (position < source.Length && 
			(IsDigit(source[position]) || 
			(source[position] == '.' && !hasDecimal && position + 1 < source.Length && IsDigit(source[position + 1]))))
		{
			if (source[position] == '.')
				hasDecimal = true;
			position++;
			column++;
		}

		string value = source.Substring(startPos, position - startPos);
		return new Token(TokenType.Number, value, startLine, startColumn);
	}

	private Token String()
	{
		int startLine = line;
		int startColumn = column;
		char quote = source[position];
		position++; // 跳过引号
		column++;

		int startPos = position;
		while (position < source.Length && source[position] != quote)
		{
			if (source[position] == '\\' && position + 1 < source.Length)
			{
				position += 2; // 跳过转义字符
				column += 2;
			}
			else if (source[position] == '\n')
			{
				line++;
				column = 1;
				position++;
			}
			else
			{
				position++;
				column++;
			}
		}

		if (position >= source.Length)
			throw new Exception("Unterminated string literal");

		string value = source.Substring(startPos, position - startPos);
		position++; // 跳过结束引号
		column++;

		return new Token(TokenType.String, value, startLine, startColumn);
	}

	private Token OperatorOrPunctuator()
	{
		int startLine = line;
		int startColumn = column;
		char c = source[position];
		position++;
		column++;

		// 检查双字符运算符
		if (position < source.Length)
		{
			string op = c + source[position].ToString();
			if (op == "==" || op == "!=" || op == "<=" || op == ">=" || 
				op == "&&" || op == "||" || op == "+=" || op == "-=" || 
				op == "*=" || op == "/=" || op == "++" || op == "--")
			{
				position++;
				column++;
				return new Token(TokenType.Operator, op, startLine, startColumn);
			}
		}

		// 单字符运算符和标点
		if ("+-*/%=<>!&|^~".Contains(c))
			return new Token(TokenType.Operator, c.ToString(), startLine, startColumn);
		else
			return new Token(TokenType.Punctuator, c.ToString(), startLine, startColumn);
	}

	private void SkipWhitespace()
	{
		while (position < source.Length && char.IsWhiteSpace(source[position]))
		{
			if (source[position] == '\n')
			{
				line++;
				column = 1;
			}
			else
			{
				column++;
			}
			position++;
		}
	}

	private void SkipLineComment()
	{
		position += 2; // 跳过 //
		column += 2;

		while (position < source.Length && source[position] != '\n')
		{
			position++;
			column++;
		}
	}

	private void SkipBlockComment()
	{
		position += 2; // 跳过 /*
		column += 2;

		while (position < source.Length && !(source[position] == '*' && position + 1 < source.Length && source[position + 1] == '/'))
		{
			if (source[position] == '\n')
			{
				line++;
				column = 1;
			}
			else
			{
				column++;
			}
			position++;
		}

		if (position >= source.Length)
			throw new Exception("Unterminated block comment");

		position += 2; // 跳过 */
		column += 2;
	}

	private bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
	private bool IsDigit(char c) => c >= '0' && c <= '9';
	private bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
}

public enum ValueType
{
	Number,
	String,
	Boolean,
	Object,
	Function,
	Class,
	List,
	Dict,
	Null
}

public class RuntimeValue
{
	public ValueType Type { get; protected set; }
	public object Value { get; protected set; }

	public RuntimeValue(ValueType type, object value)
	{
		Type = type;
		Value = value;
	}
}

public class Variable
{
	public bool IsConstant { get; }
	public RuntimeValue Value { get; set; }

	public Variable(bool isConstant, RuntimeValue value)
	{
		IsConstant = isConstant;
		Value = value;
	}
}

// ================== 作用域管理 ==================
public class Scope
{
	private Dictionary<string, Variable> variables = new();
	private Dictionary<string, FunctionDef> functions = new();
	private Dictionary<string, ClassDef> classes = new();
	public Scope Parent { get; }

	public Scope(Scope parent = null) => Parent = parent;

	public Variable GetVariable(string name)
	{
		if (variables.ContainsKey(name)) return variables[name];
		return Parent?.GetVariable(name) ?? throw new Exception($"Undefined variable: {name}");
	}

	public void DeclareVariable(string name, bool isConstant, RuntimeValue value)
	{
		if (variables.ContainsKey(name))
			throw new Exception($"Variable {name} already declared");
		variables[name] = new Variable(isConstant, value);
	}

	public void AssignVariable(string name, RuntimeValue value)
	{
		var variable = GetVariable(name);
		if (variable.IsConstant)
			throw new Exception($"Cannot assign to constant variable: {name}");
		variable.Value = value;
	}

	public FunctionDef GetFunction(string name)
	{
		if (functions.ContainsKey(name)) return functions[name];
		return Parent?.GetFunction(name) ?? throw new Exception($"Undefined function: {name}");
	}

	public void DeclareFunction(string name, FunctionDef function)
	{
		if (functions.ContainsKey(name))
			throw new Exception($"Function {name} already declared");
		functions[name] = function;
	}

	public ClassDef GetClass(string name)
	{
		if (classes.ContainsKey(name)) return classes[name];
		return Parent?.GetClass(name) ?? throw new Exception($"Undefined class: {name}");
	}

	public void DeclareClass(string name, ClassDef classDef)
	{
		if (classes.ContainsKey(name))
			throw new Exception($"Class {name} already declared");
		classes[name] = classDef;
	}

	public Dictionary<string, FunctionDef> GetAllFunctions() => functions;
	public Dictionary<string, Variable> GetAllVariables() => variables;
}

// ================== 语法树节点 ==================
public interface INode
{
	RuntimeValue Evaluate(Scope scope);
}

public class BlockStatement : INode
{
	public List<INode> Statements { get; } = new();

	public RuntimeValue Evaluate(Scope scope)
	{
		foreach (var stmt in Statements)
		{
			var result = stmt.Evaluate(scope);
			if (result is ReturnValue) return result;
			if (result is BreakSignal || result is ContinueSignal) return result;
		}
		return new RuntimeValue(ValueType.Null, null);
	}
}

// ================== 控制结构实现 ==================
public class IfStatement : INode
{
	public INode Condition { get; set; }
	public BlockStatement ThenBranch { get; set; }
	public BlockStatement ElseBranch { get; set; }

	public RuntimeValue Evaluate(Scope scope)
	{
		var cond = Condition.Evaluate(scope);
		if (cond.Type != ValueType.Boolean)
			throw new Exception("Condition must evaluate to boolean");

		return (bool)cond.Value ?
			ThenBranch.Evaluate(new Scope(scope)) :
			ElseBranch?.Evaluate(new Scope(scope)) ?? new RuntimeValue(ValueType.Null, null);
	}
}

public class WhileLoop : INode
{
	public INode Condition { get; set; }
	public BlockStatement Body { get; set; }

	public RuntimeValue Evaluate(Scope scope)
	{
		while (true)
		{
			var cond = Condition.Evaluate(scope);
			if (cond.Type != ValueType.Boolean)
				throw new Exception("Condition must be boolean");

			if (!(bool)cond.Value) break;

			var result = Body.Evaluate(new Scope(scope));
			if (result is ReturnValue) return result;
			if (result is BreakSignal) break;
		}
		return new RuntimeValue(ValueType.Null, null);
	}
}

// ================== 函数系统实现 ==================
public class FunctionDef
{
	public List<string> Parameters { get; }
	public BlockStatement Body { get; }
	public bool IsMethod { get; }

	public FunctionDef(List<string> parameters, BlockStatement body, bool isMethod = false)
	{
		Parameters = parameters;
		Body = body;
		IsMethod = isMethod;
	}
}

public class FunctionCall : INode
{
	public string Name { get; set; }
	public List<INode> Args { get; set; }

	public RuntimeValue Evaluate(Scope scope)
	{
		var func = ResolveFunction(scope, Name);
		var funcScope = new Scope(scope.Parent); // 函数作用域继承定义时的父作用域

		// 处理this绑定
		if (func.IsMethod)
		{
			if (scope.GetVariable("this") is { } thisVar)
				funcScope.DeclareVariable("this", true, thisVar.Value);
		}

		// 参数绑定
		for (int i = 0; i < func.Parameters.Count; i++)
		{
			funcScope.DeclareVariable(func.Parameters[i], false, Args[i].Evaluate(scope));
		}

		var result = func.Body.Evaluate(funcScope);
		return result is ReturnValue rv ? (RuntimeValue)rv.Value : new RuntimeValue(ValueType.Null, null);
	}

	private FunctionDef ResolveFunction(Scope scope, string name)
	{
		try
		{
			return scope.GetFunction(name);
		}
		catch (Exception)
		{
			throw new Exception($"Function {name} is not defined");
		}
	}
}

// ================== 面向对象系统 ==================
public class ClassDef
{
	public string Name { get; }
	public ClassDef Parent { get; }
	public Dictionary<string, FunctionDef> Methods { get; } = new();
	public List<string> Fields { get; } = new();

	public ClassDef(string name, ClassDef parent = null)
	{
		Name = name;
		Parent = parent;
	}

	public bool HasField(string name)
	{
		return Fields.Contains(name) || (Parent?.HasField(name) ?? false);
	}

	public FunctionDef GetMethod(string name)
	{
		if (Methods.ContainsKey(name)) return Methods[name];
		return Parent?.GetMethod(name);
	}
}

public class ClassInstance
{
	public ClassDef Class { get; }
	private Dictionary<string, RuntimeValue> fields = new();
	public Dictionary<string, RuntimeValue> Fields => fields;

	public ClassInstance(ClassDef classDef)
	{
		Class = classDef;
		InitializeFields(classDef);
	}

	private void InitializeFields(ClassDef classDef)
	{
		if (classDef.Parent != null) InitializeFields(classDef.Parent);

		foreach (var field in classDef.Fields)
		{
			fields[field] = new RuntimeValue(ValueType.Null, null);
		}
	}

	public RuntimeValue GetField(string name)
	{
		if (!Class.HasField(name))
			throw new Exception($"Undefined field: {name}");
		return fields[name];
	}

	public void SetField(string name, RuntimeValue value)
	{
		if (!Class.HasField(name))
			throw new Exception($"Undefined field: {name}");
		fields[name] = value;
	}
}

// ================== 继承实现示例 ==================
public class ClassInheritance : INode
{
	public string ClassName { get; set; }
	public string ParentName { get; set; }
	public BlockStatement Body { get; set; }

	public RuntimeValue Evaluate(Scope scope)
	{
		var parentClass = scope.GetClass(ParentName) ?? throw new Exception($"Parent class {ParentName} not found");
		var classDef = new ClassDef(ClassName, parentClass);

		// 处理字段继承
		classDef.Fields.AddRange(parentClass.Fields);

		// 处理成员方法
		var classScope = new Scope(scope);
		Body.Evaluate(classScope);

		foreach (var method in classScope.GetAllFunctions())
		{
			classDef.Methods[method.Key] = new FunctionDef(
				method.Value.Parameters,
				method.Value.Body,
				isMethod: true
			);
		}

		scope.DeclareClass(ClassName, classDef);
		return new RuntimeValue(ValueType.Class, classDef);
	}
}

// ================== 语法分析器实现 ==================


// ================== 使用示例 ==================
public class Interpreter
{
	private Scope globalScope;

	public Interpreter()
	{
		globalScope = new Scope();
		InitializeStandardLibrary();
	}

	private void InitializeStandardLibrary()
	{
		// 添加内置函数
		globalScope.DeclareFunction("print", new FunctionDef(
			new List<string> { "message" },
			new BlockStatement
			{
				Statements = { new PrintNode(new IdentifierNode("message")) }
			}
		));

		// 添加内置类型
		var listClass = new ClassDef("List");
		listClass.Methods["add"] = new FunctionDef(
			new List<string> { "item" },
			new BlockStatement(),
			isMethod: true
		);
		globalScope.DeclareClass("List", listClass);

		var dictClass = new ClassDef("Dict");
		dictClass.Methods["set"] = new FunctionDef(
			new List<string> { "key", "value" },
			new BlockStatement(),
			isMethod: true
		);
		globalScope.DeclareClass("Dict", dictClass);
	}

	public RuntimeValue Execute(string source)
	{
		// 词法分析
		var lexer = new Lexer(source);
		var tokens = lexer.Tokenize();

		// 语法分析
		var parser = new Parser(tokens);
		var program = parser.ParseProgram();

		// 执行程序
		return program.Evaluate(globalScope);
	}

	public void ExecuteFile(string filePath)
	{
		string source = System.IO.File.ReadAllText(filePath);
		Execute(source);
	}
}

// ================== 辅助节点定义 ==================
// ================== 表达式节点 ==================
public class ExpressionNode : INode
{
	private string expression;

	public ExpressionNode(string expr) => expression = expr;

	public RuntimeValue Evaluate(Scope scope)
	{
		// 使用evalexprDS计算表达式
		var parameters = new Dictionary<string, string>();
		
		// 将作用域中的变量转换为evalexprDS可用的参数
		foreach (var variable in scope.GetAllVariables())
		{
			var value = variable.Value.Value;
			if (value.Type == ValueType.Number)
				parameters[variable.Key] = value.Value.ToString();
			else if (value.Type == ValueType.String)
				parameters[variable.Key] = $"'{value.Value}'";
			else if (value.Type == ValueType.Boolean)
				parameters[variable.Key] = (bool)value.Value ? ".true." : ".false.";
		}

		var result = ExpressionEvaluatorDS.EvalExpr(expression, parameters);
		
		// 将结果转换为RuntimeValue
		if (result is double || result is int)
			return new RuntimeValue(ValueType.Number, result);
		else if (result is string)
			return new RuntimeValue(ValueType.String, result);
		else if (result is bool)
			return new RuntimeValue(ValueType.Boolean, result);
		else
			return new RuntimeValue(ValueType.Null, null);
	}
}

public class VariableDeclarationNode : INode
{
	public string Name { get; }
	public INode Initializer { get; }
	public bool IsConstant { get; }

	public VariableDeclarationNode(string name, INode initializer, bool isConstant = false)
	{
		Name = name;
		Initializer = initializer;
		IsConstant = isConstant;
	}

	public RuntimeValue Evaluate(Scope scope)
	{
		var value = Initializer?.Evaluate(scope) ?? new RuntimeValue(ValueType.Null, null);
		scope.DeclareVariable(Name, IsConstant, value);
		return value;
	}
}

public class VariableAssignmentNode : INode
{
	public string Name { get; }
	public INode Value { get; }

	public VariableAssignmentNode(string name, INode value)
	{
		Name = name;
		Value = value;
	}

	public RuntimeValue Evaluate(Scope scope)
	{
		var value = Value.Evaluate(scope);
		scope.AssignVariable(Name, value);
		return value;
	}
}

public class IdentifierNode : INode
{
	public string Name { get; }

	public IdentifierNode(string name) => Name = name;

	public RuntimeValue Evaluate(Scope scope)
	{
		return scope.GetVariable(Name).Value;
	}
}

public class LiteralNode : INode
{
	private RuntimeValue value;

	public LiteralNode(object value)
	{
		if (value is int || value is double)
			this.value = new RuntimeValue(ValueType.Number, value);
		else if (value is string)
			this.value = new RuntimeValue(ValueType.String, value);
		else if (value is bool)
			this.value = new RuntimeValue(ValueType.Boolean, value);
		else
			this.value = new RuntimeValue(ValueType.Null, null);
	}

	public RuntimeValue Evaluate(Scope scope) => value;
}

public class ListLiteralNode : INode
{
	public List<INode> Items { get; }

	public ListLiteralNode(List<INode> items) => Items = items;

	public RuntimeValue Evaluate(Scope scope)
	{
		var items = new List<RuntimeValue>();
		foreach (var item in Items)
		{
			items.Add(item.Evaluate(scope));
		}
		return new ListValue(items);
	}
}

public class ReturnValue : RuntimeValue
{
	public ReturnValue(RuntimeValue value) : base(value.Type, value.Value) { }
}

public class BreakSignal : RuntimeValue
{
	public BreakSignal() : base(ValueType.Null, null) { }
}

public class ContinueSignal : RuntimeValue
{
	public ContinueSignal() : base(ValueType.Null, null) { }
}
public class MemoryManager
{
	private List<ClassInstance> instances = new();

	public void TrackInstance(ClassInstance instance) => instances.Add(instance);

	public void CollectGarbage(Scope root)
	{
		var referenced = new HashSet<object>();
		ScanScope(root);

		foreach (var inst in instances.Where(i => !referenced.Contains(i)).ToList())
		{
			instances.Remove(inst);
		}

		void ScanScope(Scope scope)
		{
			foreach (var variable in scope.GetAllVariables())
			{
				// 检查变量值是否为ClassInstance类型
				var value = variable.Value.Value;
				
				//if (value is ClassInstance ci)
				//{
				//	if (!referenced.Add(ci)) continue;
				//	ScanClassInstance(ci);
				//}
			}
			
			// 递归扫描子作用域
			if (scope.Parent != null)
			{
				ScanScope(scope.Parent);
			}
			
			// 扫描所有使用当前作用域的子作用域
			// 这里需要遍历所有可能创建新作用域的地方，如函数调用、条件语句等
			// 由于我们没有直接的方法获取所有子作用域，我们可以通过扫描函数来间接处理
			foreach (var function in scope.GetAllFunctions().Values)
			{
				// 函数体可能包含对ClassInstance的引用
				if (function.Body != null)
				{
					// 为函数创建一个新的作用域并扫描它
					var functionScope = new Scope(scope);
					// 这里我们不实际执行函数，只是扫描其可能引用的对象
					ScanBlockStatement(function.Body, functionScope);
				}
			}
		}

		// 扫描语句块中可能包含的ClassInstance引用
		void ScanBlockStatement(BlockStatement block, Scope blockScope)
		{
			foreach (var statement in block.Statements)
			{
				// 处理不同类型的语句
				if (statement is IfStatement ifStmt)
				{
					// 扫描条件表达式
					ScanExpression(ifStmt.Condition, blockScope);
					
					// 扫描then分支
					if (ifStmt.ThenBranch != null)
					{
						ScanBlockStatement(ifStmt.ThenBranch, new Scope(blockScope));
					}
					
					// 扫描else分支
					if (ifStmt.ElseBranch != null)
					{
						ScanBlockStatement(ifStmt.ElseBranch, new Scope(blockScope));
					}
				}
				else if (statement is WhileLoop whileLoop)
				{
					// 扫描循环条件
					ScanExpression(whileLoop.Condition, blockScope);
					
					// 扫描循环体
					if (whileLoop.Body != null)
					{
						ScanBlockStatement(whileLoop.Body, new Scope(blockScope));
					}
				}
				else if (statement is FunctionCall funcCall)
				{
					// 扫描函数参数
					foreach (var arg in funcCall.Args)
					{
						ScanExpression(arg, blockScope);
					}
				}
				else
				{
					// 对于其他类型的语句，尝试评估它们以查找ClassInstance引用
					ScanExpression(statement, blockScope);
				}
			}
		}

		// 扫描表达式中可能包含的ClassInstance引用
		void ScanExpression(INode node, Scope exprScope)
		{
			// 这里我们不实际执行表达式，只是检查它是否直接引用了ClassInstance
			// 对于复杂表达式，可能需要更详细的处理
			if (node is IdentifierNode idNode)
			{
				try
				{
					var variable = exprScope.GetVariable(idNode.Name);
					if (variable.Value.Value is ClassInstance ci)
					{
						if (!referenced.Add(ci)) return;
						ScanClassInstance(ci);
					}
				}
				catch (Exception)
				{
					// 忽略未定义的变量
				}
			}
		}

		void ScanClassInstance(ClassInstance instance)
		{
			foreach (var field in instance.Fields.Values)
			{
				// 检查字段值是否为ClassInstance类型
				var fieldValue = field.Value;
				if (fieldValue is ClassInstance ci)
				{
					if (!referenced.Add(ci)) continue;
					ScanClassInstance(ci);
				}
			}
		}
	}
}
public interface IMethodResolver
{
	FunctionDef ResolveMethod(string name, List<RuntimeValue> args);
}

public class InterfaceDef : IMethodResolver
{
	public Dictionary<string, FunctionSignature> RequiredMethods { get; } = new();
	public ClassDef Implementation { get; set; }

	public FunctionDef ResolveMethod(string name, List<RuntimeValue> args)
	{
		if (!RequiredMethods.ContainsKey(name))
			throw new Exception($"Interface does not require method {name}");

		// 验证参数类型匹配...
		return Implementation?.GetMethod(name);
	}
}

public class FunctionSignature
{
	public List<ValueType> ParameterTypes { get; }
	public ValueType ReturnType { get; }

	public FunctionSignature(List<ValueType> paramTypes, ValueType returnType)
	{
		ParameterTypes = paramTypes;
		ReturnType = returnType;
	}
}

// ================== 复杂数据结构实现 ==================
public class ListValue : RuntimeValue
{
	public List<RuntimeValue> Items { get; }

	public ListValue() : base(ValueType.List, null)
	{
		Items = new List<RuntimeValue>();
		Value = Items;
	}

	public ListValue(List<RuntimeValue> items) : base(ValueType.List, null)
	{
		Items = items;
		Value = Items;
	}

	public RuntimeValue GetItem(int index)
	{
		if (index < 0 || index >= Items.Count)
			throw new Exception($"Index {index} out of range");
		return Items[index];
	}

	public void SetItem(int index, RuntimeValue value)
	{
		if (index < 0 || index >= Items.Count)
			throw new Exception($"Index {index} out of range");
		Items[index] = value;
	}

	public void Append(RuntimeValue value)
	{
		Items.Add(value);
	}

	public void Remove(int index)
	{
		if (index < 0 || index >= Items.Count)
			throw new Exception($"Index {index} out of range");
		Items.RemoveAt(index);
	}

	public int Length() => Items.Count;
}

public class DictValue : RuntimeValue
{
	public Dictionary<string, RuntimeValue> Items { get; }

	public DictValue() : base(ValueType.Dict, null)
	{
		Items = new Dictionary<string, RuntimeValue>();
		Value = Items;
	}

	public DictValue(Dictionary<string, RuntimeValue> items) : base(ValueType.Dict, null)
	{
		Items = items;
		Value = Items;
	}

	public RuntimeValue GetItem(string key)
	{
		if (!Items.ContainsKey(key))
			throw new Exception($"Key '{key}' not found in dictionary");
		return Items[key];
	}

	public void SetItem(string key, RuntimeValue value)
	{
		Items[key] = value;
	}

	public void Remove(string key)
	{
		if (!Items.ContainsKey(key))
			throw new Exception($"Key '{key}' not found in dictionary");
		Items.Remove(key);
	}

	public bool ContainsKey(string key) => Items.ContainsKey(key);

	public List<string> Keys() => Items.Keys.ToList();

	public int Length() => Items.Count;
}