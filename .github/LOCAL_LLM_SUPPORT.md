# Local LLM Support for Agentic Copilot Features

This document provides detailed information about using local Large Language Models (LLMs) with this Game Discovery MCP Server for agentic (autonomous tool-calling) features.

## What is Agentic Copilot?

**Agentic** refers to AI models that can autonomously:
- Call tools/functions to accomplish tasks
- Make decisions about which tools to use
- Chain multiple tool calls together to solve complex problems
- Interact with MCP servers like this Game Discovery server

## Supported Local LLM Solutions

### 1. Ollama (Recommended for Local Use)

**Installation:**
```bash
# Download from https://ollama.ai
ollama pull llama3.1:70b
# or
ollama pull qwen2.5:32b
```

**MCP Configuration:**
Ollama doesn't directly support MCP protocol, but can be used through MCP clients like:
- Continue.dev (VS Code extension with MCP support)
- Open WebUI with MCP plugin

**Best Models for Agentic Tasks:**
- `llama3.1:70b` - Excellent function calling, 70B parameters
- `llama3.1:405b` - Best accuracy, requires significant RAM (200GB+)
- `qwen2.5:32b` - Good balance of performance and resource usage
- `mistral-nemo` - Smaller (12B), decent function calling

### 2. LM Studio

**Installation:**
Download from https://lmstudio.ai

**MCP Support:**
- LM Studio has beta MCP server integration
- Can load models with function calling capabilities
- Configure through Settings → Server → MCP Servers

**Recommended Models:**
- Llama 3.1 variants (8B, 70B, 405B)
- Mistral models with function calling
- Qwen 2.5 variants

**Configuration Example:**
```json
{
  "mcpServers": {
    "game-discovery": {
      "command": "dnx",
      "args": ["GameMcpServer@1.0.4", "--yes"]
    }
  }
}
```

### 3. Jan.ai

**Installation:**
Download from https://jan.ai

**MCP Support:**
- Native MCP integration in recent versions
- Easy-to-configure server management
- Built-in model downloader

**Setup:**
1. Open Jan → Settings → MCP Servers
2. Add server configuration:
   ```json
   {
     "name": "GameMcpServer",
     "command": "dnx",
     "args": ["GameMcpServer@1.0.4", "--yes"]
   }
   ```

### 4. Claude Desktop (Local MCP, Cloud Model)

**Note:** Not fully "local" as the model runs on Anthropic's servers, but MCP server runs locally.

**Installation:**
Download from https://claude.ai/desktop

**Configuration:**
Edit `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) or
`%APPDATA%\Claude\claude_desktop_config.json` (Windows):

```json
{
  "mcpServers": {
    "game-discovery": {
      "command": "dnx",
      "args": ["GameMcpServer@1.0.4", "--yes"]
    }
  }
}
```

**Advantages:**
- Best tool-calling accuracy
- Official MCP support from Anthropic
- Seamless integration

## GitHub Copilot Integration

### Current Status (as of 2025)

**GitHub Copilot Workspace:**
- Supports MCP servers for enhanced context
- Currently in preview
- Requires GitHub Copilot subscription

**VS Code with Copilot:**
- Native MCP support is limited
- Can use MCP through extensions like Continue.dev
- Configure via `.vscode/mcp.json`

### Alternative: VS Code Extensions

**Continue.dev Extension:**
```json
{
  "models": [
    {
      "title": "Local Llama",
      "provider": "ollama",
      "model": "llama3.1:70b"
    }
  ],
  "mcpServers": [
    {
      "name": "game-discovery",
      "command": "dnx",
      "args": ["GameMcpServer@1.0.4", "--yes"]
    }
  ]
}
```

## Model Requirements for Agentic Features

### Minimum Requirements
- **Parameter Count:** 7B+ (32B+ recommended for complex tasks)
- **Function Calling:** Native support for tool/function calling
- **Context Window:** 8K+ tokens (32K+ preferred)
- **RAM:** 16GB for 7B models, 64GB+ for 32B models, 200GB+ for 70B models

### Function Calling Support

Not all models support function calling. Verify with:
```bash
# For Ollama
ollama show llama3.1:70b --modelfile | grep -i "tool"
```

**Models WITH function calling:**
- ✅ Llama 3.1 series (8B, 70B, 405B)
- ✅ Mistral Nemo and larger
- ✅ Qwen 2.5 series (7B, 14B, 32B, 72B)
- ✅ Command-R series (Cohere)

**Models WITHOUT native function calling:**
- ❌ Llama 2.x series
- ❌ Most models under 7B parameters
- ❌ Older Mistral 7B base model

## Performance Considerations

### Game Discovery Task Complexity

This MCP server's tools (`discover_games`, `get_game_info`, `launch_game`) require:
- Understanding of tool descriptions
- Ability to parse JSON responses
- Decision-making about which tools to call
- Parameter extraction from user queries

### Recommended Model Sizes by Use Case

| Use Case | Recommended Model | RAM Required |
|----------|------------------|--------------|
| Basic game discovery | Qwen 2.5:14b | 16GB |
| Complex multi-tool tasks | Llama 3.1:70b | 64GB |
| Production/best results | Llama 3.1:405b or Claude | 200GB+ |
| Low-resource systems | Qwen 2.5:7b | 8GB |

## Troubleshooting

### "Model doesn't call tools"
- Ensure model supports function calling
- Check that prompts mention the tools are available
- Try larger models (32B+)
- Verify MCP server is running (`dnx GameMcpServer@1.0.4 --yes`)

### "MCP server connection failed"
- Check that .NET 10+ is installed
- Verify `dnx` is in PATH
- Review server logs in stderr
- Ensure no port conflicts

### "Poor tool selection"
- Use larger models (70B+)
- Provide clearer prompts
- Try Claude Desktop for baseline comparison

## Example Usage

### With Ollama + Continue.dev

1. Install Ollama and pull a model:
   ```bash
   ollama pull llama3.1:70b
   ```

2. Install Continue.dev extension in VS Code

3. Configure Continue (`.continue/config.json`):
   ```json
   {
     "models": [{
       "title": "Llama 3.1 70B",
       "provider": "ollama",
       "model": "llama3.1:70b"
     }],
     "mcpServers": [{
       "name": "game-discovery",
       "command": "dnx",
       "args": ["GameMcpServer@1.0.4", "--yes"]
     }]
   }
   ```

4. Ask the model: "Can you discover all the games I have installed on my PC?"

### With Claude Desktop

1. Install Claude Desktop
2. Configure MCP server (see above)
3. Restart Claude Desktop
4. Ask: "What games do I have installed? Launch my most played game."

## Additional Resources

- [Model Context Protocol Documentation](https://modelcontextprotocol.io)
- [Ollama Function Calling Guide](https://ollama.ai/blog/tool-support)
- [Continue.dev MCP Support](https://continue.dev/docs/mcp)
- [LM Studio Documentation](https://lmstudio.ai/docs)

## Contributing

If you find other local LLMs that work well with agentic features and this MCP server, please open an issue or PR to update this documentation.
