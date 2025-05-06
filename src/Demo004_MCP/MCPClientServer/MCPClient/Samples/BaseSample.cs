// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;

namespace MCPClient.Samples;

internal abstract class BaseSample
{

    public static string OpenAIEndpoint { get; set; }
    public static string OpenAIAPIKey { get; set; }
    public static string AzureAIEndpoint { get; set; }
    public static string AzureAIAPIKey { get; set; }

    public static void LoadCredentials()
    {
        // Load environment variables from .env file
        var root = Directory.GetCurrentDirectory();
        var dotenv = Path.Combine(root, ".env");
        Env.Load(dotenv);

        // Populate values from your OpenAI deployment
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? 
            throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT environment variable is not set");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? 
            throw new ArgumentNullException("AZURE_OPENAI_KEY environment variable is not set");
            
        var azureAIendpoint = Environment.GetEnvironmentVariable("AZUREAI_OPENAI_ENDPOINT") ?? 
            throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT environment variable is not set");
        var azureAIAPIKey = Environment.GetEnvironmentVariable("AZUREAI_OPENAI_KEY") ?? 
            throw new ArgumentNullException("AZURE_OPENAI_KEY environment variable is not set");

        OpenAIEndpoint = endpoint;
        OpenAIAPIKey = apiKey; 
        AzureAIEndpoint = azureAIendpoint;
        AzureAIAPIKey = azureAIAPIKey;
    }

    /// <summary>
    /// Creates an MCP client and connects it to the MCPServer server.
    /// </summary>
    /// <param name="kernel">Optional kernel instance to use for the MCP client.</param>
    /// <param name="samplingRequestHandler">Optional handler for MCP sampling requests.</param>
    /// <returns>An instance of <see cref="IMcpClient"/>.</returns>
    protected static Task<IMcpClient> CreateMcpClientAsync(
        Kernel? kernel = null,
        Func<Kernel, CreateMessageRequestParams?, IProgress<ProgressNotificationValue>, CancellationToken, Task<CreateMessageResult>>? samplingRequestHandler = null)
    {
        KernelFunction? skSamplingHandler = null;

        // Create and return the MCP client
        return McpClientFactory.CreateAsync(
            clientTransport: new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "MCPServer",
                Command = GetMCPServerPath(), // Path to the MCPServer executable
            }),
            clientOptions: samplingRequestHandler != null ? new McpClientOptions()
            {
                Capabilities = new ClientCapabilities
                {
                    Sampling = new SamplingCapability
                    {
                        SamplingHandler = InvokeHandlerAsync
                    },
                },
            } : null
         );

        async ValueTask<CreateMessageResult> InvokeHandlerAsync(CreateMessageRequestParams? request, IProgress<ProgressNotificationValue> progress, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            skSamplingHandler ??= KernelFunctionFactory.CreateFromMethod(
                (CreateMessageRequestParams? request, IProgress<ProgressNotificationValue> progress, CancellationToken ct) =>
                {
                    return samplingRequestHandler(kernel!, request, progress, ct);
                },
                "MCPSamplingHandler"
            );

            // The argument names must match the parameter names of the delegate the SK Function is created from
            KernelArguments kernelArguments = new()
            {
                ["request"] = request,
                ["progress"] = progress
            };

            FunctionResult functionResult = await skSamplingHandler.InvokeAsync(kernel!, kernelArguments, cancellationToken);

            return functionResult.GetValue<CreateMessageResult>()!;
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="Kernel"/> with the OpenAI chat completion service registered.
    /// </summary>
    /// <returns>An instance of <see cref="Kernel"/>.</returns>
    protected static Kernel CreateKernelWithChatCompletionService()
    {
        // Load and validate configuration
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        string modelId = config["OpenAI:ChatModelId"] ?? "gpt-4o";

        // Create kernel
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddAzureOpenAIChatCompletion(endpoint: OpenAIEndpoint, deploymentName: modelId, apiKey: OpenAIAPIKey);

        return kernelBuilder.Build();
    }

    /// <summary>
    /// Displays the list of available MCP tools.
    /// </summary>
    /// <param name="tools">The list of the tools to display.</param>
    protected static void DisplayTools(IList<McpClientTool> tools)
    {
        Console.WriteLine("Available MCP tools:");
        foreach (var tool in tools)
        {
            Console.WriteLine($"- Name: {tool.Name}, Description: {tool.Description}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Returns the path to the MCPServer server executable.
    /// </summary>
    /// <returns>The path to the MCPServer server executable.</returns>
    private static string GetMCPServerPath()
    {
        // Determine the configuration (Debug or Release)  
        string configuration;

#if DEBUG
        configuration = "Debug";
#else
        configuration = "Release";
#endif

        return Path.Combine("..", "..", "..", "..", "MCPServer", "bin", configuration, "net9.0", "MCPServer.exe");
    }
}