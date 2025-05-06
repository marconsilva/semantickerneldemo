using System.ComponentModel;
using Microsoft.SemanticKernel;

public sealed class OrderPlugin
{
    [KernelFunction, Description("Place an order for the specified item.")]
    public string PlaceOrder([Description("The name of the item to be ordered.")] string itemName) => "success";
}