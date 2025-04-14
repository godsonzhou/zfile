using MCPSharp;
using Microsoft.SemanticKernel;
using System.ComponentModel;

public class MySkillClass
{
	[KernelFunction("MyFunction")]
	[Description("Description of my function")]
	public string MyFunction(string input) => $"Processed: {input}";
}


public class Calculator
{
	[McpTool("add", "Adds two numbers")]  // Note: [McpFunction] is deprecated, use [McpTool] instead
	public static int Add([McpParameter(true)] int a, [McpParameter(true)] int b)
	{
		return a + b;
	}
}