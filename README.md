# Semantic Kernel Demo Project 🌟

Welcome to the **Semantic Kernel Demo Project**! This repository contains a collection of demos showcasing various capabilities and integrations of the Semantic Kernel. Each demo is designed to highlight specific features and use cases, providing a hands-on experience for developers. 🚀

---

## Table of Contents 📚

1. [Project Overview](#project-overview)
2. [Demos](#demos)
   - [Light Plugin Demo](#light-plugin-demo)
   - [Built-In Plugins](#built-in-plugins)
   - [Agentic RAG](#agentic-rag)
   - [MCP (Model Context Protocol)](#mcp-model-context-protocol)
   - [Multi-Agent Orchestration](#multi-agent-orchestration)
3. [Setup Instructions](#setup-instructions)
4. [Contributing](#contributing)
5. [License](#license)

---

## Project Overview 📝

The Semantic Kernel Demo Project is a collection of examples demonstrating the power of the Semantic Kernel. Each demo focuses on a unique aspect of the kernel, from plugin development to multi-agent orchestration. Whether you're a beginner or an experienced developer, these demos will help you understand and leverage the Semantic Kernel effectively.

---

## Demos 🎥

### 1. Light Plugin Demo 💡

**Description:**
This demo showcases how to create and use lightweight plugins with the Semantic Kernel. It provides a simple example to get started with plugin development.

**Setup:**
- Navigate to `src/Demo001_LightPluginDemo/`.
- Open the solution file `semantickerneldemo.sln` in Visual Studio.
- Build and run the project.

---

### 2. Built-In Plugins 🔌

**Description:**
Explore the built-in plugins available in the Semantic Kernel, including integrations with Bing Search, Google Text Search, and more.

**Setup:**
- Navigate to `src/Demo002_BuiltIn/`.
- Open the solution file `semantickerneldemo.sln` in Visual Studio.
- Build and run the desired plugin project (e.g., `GoogleTextSearch`).

---

### 3. Agentic RAG 📖

**Description:**
This demo demonstrates how to use the Semantic Kernel for Retrieval-Augmented Generation (RAG) with agents. It includes a recipe guide example.

**Setup:**
- Navigate to `src/Demo003_AgenticRAG/RecipeGuide/`.
- Open the solution file `semantickerneldemo.sln` in Visual Studio.
- Build and run the `RecipeGuide` project.

---

### 4. MCP (Model Context Protocol) 🌐

**Description:**
Learn how to set up and use the Model Context Protocol (MCP) server and client for advanced integrations.

**Setup:**
- Navigate to `src/Demo004_MCP/`.
- Install Node.js (if not already installed):
  ```
  Download and install Node.js from [https://nodejs.org/](https://nodejs.org/).
  ```
- Open a terminal and navigate to `src/Demo004_MCP/MCPServer/`.
- Run the following commands:
  ```
  npm install
  npm start
  ```
- Alternatively, you can use the MCP Inspector to run the server:
  ```
  npx @modelcontextprotocol/inspector dotnet run
  ```
  This will start the MCP Inspector, displaying a URL in the terminal (e.g., `http://127.0.0.1:6274`). Open this URL in a browser to access the MCP Inspector interface.
- Open the solution file `semantickerneldemo.sln` in Visual Studio.
- Build and run the `MCPClient` project.

---

### 5. Multi-Agent Orchestration 🤖

**Description:**
This demo highlights the orchestration of multiple agents using the Semantic Kernel. It includes examples like `AgentsAsPlugins` and `MultiAgentChat`.

**Setup:**
- Navigate to `src/Demo005_Multi-Agent_Orchestration/`.
- Open the solution file `semantickerneldemo.sln` in Visual Studio.
- Build and run the desired project (e.g., `MultiAgentChat`).

---

## Setup Instructions 🛠️

1. Clone the repository:
   ```
   git clone https://github.com/your-repo/semantickerneldemo.git
   ```
2. Install Visual Studio (if not already installed):
   - Download and install Visual Studio from [https://visualstudio.microsoft.com/](https://visualstudio.microsoft.com/).
3. Install Node.js (for MCP Server):
   - Download and install Node.js from [https://nodejs.org/](https://nodejs.org/).
4. Follow the setup instructions for each demo as described above.

---

## Contributing 🤝

Contributions are welcome! Please read the [CONTRIBUTING.md](CONTRIBUTING.md) file for guidelines on how to contribute to this project.

---

## License 📄

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.