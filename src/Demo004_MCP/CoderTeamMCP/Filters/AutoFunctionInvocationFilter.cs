using Microsoft.SemanticKernel;

namespace CoderTeamMCP.Filters;

public sealed class AutoFunctionInvocationFilter() : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"Invoke: {context.Function.Name} - {context.Function.Description} - ({context.Function.PluginName})");

        await next(context);
    }
}