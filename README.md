# Game Discovery MCP Server

<!-- mcp-name: io.github.moonolgerd/game-mcp -->

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
- Visual Studio Code (optional, for development)

## Usage with VS Code

### For local development:
```json
{
  "servers": {
    "game-discovery": {
      "command": "dotnet",
      "args": ["run", "path/to/game-mcp.cs"]
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