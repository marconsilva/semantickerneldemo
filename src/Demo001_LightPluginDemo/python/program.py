from dotenv import load_dotenv
import os
import asyncio
from semantic_kernel import Kernel
from semantic_kernel.functions import kernel_function
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion, AzureChatPromptExecutionSettings
from semantic_kernel.connectors.ai.chat_completion_client_base import ChatCompletionClientBase
from semantic_kernel.connectors.ai import FunctionChoiceBehavior
from semantic_kernel.contents import ChatHistory
from semantic_kernel.functions import KernelArguments

from lights_plugin import LightsPlugin

async def main():
    # Load environment variables from .env file
    load_dotenv()

    # Populate values from your OpenAI deployment
    model_id = "gpt-4o"
    endpoint = os.environ.get("AZURE_OPENAI_ENDPOINT")
    api_key = os.environ.get("AZURE_OPENAI_KEY")

    if not endpoint:
        raise ValueError("AZURE_OPENAI_ENDPOINT environment variable is not set")
    if not api_key:
        raise ValueError("AZURE_OPENAI_KEY environment variable is not set")

    # Create a kernel with Azure OpenAI chat completion
    kernel = Kernel()
    
    # Add Azure OpenAI chat service
    kernel.add_service(
        AzureChatCompletion(
            service_id="chat_completion",
            deployment_name=model_id,
            endpoint=endpoint,
            api_key=api_key
        )
    )
    
    # Add a plugin (the LightsPlugin class is defined in its own file)
    kernel.add_plugin(LightsPlugin(), "Lights")

    chat_completion : AzureChatCompletion = kernel.get_service(type=ChatCompletionClientBase)

    # Enable planning
    execution_settings = AzureChatPromptExecutionSettings()
    execution_settings.function_choice_behavior = FunctionChoiceBehavior.Auto()
    
    # Create a history to store the conversation
    chat_history = ChatHistory()
    
    # Initiate a back-and-forth chat
    while True:
        # Collect user input
        user_input = input("User > ")
        
        if not user_input:
            break
            
        # Add user input
        chat_history.add_user_message(user_input)
        
        # Get the response from the AI
        result = (await chat_completion.get_chat_message_contents(
            chat_history=chat_history,
            settings=execution_settings,
            kernel=kernel,
            kernel_arguments=KernelArguments(),
        ))[0]
        
        # Print the results
        print(f"Assistant > {result}")
        
        # Add the message from the agent to the chat history
        chat_history.add_message(result)

if __name__ == "__main__":
    asyncio.run(main())
