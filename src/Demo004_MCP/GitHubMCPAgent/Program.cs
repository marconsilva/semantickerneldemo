// Import packages
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using DotNetEnv;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Telemetry.Console;



// Load environment variables from .env file
var root = Directory.GetCurrentDirectory();
var dotenv = Path.Combine(root, ".env");
Env.Load(dotenv);

// Populate values from your OpenAI deployment
var modelId = "gpt-4o";
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? 
    throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT environment variable is not set");
var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? 
    throw new ArgumentNullException("AZURE_OPENAI_KEY environment variable is not set");
var gitHubToken = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN") ?? 
    throw new ArgumentNullException("GITHUB_PERSONAL_ACCESS_TOKEN environment variable is not set");

// Create an MCPClient for the GitHub server
var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "MCPServer",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-github"],
    EnvironmentVariables =  new() {{ "GITHUB_PERSONAL_ACCESS_TOKEN", gitHubToken }},
});

var mcpClient = await McpClientFactory.CreateAsync(clientTransport);

// Retrieve the list of tools available on the GitHub server
var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name}: {tool.Description}");
}

// Prepare and build kernel with the MCP tools as Kernel functions
// Create a kernel with Azure OpenAI chat completion
//Full list of Supported Connectors: https://learn.microsoft.com/en-us/semantic-kernel/get-started/supported-languages?pivots=programming-language-csharp
var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// var resourceBuilder = ResourceBuilder
//     .CreateDefault()
//     .AddService("TelemetryConsoleQuickstart");

// // Enable model diagnostics with sensitive data.
// AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

// using var traceProvider = Sdk.CreateTracerProviderBuilder()
//     .SetResourceBuilder(resourceBuilder)
//     .AddSource("Microsoft.SemanticKernel*")
//     .AddConsoleExporter()
//     .Build();

// using var meterProvider = Sdk.CreateMeterProviderBuilder()
//     .SetResourceBuilder(resourceBuilder)
//     .AddMeter("Microsoft.SemanticKernel*")
//     .AddConsoleExporter()
//     .Build();

// using var loggerFactory = LoggerFactory.Create(builder =>
// {
//     // Add OpenTelemetry as a logging provider
//     builder.AddOpenTelemetry(options =>
//     {
//         options.SetResourceBuilder(resourceBuilder);
//         options.AddConsoleExporter();
//         // Format log messages. This is default to false.
//         options.IncludeFormattedMessage = true;
//         options.IncludeScopes = true;
//     });
//     builder.SetMinimumLevel(LogLevel.Warning);
// });

//builder.Services.AddSingleton(loggerFactory);

builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Warning));

Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
kernel.Plugins.AddFromFunctions("GitHub", tools.Select(aiFunction => aiFunction.AsKernelFunction()));

// Enable automatic function calling
OpenAIPromptExecutionSettings executionSettings = new()
{
    Temperature = 0,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
};

// Test using GitHub tools
string? userInput;
Console.WriteLine("Welcome to the GitHub Agent! Type 'exit' to quit.");
Console.WriteLine("First mode - Tool Mode");
Console.WriteLine("You can ask me anything about GitHub repositories, and I will use the tools to answer your questions. Example:");
Console.WriteLine("\tSummarize the last four commits to the microsoft/semantic-kernel repository?");

do {
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine();

    // Add user input
    if (string.IsNullOrEmpty(userInput) || userInput == "exit" || userInput == "quit" || userInput == "q")
    {
        userInput = null;
        break;
    }

    var result = await kernel.InvokePromptAsync(userInput, new(executionSettings)).ConfigureAwait(false);
    Console.WriteLine($"Assistant > {result}");

}while (!string.IsNullOrEmpty(userInput));
Console.WriteLine("\n\nTool Mode finished.\n\n");



// Define the agent
ChatCompletionAgent agent = new ChatCompletionAgent()
{
    Instructions = "Answer questions about GitHub repositories.",
    Name = "GitHubAgent",
    Kernel = kernel,
    Arguments = new KernelArguments(executionSettings),
};

Console.WriteLine("\n\nNew Lets see this as an agent.\n\n");
Console.WriteLine("Second mode - Agent Mode Enabled");
Console.WriteLine("Ask me anything about a GitHub repository, example:");
Console.WriteLine("\tSummarize the last four commits to the microsoft/semantic-kernel repository?");

do {
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine();

    // Add user input
    if (string.IsNullOrEmpty(userInput) || userInput == "exit" || userInput == "quit" || userInput == "q")
    {
        userInput = null;
        break;
    }

    // Respond to user input, invoking functions where appropriate.
    ChatMessageContent response = await agent.InvokeAsync(userInput).FirstAsync();
    Console.WriteLine($"\n\nResponse from GitHubAgent:\n{response.Content}");
}while (!string.IsNullOrEmpty(userInput));
Console.WriteLine("\n\nAgent Mode finished.\n\n");