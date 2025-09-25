# Game Discovery MCP Server - AI Agent Instructions

## Architecture Overview

This is a **Model Context Protocol (MCP) server** built as a **single C# script** (`game-mcp.cs`) that discovers installed games across multiple Windows platforms. The project uses:

- **Single-file architecture**: Everything in `game-mcp.cs` (683 lines) - no traditional project structure
- **Top-level program**: Uses C# 9+ top-level statements with inline package references
- **MCP framework**: Uses `ModelContextProtocol@0.3.0-preview.3` with .NET hosting
- **JSON source generation**: Uses `GameJsonContext` for AOT-compatible serialization

## Key Patterns & Conventions

### MCP Tool Registration Pattern
```csharp
[McpServerToolType]
public static class GameDiscoveryTools
{
    [McpServerTool(Name = "discover_games")]
    [Description("...")]
    public static async Task<string> DiscoverGames() { ... }
}
```

### Platform Discovery Pattern
Each platform (Steam, Epic, GOG, Xbox, Registry) follows this structure:
- Private `DiscoverXxxGames()` method returning `List<Game>`
- Registry/file system scanning with error isolation
- Graceful degradation when platforms unavailable
- Consistent `Game` object mapping

### Error Handling Convention
- **Continue on platform errors**: Individual platform failures don't break discovery
- **Stderr logging**: Uses `Console.Error.WriteLine()` for debugging without breaking MCP protocol
- **JSON error responses**: All tools return JSON with `ErrorResponse` type for failures

### JSON Serialization Approach
- **Source generation required**: Uses `GameJsonContext.Default.XxxType` pattern
- **Response classes**: Separate DTOs (`GameInfo`, `GameDiscoveryResult`, `LaunchResponse`, `ErrorResponse`)
- **No reflection**: AOT-compatible with `[JsonSerializable]` attributes

## Critical Development Workflows

### Running the MCP Server
```bash
dotnet run game-mcp.cs  # Starts stdio MCP server
```

### VS Code Integration
Configure in `.vscode/mcp.json`:
```json
{
  "servers": {
    "game-discovery": {
      "type": "stdio", 
      "command": "dotnet",
      "args": ["run", "D:\\Repos\\game-mcp\\game-mcp.cs"]
    }
  }
}
```

### Adding New Game Platforms
1. Create `DiscoverNewPlatformGames()` method following existing pattern
2. Add to `DiscoverGames()` aggregation
3. Test error isolation with non-existent platforms
4. Update `IsLikelyGame()` heuristics if needed

### Registry Discovery Heuristics
Located in `IsLikelyGame()` - uses keyword matching:
- **Game keywords**: "game", "play", "adventure", "action", "rpg", etc.
- **Publisher whitelist**: "valve", "epic", "ubisoft", "ea", etc.
- **Exclusion list**: "runtime", "redistributable", "driver", etc.

## Windows-Specific Implementation Details

### Registry Paths Used
- Steam: `SOFTWARE\WOW6432Node\Valve\Steam` → `InstallPath`
- GOG: `SOFTWARE\WOW6432Node\GOG.com\Games\{gameId}` → `gameName`, `path`, `exe`
- Uninstall: `SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall`

### File System Scanning
- **Steam**: Parses `libraryfolders.vdf` for custom library locations
- **Epic**: Scans `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\*.item` JSON files
- **Xbox**: Attempts `%ProgramFiles%\WindowsApps` (permission-limited)

### Executable Discovery Logic
`FindGameExecutable()` filters out installers/redistributables and prioritizes executables matching game name.

## Common Modification Patterns

- **Adding response fields**: Update DTO classes + `GameJsonContext` + mapping logic
- **New discovery sources**: Follow platform discovery pattern with error isolation
- **Enhanced filtering**: Modify `IsLikelyGame()` keyword arrays
- **Performance optimization**: Consider caching in private static fields (currently rescans each call)

## Testing & Debugging

- **MCP protocol**: All output must be valid JSON - use stderr for debug logging
- **Platform availability**: Test with missing platforms (Steam uninstalled, etc.)
- **Permission errors**: Xbox/WindowsApps access commonly fails - handle gracefully
- **Large libraries**: Directory size calculation can be slow - consider async patterns