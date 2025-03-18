﻿using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace MCPSharp.ExternalExample
{

    [McpTool("external_tools", "for testing accessing tool classes loaded from a library")]
    public class ExternalTool
    {

        [McpFunction("dll-tool", "attempts to use a tool that is loaded from an external assembly dll. should return 'success'")]
        public static async Task<string> UseAsync() 
        {
            return await Task.Run(()=>"success");
        }

    }

    public class SemKerExample
    {
        [KernelFunction("SemanticTest")]
        [Description("test semantic kernel integration")]
        public static async Task<string> UseAsync()
        {
            return await Task.Run(() => "success");
        }
    }
}
