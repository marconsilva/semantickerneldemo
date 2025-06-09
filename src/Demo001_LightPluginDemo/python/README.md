# Light Plugin Demo (Python)

This is the Python version of the Light Plugin Demo using Semantic Kernel.

## Setup

1. Create a virtual environment:
   ```
   python -m venv venv
   ```

2. Activate the virtual environment:
   - Windows: `venv\Scripts\activate`
   - Linux/Mac: `source venv/bin/activate`

3. Install dependencies:
   ```
   pip install -r requirements.txt
   ```

4. Create a `.env` file in the project root with your Azure OpenAI credentials:
   ```
   AZURE_OPENAI_ENDPOINT=your_endpoint_here
   AZURE_OPENAI_KEY=your_key_here
   ```

## Running the Demo

Run the program:
```
python program.py
```

You can interact with the light plugin by asking questions like:
- "Show me all the lights"
- "Turn on the Main Stage light"
- "Change the color of the Second Stage light to blue (#0000FF)"
- "Set the brightness of the Outside light to high"
