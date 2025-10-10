# Game Discovery MCP Server

<!-- mcp-name: io.github.moonolgerd/game-mcp -->

[![NuGet](https://img.shields.io/nuget/v/GameMcpServer.svg)](https://www.nuget.org/packages/GameMcpServer/)

A Model Context Protocol (MCP) server that discovers and manages installed games on Windows PC from various platforms including Steam, Epic Games, GOG, Windows Store/Xbox, and other installed programs.

## Features

- **Multi-platform game discovery**: Automatically scans for games from:
  - Steam (including custom library locations)
  - Epic Games Store
  - GOG (Good Old Games)
  - Xbox/Windows Store games
  - EA games
  - Ubisoft Connect games
  - Rockstar games
  - Battle.Net games
  - Other installed programs (via Windows registry)

- **Comprehensive game information**: Provides details including:
  - Game name and platform
  - Installation path and executable location
  - Installation date and size
  - Platform-specific metadata

- **MCP Tools Available**:
  - `discover_games`: Finds all installed games across all platforms
  - `get_game_info`: Gets detailed information about a specific game
  - `launch_game`: Launches a game by name (if executable is found)

## Prerequisites

- .NET 10.0 or later
- Windows operating system

## Local LLM Support for Agentic Features

This MCP server works with any MCP-compatible client. For **agentic** (autonomous tool-calling) capabilities, the following local LLMs are supported:

### Fully Supported Local LLMs
- **Claude via API** (Anthropic) - Full MCP and agentic support through Claude Desktop or API
- **Ollama** with function-calling models:
  - `llama3.1:70b` and larger - Native function calling support
  - `mistral-nemo` - Built-in tool use capabilities
  - `qwen2.5:32b` and larger - Strong function calling performance
- **LM Studio** - Supports MCP servers with compatible models
- **Jan.ai** - MCP integration available

### Configuration Notes
- **Agentic features** require models that support function/tool calling
- Smaller models (<7B parameters) may struggle with complex tool orchestration
- For best results with game discovery, use models with 32B+ parameters
- MCP protocol is client-agnostic - any MCP client can connect to this server

### GitHub Copilot Integration
- GitHub Copilot Workspace supports MCP servers for enhanced context
- Local LLM support in VS Code requires MCP-compatible extensions
- See `.vscode/mcp.json` for configuration example

**📖 For detailed information, see [Local LLM Support Guide](.github/LOCAL_LLM_SUPPORT.md)**

## Usage with VS Code

```json
{
  "servers": {
    "McpServer1": {
      "type": "stdio",
      "command": "dnx",
      "args": [
        "GameMcpServer@1.0.4",
        "--yes"
      ]
    }
  }
}
```
## Available Tools

### `discover_games`
Discovers all installed games from all supported platforms.

**Example usage in Claude:**
"Can you discover all the games I have installed on my PC?"

**Returns:** JSON object with:
- Total game count
- Games grouped by platform
- Detailed list of all games with metadata

### `get_game_info`
Gets detailed information about a specific game by name.

**Parameters:**
- `gameName`: The name of the game to search for

**Example usage:**
"Get information about Cyberpunk 2077"

### `launch_game`
Launches a game by name if it has a valid executable path.

**Parameters:**
- `gameName`: The exact name of the game to launch

**Example usage:**
"Launch Steam's Half-Life 2"

## How It Works

The server uses multiple discovery methods:

1. **Steam Games**: 
   - Reads Steam installation path from registry
   - Parses `libraryfolders.vdf` for custom library locations
   - Scans `steamapps/common` directories for game folders

2. **Epic Games**: 
   - Reads game manifests from `%ProgramData%/Epic/EpicGamesLauncher/Data/Manifests`
   - Parses JSON manifest files for game information

3. **GOG Games**: 
   - Queries Windows registry under `SOFTWARE\GOG.com\Games`
   - Extracts game paths and executable information

4. **Xbox/Windows Store**: 
   - Scans `%ProgramFiles%/WindowsApps` directory
   - Parses AppxManifest.xml files for application information
   - Filters for gaming applications

5. **Registry Programs**: 
   - Scans Windows uninstall registry entries
   - Uses heuristics to identify game-like applications
   - Filters out system utilities and non-gaming software

## Error Handling

The server is designed to be robust and continue functioning even if:
- Some platforms are not installed
- Registry keys are inaccessible
- Directories have permission restrictions
- Individual games have corrupted metadata

Errors are logged to stderr and don't interrupt the discovery process for other platforms.

## Security Considerations

- The server only reads system information and doesn't modify anything
- Game launching requires explicit user confirmation through the MCP client
- All file system access is read-only except for launching executables
- Registry access is read-only

## Development

This project uses:
- .NET 10.0
- Model Context Protocol SDK for .NET
- Microsoft.Extensions.Hosting for application lifecycle