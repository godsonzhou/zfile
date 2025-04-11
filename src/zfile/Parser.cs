using System;
using System.Collections.Generic;
using System.Linq;

public partial class Parser
{
    private readonly List<Token> tokens;
    private int position = 0;

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    // 解析程序
    public BlockStatement ParseProgram()
    {
        var program = new BlockStatement();
        
        while (position < tokens.Count && tokens[position].Type != TokenType.EOF)
        {
            try
            {
                var statement = ParseStatement();
                if (statement != null)
                    program.Statements.Add(statement);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析错误: {ex.Message} 在位置 {tokens[position].Line}:{tokens[position].Column}");
                // 尝试恢复解析，跳过当前语句
                SkipToNextStatement();
            }
        }
        
        return program;
    }

    // 跳过当前语句，尝试恢复解析
    private void SkipToNextStatement()
    {
        while (position < tokens.Count)
        {
            if (tokens[position].Type == TokenType.Punctuator && tokens[position].Value == ";")
            {
                position++;
                break;
            }
            position++;
        }
    }

    // 解析语句
    private INode ParseStatement()
    {
        var token = Peek();
        
        if (token.Type == TokenType.Keyword)
        {
            switch (token.Value)
            {
                case "if":
                    return ParseIfStatement();
                case "while":
                    return ParseWhileStatement();
                case "for":
                    return ParseForStatement();
                case "def":
                    return ParseFunctionDeclaration();
                case "class":
                    return ParseClassDeclaration();
                case "return":
                    return ParseReturnStatement();
                case "break":
                    Consume(); // 消费 break 关键字
                    ConsumeIfPresent(TokenType.Punctuator, ";");
                    return new BreakNode();
                case "continue":
                    Consume(); // 消费 continue 关键字
                    ConsumeIfPresent(TokenType.Punctuator, ";");
                    return new ContinueNode();
                case "var":
                case "let":
                    return ParseVariableDeclaration(false);
                case "const":
                    return ParseVariableDeclaration(true);
                case "import":
                    return ParseImportStatement();
            }
        }
        
        if (token.Type == TokenType.Punctuator && token.Value == "{")
        {
            return ParseBlock();
        }
        
        // 表达式语句
        var expr = ParseExpression();
        ConsumeIfPresent(TokenType.Punctuator, ";");
        return expr;
    }

    // 解析块语句
    private BlockStatement ParseBlock()
    {
        Consume(TokenType.Punctuator, "{");
        
        var block = new BlockStatement();
        
        while (position < tokens.Count && !(tokens[position].Type == TokenType.Punctuator && tokens[position].Value == "}"))
        {
            var statement = ParseStatement();
            if (statement != null)
                block.Statements.Add(statement);
        }
        
        Consume(TokenType.Punctuator, "}");
        return block;
    }

    // 解析if语句
    private INode ParseIfStatement()
    {
        Consume(TokenType.Keyword, "if");
        Consume(TokenType.Punctuator, "(");
        var condition = ParseExpression();
        Consume(TokenType.Punctuator, ")");
        
        var thenBranch = ParseStatement();
        BlockStatement elseBranch = null;
        
        if (position < tokens.Count && tokens[position].Type == TokenType.Keyword && tokens[position].Value == "else")
        {
            Consume(); // 消费 else 关键字
            elseBranch = ParseStatement() as BlockStatement;
            if (elseBranch == null)
            {
                // 如果else后面不是块语句，创建一个包含单个语句的块
                elseBranch = new BlockStatement();
                elseBranch.Statements.Add(ParseStatement());
            }
        }
        
        return new IfStatement
        {
            Condition = condition,
            ThenBranch = thenBranch as BlockStatement ?? new BlockStatement { Statements = { thenBranch } },
            ElseBranch = elseBranch
        };
    }

    // 解析while语句
    private INode ParseWhileStatement()
    {
        Consume(TokenType.Keyword, "while");
        Consume(TokenType.Punctuator, "(");
        var condition = ParseExpression();
        Consume(TokenType.Punctuator, ")");
        
        var body = ParseStatement();
        
        return new WhileLoop
        {
            Condition = condition,
            Body = body as BlockStatement ?? new BlockStatement { Statements = { body } }
        };
    }

    // 解析for语句
    private INode ParseForStatement()
    {
        Consume(TokenType.Keyword, "for");
        Consume(TokenType.Punctuator, "(");
        
        // 初始化部分
        INode initializer = null;
        if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != ";")
        {
            if (tokens[position].Type == TokenType.Keyword && 
                (tokens[position].Value == "var" || tokens[position].Value == "let" || tokens[position].Value == "const"))
            {
                bool isConstant = tokens[position].Value == "const";
                Consume(); // 消费关键字
                initializer = ParseVariableDeclarationWithoutKeyword(isConstant);
            }
            else
            {
                initializer = ParseExpression();
            }
        }
        Consume(TokenType.Punctuator, ";");
        
        // 条件部分
        INode condition = null;
        if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != ";")
        {
            condition = ParseExpression();
        }
        else
        {
            // 如果没有条件，默认为true
            condition = new LiteralNode(true);
        }
        Consume(TokenType.Punctuator, ";");
        
        // 增量部分
        INode increment = null;
        if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != ")")
        {
            increment = ParseExpression();
        }
        Consume(TokenType.Punctuator, ")");
        
        var body = ParseStatement();
        
        return new ForLoop
        {
            Initializer = initializer,
            Condition = condition,
            Increment = increment,
            Body = body as BlockStatement ?? new BlockStatement { Statements = { body } }
        };
    }

    // 解析函数声明
    private INode ParseFunctionDeclaration()
    {
        Consume(TokenType.Keyword, "def");
        string name = Consume(TokenType.Identifier).Value;
        
        Consume(TokenType.Punctuator, "(");
        var parameters = new List<string>();
        
        if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != ")")
        {
            do
            {
                parameters.Add(Consume(TokenType.Identifier).Value);
            } while (ConsumeIfPresent(TokenType.Punctuator, ","));
        }
        
        Consume(TokenType.Punctuator, ")");
        var body = ParseBlock();
        
        return new FunctionDeclarationNode
        {
            Name = name,
            Parameters = parameters,
            Body = body
        };
    }

    // 解析类声明
    private INode ParseClassDeclaration()
    {
        Consume(TokenType.Keyword, "class");
        string className = Consume(TokenType.Identifier).Value;
        
        string parentName = null;
        if (ConsumeIfPresent(TokenType.Keyword, "extends"))
        {
            parentName = Consume(TokenType.Identifier).Value;
        }
        
        Consume(TokenType.Punctuator, "{");
        
        var body = new BlockStatement();
        
        while (position < tokens.Count && !(tokens[position].Type == TokenType.Punctuator && tokens[position].Value == "}"))
        {
            if (tokens[position].Type == TokenType.Keyword && tokens[position].Value == "def")
            {
                Consume(); // 消费 function 关键字
                string methodName = Consume(TokenType.Identifier).Value;
                
                Consume(TokenType.Punctuator, "(");
                var parameters = new List<string>();
                
                if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != ")")
                {
                    do
                    {
                        parameters.Add(Consume(TokenType.Identifier).Value);
                    } while (ConsumeIfPresent(TokenType.Punctuator, ","));
                }
                
                Consume(TokenType.Punctuator, ")");
                var methodBody = ParseBlock();
                
                body.Statements.Add(new MethodDefNode(methodName, parameters, methodBody));
            }
            else
            {
                // 类字段
                string fieldName = Consume(TokenType.Identifier).Value;
                ConsumeIfPresent(TokenType.Punctuator, ";");
                body.Statements.Add(new FieldDefNode(fieldName));
            }
        }
        
        Consume(TokenType.Punctuator, "}");
        
        return new ClassInheritance
        {
            ClassName = className,
            ParentName = parentName,
            Body = body
        };
    }

    // 解析return语句
    private INode ParseReturnStatement()
    {
        Consume(TokenType.Keyword, "return");
        
        INode value = null;
        if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != ";")
        {
            value = ParseExpression();
        }
        
        ConsumeIfPresent(TokenType.Punctuator, ";");
        
        return new ReturnNode(value);
    }

    // 解析变量声明
    private INode ParseVariableDeclaration(bool isConstant)
    {
        Consume(); // 消费 var/let/const 关键字
        return ParseVariableDeclarationWithoutKeyword(isConstant);
    }

    // 解析变量声明（不包括关键字）
    private INode ParseVariableDeclarationWithoutKeyword(bool isConstant)
    {
        string name = Consume(TokenType.Identifier).Value;
        
        INode initializer = null;
        if (ConsumeIfPresent(TokenType.Operator, "="))
        {
            initializer = ParseExpression();
        }
        else if (isConstant)
        {
            throw new Exception("常量必须初始化");
        }
        
        ConsumeIfPresent(TokenType.Punctuator, ";");
        
        return new VariableDeclarationNode(name, initializer, isConstant);
    }

    // 解析import语句
    private INode ParseImportStatement()
    {
        Consume(TokenType.Keyword, "import");
        string moduleName = Consume(TokenType.String).Value;
        ConsumeIfPresent(TokenType.Punctuator, ";");
        
        return new ImportNode(moduleName);
    }

    // 解析表达式
    private INode ParseExpression()
    {
        return ParseAssignment();
    }

    // 解析赋值表达式
    private INode ParseAssignment()
    {
        var expr = ParseLogicalOr();
        
        if (tokens[position].Type == TokenType.Operator && tokens[position].Value == "=")
        {
            Consume(); // 消费 = 运算符
            var value = ParseAssignment(); // 递归解析右侧表达式
            
            if (expr is IdentifierNode identNode)
            {
                return new VariableAssignmentNode(identNode.Name, value);
            }
            else if (expr is PropertyAccessNode propNode)
            {
                return new PropertyAssignmentNode(propNode.Object, propNode.Property, value);
            }
            else if (expr is IndexAccessNode indexNode)
            {
                return new IndexAssignmentNode(indexNode.Object, indexNode.Index, value);
            }
            
            throw new Exception("无效的赋值目标");
        }
        
        return expr;
    }

    // 解析逻辑或表达式
    private INode ParseLogicalOr()
    {
        var expr = ParseLogicalAnd();
        
        while (tokens[position].Type == TokenType.Operator && tokens[position].Value == "||") 
        {
            Consume(); // 消费 || 运算符
            var right = ParseLogicalAnd();
            expr = new BinaryOpNode("||" , expr, right);
        }
        
        return expr;
    }

    // 解析逻辑与表达式
    private INode ParseLogicalAnd()
    {
        var expr = ParseEquality();
        
        while (tokens[position].Type == TokenType.Operator && tokens[position].Value == "&&") 
        {
            Consume(); // 消费 && 运算符
            var right = ParseEquality();
            expr = new BinaryOpNode("&&", expr, right);
        }
        
        return expr;
    }

    // 解析相等性表达式
    private INode ParseEquality()
    {
        var expr = ParseComparison();
        
        while (tokens[position].Type == TokenType.Operator && 
              (tokens[position].Value == "==" || tokens[position].Value == "!=")) 
        {
            var op = Consume().Value;
            var right = ParseComparison();
            expr = new BinaryOpNode(op, expr, right);
        }
        
        return expr;
    }

    // 解析比较表达式
    private INode ParseComparison()
    {
        var expr = ParseAdditive();
        
        while (tokens[position].Type == TokenType.Operator && 
              (tokens[position].Value == "<" || tokens[position].Value == ">" || 
               tokens[position].Value == "<=" || tokens[position].Value == ">=")) 
        {
            var op = Consume().Value;
            var right = ParseAdditive();
            expr = new BinaryOpNode(op, expr, right);
        }
        
        return expr;
    }

    // 解析加法表达式
    private INode ParseAdditive()
    {
        var expr = ParseMultiplicative();
        
        while (tokens[position].Type == TokenType.Operator && 
              (tokens[position].Value == "+" || tokens[position].Value == "-")) 
        {
            var op = Consume().Value;
            var right = ParseMultiplicative();
            expr = new BinaryOpNode(op, expr, right);
        }
        
        return expr;
    }

    // 解析乘法表达式
    private INode ParseMultiplicative()
    {
        var expr = ParseUnary();
        
        while (tokens[position].Type == TokenType.Operator && 
              (tokens[position].Value == "*" || tokens[position].Value == "/" || tokens[position].Value == "%")) 
        {
            var op = Consume().Value;
            var right = ParseUnary();
            expr = new BinaryOpNode(op, expr, right);
        }
        
        return expr;
    }

    // 解析一元表达式
    private INode ParseUnary()
    {
        if (tokens[position].Type == TokenType.Operator && 
           (tokens[position].Value == "-" || tokens[position].Value == "!" || 
            tokens[position].Value == "++" || tokens[position].Value == "--"))
        {
            var op = Consume().Value;
            var expr = ParseUnary();
            return new UnaryOpNode(op, expr);
        }
        
        return ParsePostfix();
    }

    // 解析后缀表达式
    private INode ParsePostfix()
    {
        var expr = ParsePrimary();
        
        while (true)
        {
            if (tokens[position].Type == TokenType.Operator && 
               (tokens[position].Value == "++" || tokens[position].Value == "--"))
            {
                var op = Consume().Value;
                expr = new PostfixOpNode(op, expr);
            }
            else if (tokens[position].Type == TokenType.Punctuator && tokens[position].Value == ".")
            {
                Consume(); // 消费 . 符号
                string property = Consume(TokenType.Identifier).Value;
                expr = new PropertyAccessNode(expr, property);
            }
            else if (tokens[position].Type == TokenType.Punctuator && tokens[position].Value == "[")
            {
                Consume(); // 消费 [ 符号
                var index = ParseExpression();
                Consume(TokenType.Punctuator, "]");
                expr = new IndexAccessNode(expr, index);
            }
            else if (tokens[position].Type == TokenType.Punctuator && tokens[position].Value == "(")
            {
                expr = ParseFunctionCall(expr);
            }
            else
            {
                break;
            }
        }
        
        return expr;
    }

    // 解析函数调用
    private INode ParseFunctionCall(INode callee)
    {
        Consume(TokenType.Punctuator, "(");
        var args = new List<INode>();
        
        if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != ")")
        {
            do
            {
                args.Add(ParseExpression());
            } while (ConsumeIfPresent(TokenType.Punctuator, ","));
        }
        
        Consume(TokenType.Punctuator, ")");
        
        if (callee is PropertyAccessNode propNode)
        {
            return new MethodCallNode(propNode.Object, propNode.Property, args);
        }
        else if (callee is IdentifierNode identNode)
        {
            return new FunctionCall
            {
                Name = identNode.Name,
                Args = args
            };
        }
        
        throw new Exception("无效的函数调用");
    }

    // 解析基本表达式
    private INode ParsePrimary()
    {
        var token = Peek();
        
        switch (token.Type)
        {
            case TokenType.Number:
                Consume();
                return new LiteralNode(double.Parse(token.Value));
                
            case TokenType.String:
                Consume();
                return new LiteralNode(token.Value);
                
            case TokenType.Keyword:
                if (token.Value == "true" || token.Value == "false")
                {
                    Consume();
                    return new LiteralNode(token.Value == "true");
                }
                else if (token.Value == "null")
                {
                    Consume();
                    return new LiteralNode(null);
                }
                else if (token.Value == "this")
                {
                    Consume();
                    return new ThisNode();
                }
                else if (token.Value == "super")
                {
                    Consume();
                    return new SuperNode();
                }
                else if (token.Value == "new")
                {
                    return ParseNewExpression();
                }
                break;
                
            case TokenType.Identifier:
                Consume();
                return new IdentifierNode(token.Value);
                
            case TokenType.Punctuator:
                if (token.Value == "(")
                {
                    Consume();
                    var expr = ParseExpression();
                    Consume(TokenType.Punctuator, ")");
                    return expr;
                }
                else if (token.Value == "[")
                {
                    return ParseArrayLiteral();
                }
                else if (token.Value == "{")
                {
                    return ParseObjectLiteral();
                }
                break;
        }
        
        throw new Exception($"意外的标记: {token.Value}");
    }

    // 解析new表达式
    private INode ParseNewExpression()
    {
        Consume(TokenType.Keyword, "new");
        string className = Consume(TokenType.Identifier).Value;
        
        Consume(TokenType.Punctuator, "(");
        var args = new List<INode>();
        
        if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != ")")
        {
            do
            {
                args.Add(ParseExpression());
            } while (ConsumeIfPresent(TokenType.Punctuator, ","));
        }
        
        Consume(TokenType.Punctuator, ")");
        
        return new NewExpressionNode(className, args);
    }

    // 解析数组字面量
    private INode ParseArrayLiteral()
    {
        Consume(TokenType.Punctuator, "[");
        var items = new List<INode>();
        
        if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != "]")
        {
            do
            {
                items.Add(ParseExpression());
            } while (ConsumeIfPresent(TokenType.Punctuator, ","));
        }
        
        Consume(TokenType.Punctuator, "]");
        
        return new ListLiteralNode(items);
    }

    // 解析对象字面量
    private INode ParseObjectLiteral()
    {
        Consume(TokenType.Punctuator, "{");
        var properties = new Dictionary<string, INode>();
        
        if (tokens[position].Type != TokenType.Punctuator || tokens[position].Value != "}")
        {
            do
            {
                string key;
                if (tokens[position].Type == TokenType.String)
                {
                    key = Consume().Value;
                }
                else
                {
                    key = Consume(TokenType.Identifier).Value;
                }
                
                Consume(TokenType.Punctuator, ":");
                var value = ParseExpression();
                
                properties[key] = value;
            } while (ConsumeIfPresent(TokenType.Punctuator, ","));
        }
        
        Consume(TokenType.Punctuator, "}");
        
        return new DictLiteralNode(properties);
    }
    
    // 辅助方法：获取当前标记但不消费
    private Token Peek()
    {
        if (position >= tokens.Count)
            return new Token(TokenType.EOF, "", 0, 0);
        return tokens[position];
    }
    
    // 辅助方法：消费当前标记并返回
    private Token Consume()
    {
        if (position >= tokens.Count)
            throw new Exception("意外的文件结束");
        return tokens[position++];
    }
    
    // 辅助方法：消费指定类型和值的标记
    private Token Consume(TokenType expectedType, string expectedValue = null)
    {
        var token = Peek();
        
        if (token.Type != expectedType)
            throw new Exception($"期望 {expectedType}，但得到 {token.Type}");
            
        if (expectedValue != null && token.Value != expectedValue)
            throw new Exception($"期望 '{expectedValue}'，但得到 '{token.Value}'");
            
        return Consume();
    }
    
    // 辅助方法：如果当前标记匹配指定类型和值，则消费并返回true
    private bool ConsumeIfPresent(TokenType expectedType, string expectedValue = null)
    {
        var token = Peek();
        
        if (token.Type != expectedType)
            return false;
            
        if (expectedValue != null && token.Value != expectedValue)
            return false;
            
        Consume();
        return true;
    }
}