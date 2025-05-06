// Import packages
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using DotNetEnv;
using Microsoft.SemanticKernel.Plugins.Web.Google;

// Load environment variables from .env file
var root = Directory.GetCurrentDirectory();
var dotenv = Path.Combine(root, ".env");
Env.Load(dotenv);


// Populate values from your OpenAI deployment
var modelId = "gpt-4o";
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var openAIAPIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
var googleAPIKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
var googleSearchEngineID = Environment.GetEnvironmentVariable("GOOGLE_SEARCH_ENGINE_ID");


// Check if the environment variables are set
if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(openAIAPIKey) || string.IsNullOrEmpty(googleAPIKey) || string.IsNullOrEmpty(googleSearchEngineID))
{
    Console.WriteLine("Please set the environment variables in the .env file.");
    return;
}

// Create a kernel with Azure OpenAI chat completion
//Full list of Supported Connectors: https://learn.microsoft.com/en-us/semantic-kernel/get-started/supported-languages?pivots=programming-language-csharp
var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, googleAPIKey);

// Add enterprise components
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Warning));

// Build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();


// Create a text search using Google Text search
// ATTENTION: This is a Pre-release version of the Google search plugin, is subject to change.
var textSearch = new GoogleTextSearch(
    searchEngineId: googleSearchEngineID,
    apiKey: googleAPIKey);

// Build a text search plugin with GoogleSearch search and add to the kernel
var searchPlugin = textSearch.CreateWithSearch("SearchPlugin");
kernel.Plugins.Add(searchPlugin);


// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// Create a history store the conversation
var history = new ChatHistory();



var query = "What is the Semantic Kernel?";

Console.WriteLine($"Query: {query}\n");
// Search and return results as a string items
var stringResults = await textSearch.SearchAsync(
    query);
Console.WriteLine("--- String Results ---\n");
await foreach (string result in stringResults.Results)
{
    Console.WriteLine(result);
}

Console.ReadKey(true);

// Search and return results as TextSearchResult items
KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(query, new() { Top = 4, Skip = 4 });
Console.WriteLine("\n——— Text Search Results ———\n");
await foreach (TextSearchResult result in textResults.Results)
{
    Console.WriteLine($"Name:  {result.Name}");
    Console.WriteLine($"Value: {result.Value}");
    Console.WriteLine($"Link:  {result.Link}");
}
Console.ReadKey(true);

// Search and return results as Google.Apis.CustomSearchAPI.v1.Data.Result items
KernelSearchResults<object> fullResults = await textSearch.GetSearchResultsAsync(query, new() { Top = 4, Skip = 8 });
Console.WriteLine("\n——— Google Web Page Results ———\n");
await foreach (Google.Apis.CustomSearchAPI.v1.Data.Result result in fullResults.Results)
{
    Console.WriteLine($"Title:       {result.Title}");
    Console.WriteLine($"Snippet:     {result.Snippet}");
    Console.WriteLine($"Link:        {result.Link}");
    Console.WriteLine($"DisplayLink: {result.DisplayLink}");
    Console.WriteLine($"Kind:        {result.Kind}");
}
Console.ReadKey(true);
