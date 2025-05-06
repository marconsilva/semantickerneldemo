// Import packages
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents;
using DotNetEnv;

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

// Create a kernel with Azure OpenAI chat completion
//Full list of Supported Connectors: https://learn.microsoft.com/en-us/semantic-kernel/get-started/supported-languages?pivots=programming-language-csharp
var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// Add enterprise components
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Warning));

// Build the kernel
Kernel kernel = builder.Build();

Kernel salesAssistentKernel = Kernel.CreateBuilder()
.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey)
.Build();

salesAssistentKernel.Plugins.AddFromType<OrderPlugin>("Orders");

Kernel refundsAssistantKernel = Kernel.CreateBuilder()
.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey)
.Build();

refundsAssistantKernel.Plugins.AddFromType<RefundPlugin>("Refunds");


// Add plugins
var agentPlugin = KernelPluginFactory.CreateFromFunctions("AgentPlugin",
    [
            AgentKernelFunctionFactory.CreateFromAgent(new ChatCompletionAgent() {
                Name = "SalesAssistant",
                Instructions = "You are a sales assistant. Place orders for items the user requests.",
                Description = "Agent to invoke to place orders for items the user requests.",
                Kernel = salesAssistentKernel,
                Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            }),

            AgentKernelFunctionFactory.CreateFromAgent(new ChatCompletionAgent() {
                Name = "RefundAgent",
                Instructions = "You are a refund agent. Help the user with refunds.",
                Description = "Agent to invoke to execute a refund an item on behalf of the user.",
                Kernel = refundsAssistantKernel,
                Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            })
    ]);

kernel.Plugins.Add(agentPlugin);
kernel.AutoFunctionInvocationFilters.Add(new AutoFunctionInvocationFilter());


ChatCompletionAgent agent =
    new()
    {
        Name = "ShoppingAssistant",
        Instructions = "You are a sales assistant. Delegate to the provided agents to help the user with placing orders and requesting refunds.",
        Kernel = kernel,
        Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    };

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// Create a history store the conversation
var history = new ChatHistory();
AgentThread? agentThread = null;

// Initiate a back-and-forth chat
Console.WriteLine("Welcome to the Shopping Assistant Demo!");
Console.WriteLine("Use commands like:");
Console.WriteLine("\tPlace an order for a Super-Man shirt.");
Console.WriteLine("\tNow I want a refund for the Super-Man shirt.");
string? userInput;
do {
    // Collect user input
    
    Console.Write("User > ");
    userInput = Console.ReadLine();

    // Add user input
    if (!string.IsNullOrEmpty(userInput))
    {
        history.AddUserMessage(userInput);
    }else
    {
        break;
    }

    // Get the response from the AI
    var responseItems = agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, userInput), 
    agentThread);
    await foreach (var responseItem in responseItems)
    {
        if(responseItem.Message is not null){

            agentThread = responseItem.Thread;
            // Print the results
            Console.WriteLine($"{responseItem.Message.AuthorName} ({responseItem.Message.Role}) > {responseItem.Message.Content}");

            // Add the message from the agent to the chat history
            history.AddMessage(responseItem.Message.Role, responseItem.Message.Content ?? string.Empty);
        }
    }

} while (userInput is not null);




   
