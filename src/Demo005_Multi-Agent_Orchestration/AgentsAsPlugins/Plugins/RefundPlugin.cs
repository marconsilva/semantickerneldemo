using System.ComponentModel;
using Microsoft.SemanticKernel;
public sealed class RefundPlugin
{
    [KernelFunction, Description("Execute a refund for the specified item.")]
    public string ExecuteRefund(string itemName) => "success";
}