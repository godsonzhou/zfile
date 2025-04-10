using System;
using System.Collections.Generic;
using System.Linq;

// 补全DictLiteralNode类的实现
public class DictLiteralNode : INode
{
    public Dictionary<string, INode> Properties { get; }

    public DictLiteralNode(Dictionary<string, INode> properties)
    {
        Properties = properties;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var items = new Dictionary<string, RuntimeValue>();
        foreach (var prop in Properties)
        {
            items[prop.Key] = prop.Value.Evaluate(scope);
        }
        return new DictValue(items);
    }
}

// 实现PrintNode类
public class PrintNode : INode
{
    private readonly string message;
    private readonly INode expression;

    public PrintNode(string message)
    {
        this.message = message;
        this.expression = null;
    }

    public PrintNode(INode expression)
    {
        this.message = null;
        this.expression = expression;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        if (expression != null)
        {
            var value = expression.Evaluate(scope);
            Console.WriteLine(value.Value);
        }
        else
        {
            Console.WriteLine(message);
        }
        return new RuntimeValue(ValueType.Null, null);
    }
}

// 实现MethodDefNode类
public class MethodDefNode : INode
{
    public string Name { get; }
    public List<string> Parameters { get; }
    public BlockStatement Body { get; }

    public MethodDefNode(string name, List<string> parameters, BlockStatement body)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var functionDef = new FunctionDef(Parameters, Body, isMethod: true);
        scope.DeclareFunction(Name, functionDef);
        return new RuntimeValue(ValueType.Function, functionDef);
    }
}

// 实现MethodCallNode类
public class MethodCallNode : INode
{
    public INode Object { get; }
    public string MethodName { get; }
    public List<INode> Args { get; }

    public MethodCallNode(INode obj, string methodName, List<INode> args)
    {
        Object = obj;
        MethodName = methodName;
        Args = args;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var objValue = Object.Evaluate(scope);
        
        if (objValue.Type != ValueType.Object && objValue.Type != ValueType.Class)
            throw new Exception($"Cannot call method on {objValue.Type}");
        
        var instance = objValue.Value as ClassInstance;
        if (instance == null)
            throw new Exception("Invalid object instance");
        
        var method = instance.Class.GetMethod(MethodName);
        if (method == null)
            throw new Exception($"Method {MethodName} not found");
        
        // 创建方法调用的作用域
        var methodScope = new Scope(scope);
        methodScope.DeclareVariable("this", true, objValue);
        
        // 绑定参数
        for (int i = 0; i < method.Parameters.Count; i++)
        {
            if (i < Args.Count)
                methodScope.DeclareVariable(method.Parameters[i], false, Args[i].Evaluate(scope));
            else
                methodScope.DeclareVariable(method.Parameters[i], false, new RuntimeValue(ValueType.Null, null));
        }
        
        var result = method.Body.Evaluate(methodScope);
        return result is ReturnValue rv ? (RuntimeValue)rv.Value : new RuntimeValue(ValueType.Null, null);
    }
}

// 实现FieldDefNode类
public class FieldDefNode : INode
{
    public string Name { get; }

    public FieldDefNode(string name)
    {
        Name = name;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        // 在类定义上下文中，字段名称会被添加到类的字段列表中
        return new RuntimeValue(ValueType.Null, null);
    }
}

// 实现PropertyAccessNode类
public class PropertyAccessNode : INode
{
    public INode Object { get; }
    public string Property { get; }

    public PropertyAccessNode(INode obj, string property)
    {
        Object = obj;
        Property = property;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var objValue = Object.Evaluate(scope);
        
        if (objValue.Type == ValueType.Object)
        {
            var instance = objValue.Value as ClassInstance;
            return instance.GetField(Property);
        }
        else if (objValue.Type == ValueType.Dict)
        {
            var dict = objValue as DictValue;
            return dict.GetItem(Property);
        }
        
        throw new Exception($"Cannot access property {Property} on {objValue.Type}");
    }
}

// 实现PropertyAssignmentNode类
public class PropertyAssignmentNode : INode
{
    public INode Object { get; }
    public string Property { get; }
    public INode Value { get; }

    public PropertyAssignmentNode(INode obj, string property, INode value)
    {
        Object = obj;
        Property = property;
        Value = value;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var objValue = Object.Evaluate(scope);
        var value = Value.Evaluate(scope);
        
        if (objValue.Type == ValueType.Object)
        {
            var instance = objValue.Value as ClassInstance;
            instance.SetField(Property, value);
        }
        else if (objValue.Type == ValueType.Dict)
        {
            var dict = objValue as DictValue;
            dict.SetItem(Property, value);
        }
        else
        {
            throw new Exception($"Cannot set property {Property} on {objValue.Type}");
        }
        
        return value;
    }
}

// 实现IndexAccessNode类
public class IndexAccessNode : INode
{
    public INode Object { get; }
    public INode Index { get; }

    public IndexAccessNode(INode obj, INode index)
    {
        Object = obj;
        Index = index;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var objValue = Object.Evaluate(scope);
        var indexValue = Index.Evaluate(scope);
        
        if (objValue.Type == ValueType.List)
        {
            if (indexValue.Type != ValueType.Number)
                throw new Exception("List index must be a number");
                
            var list = objValue as ListValue;
            int index = Convert.ToInt32(indexValue.Value);
            return list.GetItem(index);
        }
        else if (objValue.Type == ValueType.Dict)
        {
            if (indexValue.Type != ValueType.String)
                throw new Exception("Dictionary key must be a string");
                
            var dict = objValue as DictValue;
            string key = indexValue.Value.ToString();
            return dict.GetItem(key);
        }
        else if (objValue.Type == ValueType.String)
        {
            if (indexValue.Type != ValueType.Number)
                throw new Exception("String index must be a number");
                
            string str = objValue.Value.ToString();
            int index = Convert.ToInt32(indexValue.Value);
            
            if (index < 0 || index >= str.Length)
                throw new Exception("String index out of range");
                
            return new RuntimeValue(ValueType.String, str[index].ToString());
        }
        
        throw new Exception($"Cannot index into {objValue.Type}");
    }
}

// 实现IndexAssignmentNode类
public class IndexAssignmentNode : INode
{
    public INode Object { get; }
    public INode Index { get; }
    public INode Value { get; }

    public IndexAssignmentNode(INode obj, INode index, INode value)
    {
        Object = obj;
        Index = index;
        Value = value;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var objValue = Object.Evaluate(scope);
        var indexValue = Index.Evaluate(scope);
        var value = Value.Evaluate(scope);
        
        if (objValue.Type == ValueType.List)
        {
            if (indexValue.Type != ValueType.Number)
                throw new Exception("List index must be a number");
                
            var list = objValue as ListValue;
            int index = Convert.ToInt32(indexValue.Value);
            list.SetItem(index, value);
        }
        else if (objValue.Type == ValueType.Dict)
        {
            if (indexValue.Type != ValueType.String)
                throw new Exception("Dictionary key must be a string");
                
            var dict = objValue as DictValue;
            string key = indexValue.Value.ToString();
            dict.SetItem(key, value);
        }
        else
        {
            throw new Exception($"Cannot assign to index of {objValue.Type}");
        }
        
        return value;
    }
}

// 实现BinaryOpNode类
public class BinaryOpNode : INode
{
    public string Operator { get; }
    public INode Left { get; }
    public INode Right { get; }

    public BinaryOpNode(string op, INode left, INode right)
    {
        Operator = op;
        Left = left;
        Right = right;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        // 使用ExpressionNode来计算二元运算表达式
        var left = Left.Evaluate(scope);
        var right = Right.Evaluate(scope);
        
        // 构建表达式字符串
        string leftStr = ConvertToExprString(left);
        string rightStr = ConvertToExprString(right);
        string expr = $"{leftStr} {Operator} {rightStr}";
        
        // 使用ExpressionNode计算表达式
        return new ExpressionNode(expr).Evaluate(scope);
    }
    
    private string ConvertToExprString(RuntimeValue value)
    {
        switch (value.Type)
        {
            case ValueType.Number:
                return value.Value.ToString();
            case ValueType.String:
                return $"'{value.Value}'";
            case ValueType.Boolean:
                return (bool)value.Value ? ".true." : ".false.";
            default:
                return "null";
        }
    }
}

// 实现UnaryOpNode类
public class UnaryOpNode : INode
{
    public string Operator { get; }
    public INode Operand { get; }

    public UnaryOpNode(string op, INode operand)
    {
        Operator = op;
        Operand = operand;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var operand = Operand.Evaluate(scope);
        
        // 构建表达式字符串
        string operandStr = ConvertToExprString(operand);
        string expr = $"{Operator}{operandStr}";
        
        // 使用ExpressionNode计算表达式
        return new ExpressionNode(expr).Evaluate(scope);
    }
    
    private string ConvertToExprString(RuntimeValue value)
    {
        switch (value.Type)
        {
            case ValueType.Number:
                return value.Value.ToString();
            case ValueType.String:
                return $"'{value.Value}'";
            case ValueType.Boolean:
                return (bool)value.Value ? ".true." : ".false.";
            default:
                return "null";
        }
    }
}

// 实现PostfixOpNode类
public class PostfixOpNode : INode
{
    public string Operator { get; }
    public INode Operand { get; }

    public PostfixOpNode(string op, INode operand)
    {
        Operator = op;
        Operand = operand;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        if (Operand is IdentifierNode identNode)
        {
            var variable = scope.GetVariable(identNode.Name);
            if (variable.IsConstant)
                throw new Exception($"Cannot modify constant variable: {identNode.Name}");
                
            var oldValue = variable.Value;
            if (oldValue.Type != ValueType.Number)
                throw new Exception($"Cannot apply {Operator} to non-number");
                
            double value = Convert.ToDouble(oldValue.Value);
            double newValue = Operator == "++" ? value + 1 : value - 1;
            
            scope.AssignVariable(identNode.Name, new RuntimeValue(ValueType.Number, newValue));
            return oldValue; // 返回操作前的值
        }
        
        throw new Exception("Invalid postfix operation target");
    }
}

// 实现ThisNode类
public class ThisNode : INode
{
    public RuntimeValue Evaluate(Scope scope)
    {
        try
        {
            return scope.GetVariable("this").Value;
        }
        catch
        {
            throw new Exception("'this' is not defined in current context");
        }
    }
}

// 实现SuperNode类
public class SuperNode : INode
{
    public RuntimeValue Evaluate(Scope scope)
    {
        try
        {
            var thisValue = scope.GetVariable("this").Value;
            if (thisValue.Type != ValueType.Object)
                throw new Exception("'super' can only be used in a method");
                
            var instance = thisValue.Value as ClassInstance;
            if (instance.Class.Parent == null)
                throw new Exception("Class has no parent");
                
            // 创建一个表示父类的实例
            var superInstance = new ClassInstance(instance.Class.Parent);
            return new RuntimeValue(ValueType.Object, superInstance);
        }
        catch
        {
            throw new Exception("'super' is not available in current context");
        }
    }
}

// 实现NewExpressionNode类
public class NewExpressionNode : INode
{
    public string ClassName { get; }
    public List<INode> Args { get; }

    public NewExpressionNode(string className, List<INode> args)
    {
        ClassName = className;
        Args = args;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var classDef = scope.GetClass(ClassName);
        var instance = new ClassInstance(classDef);
        
        // 调用构造函数（如果存在）
        var constructor = classDef.GetMethod("constructor");
        if (constructor != null)
        {
            var constructorScope = new Scope(scope);
            constructorScope.DeclareVariable("this", true, new RuntimeValue(ValueType.Object, instance));
            
            // 绑定参数
            for (int i = 0; i < constructor.Parameters.Count; i++)
            {
                if (i < Args.Count)
                    constructorScope.DeclareVariable(constructor.Parameters[i], false, Args[i].Evaluate(scope));
                else
                    constructorScope.DeclareVariable(constructor.Parameters[i], false, new RuntimeValue(ValueType.Null, null));
            }
            
            constructor.Body.Evaluate(constructorScope);
        }
        
        return new RuntimeValue(ValueType.Object, instance);
    }
}

// 实现FunctionDeclarationNode类
public class FunctionDeclarationNode : INode
{
    public string Name { get; set; }
    public List<string> Parameters { get; set; }
    public BlockStatement Body { get; set; }

    public RuntimeValue Evaluate(Scope scope)
    {
        var functionDef = new FunctionDef(Parameters, Body);
        scope.DeclareFunction(Name, functionDef);
        return new RuntimeValue(ValueType.Function, functionDef);
    }
}

// 实现ReturnNode类
public class ReturnNode : INode
{
    public INode Value { get; }

    public ReturnNode(INode value)
    {
        Value = value;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        var value = Value?.Evaluate(scope) ?? new RuntimeValue(ValueType.Null, null);
        return new ReturnValue(value);
    }
}

// 实现BreakNode类
public class BreakNode : INode
{
    public RuntimeValue Evaluate(Scope scope)
    {
        return new BreakSignal();
    }
}

// 实现ContinueNode类
public class ContinueNode : INode
{
    public RuntimeValue Evaluate(Scope scope)
    {
        return new ContinueSignal();
    }
}

// 实现ImportNode类
public class ImportNode : INode
{
    public string ModuleName { get; }

    public ImportNode(string moduleName)
    {
        ModuleName = moduleName;
    }

    public RuntimeValue Evaluate(Scope scope)
    {
        // 在实际实现中，这里应该加载并执行指定的模块
        Console.WriteLine($"Importing module: {ModuleName}");
        return new RuntimeValue(ValueType.Null, null);
    }
}

// 实现ForLoop类
public class ForLoop : INode
{
    public INode Initializer { get; set; }
    public INode Condition { get; set; }
    public INode Increment { get; set; }
    public BlockStatement Body { get; set; }

    public RuntimeValue Evaluate(Scope scope)
    {
        var loopScope = new Scope(scope);
        
        // 初始化
        if (Initializer != null)
            Initializer.Evaluate(loopScope);
        
        while (true)
        {
            // 检查条件
            if (Condition != null)
            {
                var condValue = Condition.Evaluate(loopScope);
                if (condValue.Type != ValueType.Boolean)
                    throw new Exception("For loop condition must evaluate to boolean");
                    
                if (!(bool)condValue.Value)
                    break;
            }
            
            // 执行循环体
            var result = Body.Evaluate(new Scope(loopScope));
            
            if (result is ReturnValue)
                return result;
                
            if (result is BreakSignal)
                break;
                
            // 如果是continue，跳过增量部分
            if (result is ContinueSignal)
                continue;
            
            // 执行增量
            if (Increment != null)
                Increment.Evaluate(loopScope);
        }
        
        return new RuntimeValue(ValueType.Null, null);
    }
}