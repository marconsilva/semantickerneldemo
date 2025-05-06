// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using MCPClient.Samples;

namespace MCPClient;

internal sealed class Program
{
    /// <summary>
    /// Main method to run all the samples.
    /// </summary>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Running MCP Client samples...");
        await MCPToolsSample.RunAsync();
        Console.ReadKey(true);

        Console.WriteLine("Running MCP Prompt samples...");
        await MCPPromptSample.RunAsync();
        Console.ReadKey(true);

        Console.WriteLine("Running MCP Resources samples...");
        await MCPResourcesSample.RunAsync();
        Console.ReadKey(true);

        Console.WriteLine("Running MCP Resource Templates samples...");
        await MCPResourceTemplatesSample.RunAsync();
        Console.ReadKey(true);

        Console.WriteLine("Running MCP Sampling samples...");
        await MCPSamplingSample.RunAsync();
        Console.ReadKey(true);

        Console.WriteLine("Running MCP Chat Completion Agent samples...");
        await ChatCompletionAgentWithMCPToolsSample.RunAsync();
        Console.ReadKey(true);

        //Console.WriteLine("Running Azure AI Agent with MCP Tools samples...");
        //await AzureAIAgentWithMCPToolsSample.RunAsync();
        //Console.ReadKey(true);

        //Console.WriteLine("Running Azure AI Agent with MCP Tool samples...");
        //await AgentAvailableAsMCPToolSample.RunAsync();
        //Console.ReadKey(true);
    }
}