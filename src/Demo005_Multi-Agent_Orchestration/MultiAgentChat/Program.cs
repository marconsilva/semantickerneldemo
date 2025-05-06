// Import packages
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents;
using DotNetEnv;

string ReviewerName = "ArtDirector";
string ReviewerInstructions =
        """
        You are an art director who has opinions about copywriting born of a love for David Ogilvy.
        The goal is to determine if the given copy is acceptable to print.
        If so, state that it is approved.
        If not, provide insight on how to refine suggested copy without example.
        """;

string CopyWriterName = "CopyWriter";
string CopyWriterInstructions =
        """
        You are a copywriter with ten years of experience and are known for brevity and a dry humor.
        The goal is to refine and decide on the single best copy as an expert in the field.
        Only provide a single proposal per response.
        You're laser focused on the goal at hand.
        Don't waste time with chit chat.
        Consider suggestions when refining an idea.
        """;


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

Kernel reviewerAgentKernel = Kernel.CreateBuilder()
.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey)
.Build();

Kernel writerAgentKernel = Kernel.CreateBuilder()
.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey)
.Build();

// Create the agents
var reviewerAgent = new ChatCompletionAgent() {
                Name = ReviewerName,
                Instructions = ReviewerInstructions,
                Kernel = reviewerAgentKernel,
            };

var writerAgent = new ChatCompletionAgent() {
                Name = CopyWriterName,
                Instructions = CopyWriterInstructions,
                Kernel = writerAgentKernel,
            };

// Create the Agent Group Chat
AgentGroupChat chat = new (reviewerAgent, writerAgent)
{
                ExecutionSettings =
                    new()
                    {
                        // Here a TerminationStrategy subclass is used that will terminate when
                        // an assistant message contains the term "approve".
                        TerminationStrategy =
                            new ApprovalTerminationStrategy()
                            {
                                // Only the art-director may approve.
                                Agents = [reviewerAgent],
                                // Limit total number of turns
                                MaximumIterations = 10,
                            }
                    }
            };;

// Initiate a back-and-forth chat
Console.WriteLine("Welcome to the Art Direction and Creativity Demo!");
Console.WriteLine("Use commands like:");
Console.WriteLine("\tconcept: maps made out of egg cartons.");
string? userInput;
do {
    // Collect user input
    
    Console.Write("User > ");
    userInput = Console.ReadLine();

    // Add user input
    if (string.IsNullOrEmpty(userInput))
    {
        break;
    }

    ChatMessageContent userInputContent = new(AuthorRole.User, userInput);
    chat.AddChatMessage(userInputContent);

    AgentHelper.WriteAgentChatMessage(userInputContent);

    await foreach (ChatMessageContent response in chat.InvokeAsync())
    {
        AgentHelper.WriteAgentChatMessage(response);
    }

    Console.WriteLine($"\n[IS COMPLETED: {chat.IsComplete}]");
} while (chat.IsComplete == false);




   
