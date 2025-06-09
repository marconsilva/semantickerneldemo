// See https://aka.ms/new-console-template for more information

using CoderTeamMCP.Filters;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using ModelContextProtocol.Client;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0110 // Suppress experimental warning for AgentGroupChat
#pragma warning disable SKEXP0111 // Suppress experimental warning for Agent

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
    Command = "podman",
    Arguments = ["run", "-i", "--rm", "-e", "GITHUB_PERSONAL_ACCESS_TOKEN", "ghcr.io/github/github-mcp-server"],
    EnvironmentVariables = new() { { "GITHUB_PERSONAL_ACCESS_TOKEN", gitHubToken } },
});

var mcpClient = await McpClientFactory.CreateAsync(clientTransport);
IList<McpClientTool> tools = await mcpClient.ListToolsAsync();

// Create base kernel with Azure OpenAI chat completion
var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Warning));
var mainKernel = builder.Build();

// Add Invocation Filters
mainKernel.AutoFunctionInvocationFilters.Add(new AutoFunctionInvocationFilter());

// Define Agent Configurations
var issueReaderAgentTools = new HashSet<string> { "get_issue", "list_issues" };
var codeWriterAgentTools = new HashSet<string> { "get_file_contents", "create_branch", "create_or_update_file", "list_branches" };
var pullRequestAgentTools = new HashSet<string> { "create_pull_request", "create_pull_request_review" };

// Create specialized kernels for each agent
var issueReaderKernel = mainKernel.Clone();

var codeWriterKernel = mainKernel.Clone();

var pullRequestKernel = mainKernel.Clone();

// Add MCP tools to each specialized kernel
var issueReaderKernelFunctions = tools.Where(t => issueReaderAgentTools.Contains(t.Name)).Select(aiFunction => aiFunction.AsKernelFunction());
issueReaderKernel.Plugins.AddFromFunctions("GitHub_IssueReader", issueReaderKernelFunctions);

var codeWriterKernelFunctions = tools.Where(t => codeWriterAgentTools.Contains(t.Name)).Select(aiFunction => aiFunction.AsKernelFunction());
codeWriterKernel.Plugins.AddFromFunctions("GitHub_CodeWriter", codeWriterKernelFunctions);

var pullRequestKernelFunctions = tools.Where(t => pullRequestAgentTools.Contains(t.Name)).Select(aiFunction => aiFunction.AsKernelFunction());
pullRequestKernel.Plugins.AddFromFunctions("GitHub_PullRequest", pullRequestKernelFunctions);

// Create agents as plugins
var agentPlugin = KernelPluginFactory.CreateFromFunctions("GitHubAgentPlugin",
    [
        AgentKernelFunctionFactory.CreateFromAgent(new ChatCompletionAgent()
        {
            Name = "IssueReaderAgent",
            Instructions = "You are an agent that reads and lists GitHub issues. Use the provided tools to get information about issues. Always provide clear, structured information about the issues you find.",
            Description = "Agent to invoke for reading and listing GitHub issues",
            Kernel = issueReaderKernel,
            Arguments = new KernelArguments(new PromptExecutionSettings()
                { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new () { RetainArgumentTypes = true } ) }),
        }),

        AgentKernelFunctionFactory.CreateFromAgent(new ChatCompletionAgent()
        {
            Name = "CodeWriterAgent", 
            Instructions = "You are an agent that reads files, creates branches, and creates or updates files in a GitHub repository." +
                           "When creating new code, start by creating a new branch, then read the most relevant files." +
                           "Finally, create or update files in the repository." +
                           "Use the provided tools to manage code efficiently and follow best practices.",
            Description = "Agent to invoke for reading files, creating branches, and managing code in GitHub repositories",
            Kernel = codeWriterKernel,
            Arguments = new KernelArguments(new PromptExecutionSettings()
                { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new () { RetainArgumentTypes = true } ) }),
        }),

        AgentKernelFunctionFactory.CreateFromAgent(new ChatCompletionAgent()
        {
            Name = "PullRequestAgent",
            Instructions = "You are an agent that creates pull requests and reviews them. Use the provided tools to manage pull requests professionally and provide thorough reviews.",
            Description = "Agent to invoke for creating and reviewing pull requests",
            Kernel = pullRequestKernel,
            Arguments = new KernelArguments(new PromptExecutionSettings()
                { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new () { RetainArgumentTypes = true } )}),
        })
    ]);

// Add the agent plugin to the main kernel
mainKernel.Plugins.Add(agentPlugin);

// Create the main orchestrator agent
ChatCompletionAgent orchestratorAgent = new()
{
    Name = "GitHubOrchestrator",
    Instructions = @"You are a GitHub workflow orchestrator that coordinates between specialized agents to help users with GitHub-related tasks. 

Delegate tasks to the appropriate agents:
- Use IssueReaderAgent for reading and listing GitHub issues
- Use CodeWriterAgent for file operations, branch management, and code updates
- Use PullRequestAgent for creating and reviewing pull requests

Always explain what you're doing and coordinate the workflow logically. For complex tasks, break them down into steps and use multiple agents in sequence.
Be resilient - if an agent fails, try multiple times or instruct the agents properly and retry.",
    Kernel = mainKernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() 
        { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new () { RetainArgumentTypes = true } ) }),
};

// Create conversation state
var history = new ChatHistory();
AgentThread? agentThread = null;

// Start the interactive session
Console.WriteLine("Multi-Agent GitHub Demo Started (Agents as Plugins)");
Console.WriteLine("Examples:");
Console.WriteLine("\t- List issues in a repository");
Console.WriteLine("\t- Create a new branch and add a file");
Console.WriteLine("\t- Create a pull request");
Console.WriteLine("Type 'exit' to end.\n");
Console.WriteLine("\nDEMO Repo: jmnbc/contoso-foods\n");

string? userInput;
do
{
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine();

    // Handle exit conditions
    if (string.IsNullOrEmpty(userInput) || 
        userInput.Equals("exit", StringComparison.OrdinalIgnoreCase) || 
        userInput.Equals("quit", StringComparison.OrdinalIgnoreCase) || 
        userInput.Equals("q", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    // Add user input to history
    history.AddUserMessage(userInput);

    // Get response from orchestrator agent
    var responseItems = orchestratorAgent.InvokeAsync(new ChatMessageContent(AuthorRole.User, userInput), agentThread);
    
    await foreach (var responseItem in responseItems)
    {
        if (responseItem.Message is not null)
        {
            agentThread = responseItem.Thread;
            Console.WriteLine($"{responseItem.Message.AuthorName} ({responseItem.Message.Role}) > {responseItem.Message.Content}");
            
            // Add the response to chat history
            history.AddMessage(responseItem.Message.Role, responseItem.Message.Content ?? string.Empty);
        }
    }

    Console.WriteLine(); // Add spacing between interactions

} while (!string.IsNullOrEmpty(userInput));

Console.WriteLine("\nGitHub Agent Demo finished.\n");

