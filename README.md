# Game Discovery MCP Server

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
  - **Playtime data**: Hours played and last played date (where available)
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

5. **Ubisoft Connect Games**:
   - Queries Windows registry for Ubisoft launcher installation path
   - Scans registry uninstall entries for Ubisoft-published games
   - Searches common Ubisoft Connect game directories
   - **Playtime Collection**: Attempts to extract playtime data from:
     - Ubisoft Connect database files and configuration files in `%LocalAppData%\Ubisoft Game Launcher`
     - Game save files and logs within installation directories
     - Executable modification times as fallback for last played detection

6. **Registry Programs**: 
   - Scans Windows uninstall registry entries
   - Uses heuristics to identify game-like applications
   - Filters out system utilities and non-gaming software

### Playtime Collection Support

The server attempts to collect playtime data (hours played and last played date) for the following platforms:

- **Steam**: Reads playtime from user configuration files (`localconfig.vdf`, `sharedconfig.vdf`)
- **Epic Games**: Uses executable modification times as proxy for activity
- **GOG Galaxy**: Checks executable access times (full database support would require SQLite dependencies)
- **Ubisoft Connect**: Scans launcher configuration files and game save data for playtime information
- **Other platforms**: Uses fallback methods like executable modification times

Note: Playtime data availability depends on platform-specific storage formats and may not be available for all games.

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