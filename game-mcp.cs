#:package Microsoft.Extensions.Hosting@9.0.9
#:package ModelContextProtocol@0.3.0-preview.3
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr to avoid interfering with MCP protocol
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Add MCP server with stdio transport and register tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

/// <summary>
/// Container class for game discovery tools
/// </summary>
[McpServerToolType]
public static class GameDiscoveryTools
{
    /// <summary>
    /// Discovers all installed games from various platforms including Steam, Epic Games, GOG, Windows Store, and installed programs
    /// </summary>
    [McpServerTool(Name = "discover_games")]
    [Description("Discovers all installed games from Steam, Epic Games, GOG, Windows Store, and other installed programs")]
    public static async Task<string> DiscoverGames()
    {
        var games = new List<Game>();
        
        try
        {
            // Discover Steam games
            var steamGames = await DiscoverSteamGames();
            games.AddRange(steamGames);

            // Discover Epic Games
            var epicGames = await DiscoverEpicGames();
            games.AddRange(epicGames);

            // Discover GOG games
            var gogGames = await DiscoverGogGames();
            games.AddRange(gogGames);

            // Discover Windows Store/Xbox games
            var xboxGames = await DiscoverXboxGames();
            games.AddRange(xboxGames);

            // Discover Rockstar Games
            var rockstarGames = await DiscoverRockstarGames();
            games.AddRange(rockstarGames);

            // Discover Ubisoft Connect games
            var ubisoftGames = await DiscoverUbisoftGames();
            games.AddRange(ubisoftGames);

            // Discover Battle.net games
            var battleNetGames = await DiscoverBattleNetGames();
            games.AddRange(battleNetGames);

            // Discover EA games
            var eaGames = await DiscoverEAGames();
            games.AddRange(eaGames);

            // Discover other installed games from registry - DISABLED (not actual games)
            // var registryGames = await DiscoverRegistryGames();
            // games.AddRange(registryGames);

            // Remove duplicates and sort by playtime (descending), then by name
            var uniqueGames = games
                .GroupBy(g => Path.GetFullPath(g.InstallPath.TrimEnd('/', '\\')).ToLowerInvariant())
                .Select(group => group.First())
                .OrderByDescending(g => g.PlayTimeHours ?? 0)
                .ThenBy(g => g.Name)
                .ToList();

            var result = new GameDiscoveryResult
            {
                total_games = uniqueGames.Count,
                games_by_platform = uniqueGames.GroupBy(g => g.Platform).ToDictionary(g => g.Key, g => g.Count()),
                games = uniqueGames.Select(g => new GameInfo
                {
                    name = g.Name,
                    platform = g.Platform,
                    install_path = g.InstallPath,
                    executable = g.Executable,
                    install_date = g.InstallDate?.ToString("yyyy-MM-dd"),
                    size_mb = g.SizeMB,
                    last_played = g.LastPlayed?.ToString("yyyy-MM-dd"),
                    play_time_hours = g.PlayTimeHours
                }).ToList()
            };

            return JsonSerializer.Serialize(result, GameJsonContext.Default.GameDiscoveryResult);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new ErrorResponse { error = $"Failed to discover games: {ex.Message}" }, GameJsonContext.Default.ErrorResponse);
        }
    }

    /// <summary>
    /// Gets detailed information about a specific game by name
    /// </summary>
    [McpServerTool(Name = "get_game_info")]
    [Description("Gets detailed information about a specific game by name")]
    public static async Task<string> GetGameInfo([Description("The name of the game to get information about")] string gameName)
    {
        try
        {
            var games = new List<Game>();
            
            // Search all platforms for the specific game
            var steamGames = await DiscoverSteamGames();
            var epicGames = await DiscoverEpicGames();
            var gogGames = await DiscoverGogGames();
            var xboxGames = await DiscoverXboxGames();
            var rockstarGames = await DiscoverRockstarGames();
            var ubisoftGames = await DiscoverUbisoftGames();
            var battleNetGames = await DiscoverBattleNetGames();
            var eaGames = await DiscoverEAGames();
            // // var registryGames = await DiscoverRegistryGames(); // DISABLED - not actual games // DISABLED - not actual games
            
            games.AddRange(steamGames);
            games.AddRange(epicGames);
            games.AddRange(gogGames);
            games.AddRange(xboxGames);
            games.AddRange(rockstarGames);
            games.AddRange(ubisoftGames);
            games.AddRange(battleNetGames);
            games.AddRange(eaGames);
            // // games.AddRange(registryGames); // DISABLED - not actual games // DISABLED - not actual games

            var matchingGames = games
                .Where(g => g.Name.Contains(gameName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!matchingGames.Any())
            {
                return JsonSerializer.Serialize(new ErrorResponse { error = $"No games found matching '{gameName}'" }, GameJsonContext.Default.ErrorResponse);
            }

            var result = matchingGames.Select(g => new GameInfo
            {
                name = g.Name,
                platform = g.Platform,
                install_path = g.InstallPath,
                executable = g.Executable,
                install_date = g.InstallDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                size_mb = g.SizeMB,
                last_played = g.LastPlayed?.ToString("yyyy-MM-dd HH:mm:ss"),
                play_time_hours = g.PlayTimeHours
            }).ToArray();

            return JsonSerializer.Serialize(result, GameJsonContext.Default.GameInfoArray);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new ErrorResponse { error = $"Failed to get game info: {ex.Message}" }, GameJsonContext.Default.ErrorResponse);
        }
    }

    /// <summary>
    /// Launches a game if it's found and has a valid executable path
    /// </summary>
    [McpServerTool(Name = "launch_game")]
    [Description("Launches a game by name if it's installed and has a valid executable")]
    public static async Task<string> LaunchGame([Description("The name of the game to launch")] string gameName)
    {
        try
        {
            var games = new List<Game>();
            
            // Search all platforms for the specific game
            var steamGames = await DiscoverSteamGames();
            var epicGames = await DiscoverEpicGames();
            var gogGames = await DiscoverGogGames();
            var xboxGames = await DiscoverXboxGames();
            var rockstarGames = await DiscoverRockstarGames();
            var ubisoftGames = await DiscoverUbisoftGames();
            var battleNetGames = await DiscoverBattleNetGames();
            var eaGames = await DiscoverEAGames();
            var registryGames = await DiscoverRegistryGames();
            
            games.AddRange(steamGames);
            games.AddRange(epicGames);
            games.AddRange(gogGames);
            games.AddRange(xboxGames);
            games.AddRange(rockstarGames);
            games.AddRange(ubisoftGames);
            games.AddRange(battleNetGames);
            games.AddRange(eaGames);
            games.AddRange(registryGames);

            var game = games.FirstOrDefault(g => 
                g.Name.Equals(gameName, StringComparison.OrdinalIgnoreCase));

            if (game == null)
            {
                return JsonSerializer.Serialize(new ErrorResponse { error = $"Game '{gameName}' not found" }, GameJsonContext.Default.ErrorResponse);
            }

            if (string.IsNullOrEmpty(game.Executable) || !File.Exists(game.Executable))
            {
                return JsonSerializer.Serialize(new ErrorResponse { error = $"Game '{gameName}' executable not found or invalid: {game.Executable}" }, GameJsonContext.Default.ErrorResponse);
            }

            // Launch the game
            var processInfo = new ProcessStartInfo
            {
                FileName = game.Executable,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(game.Executable)
            };

            Process.Start(processInfo);

            return JsonSerializer.Serialize(new LaunchResponse
            { 
                success = true,
                message = $"Launched '{game.Name}' from {game.Platform}",
                executable = game.Executable
            }, GameJsonContext.Default.LaunchResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new ErrorResponse { error = $"Failed to launch game: {ex.Message}" }, GameJsonContext.Default.ErrorResponse);
        }
    }

    private static async Task<List<Game>> DiscoverSteamGames()
    {
        var games = new List<Game>();
        
        try
        {
            // Try to find Steam installation
            var steamPath = GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
                return games;

            var steamAppsPath = Path.Combine(steamPath, "steamapps");
            if (!Directory.Exists(steamAppsPath))
                return games;

            // Read library folders
            var libraryFoldersPath = Path.Combine(steamAppsPath, "libraryfolders.vdf");
            var libraryPaths = new List<string> { steamAppsPath };

            if (File.Exists(libraryFoldersPath))
            {
                var libraryContent = await File.ReadAllTextAsync(libraryFoldersPath);
                var pathMatches = Regex.Matches(libraryContent, @"""path""\s*""([^""]+)""");
                foreach (Match match in pathMatches)
                {
                    var path = match.Groups[1].Value.Replace(@"\\", @"\");
                    var steamAppsFolder = Path.Combine(path, "steamapps");
                    if (Directory.Exists(steamAppsFolder))
                        libraryPaths.Add(steamAppsFolder);
                }
            }

            // Scan each library path for games
            foreach (var libraryPath in libraryPaths)
            {
                var commonPath = Path.Combine(libraryPath, "common");
                if (!Directory.Exists(commonPath))
                    continue;

                var gameFolders = Directory.GetDirectories(commonPath);
                foreach (var gameFolder in gameFolders)
                {
                    var gameName = Path.GetFileName(gameFolder);
                    var gameInfo = new DirectoryInfo(gameFolder);
                    
                    // Try to find the main executable
                    var executable = FindGameExecutable(gameFolder, gameName);
                    
                    // Only add Steam games that have executables and aren't launchers
                    if (!string.IsNullOrEmpty(executable) && !IsLauncherApplication(gameName))
                    {
                        // Try to find the Steam app ID from .acf files
                        var appId = await FindSteamAppId(libraryPath, gameName);
                        var playtime = await GetSteamPlaytimeAsync(appId ?? "");
                        
                        games.Add(new Game
                        {
                            Name = gameName,
                            Platform = "Steam",
                            InstallPath = gameFolder,
                            Executable = executable,
                            InstallDate = gameInfo.CreationTime,
                            SizeMB = await GetDirectorySizeAsync(gameFolder) / (1024 * 1024),
                            PlayTimeHours = playtime.Item1,
                            LastPlayed = playtime.Item2
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but continue with other platforms
            Console.Error.WriteLine($"Error discovering Steam games: {ex.Message}");
        }

        return games;
    }

    private static async Task<string?> FindSteamAppId(string libraryPath, string gameName)
    {
        try
        {
            // Get Steam path to find all library paths
            var steamPath = GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
                return null;

            // Collect all Steam library paths
            var allLibraryPaths = new List<string> { Path.Combine(steamPath, "steamapps") };
            
            // Read library folders from libraryfolders.vdf
            var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryFoldersPath))
            {
                var libraryContent = await File.ReadAllTextAsync(libraryFoldersPath);
                var pathMatches = Regex.Matches(libraryContent, @"""path""\s*""([^""]+)""");
                foreach (Match match in pathMatches)
                {
                    var path = match.Groups[1].Value.Replace(@"\\", @"\");
                    var steamAppsFolder = Path.Combine(path, "steamapps");
                    if (Directory.Exists(steamAppsFolder))
                        allLibraryPaths.Add(steamAppsFolder);
                }
            }

            // Look for .acf files in all Steam library directories
            foreach (var libPath in allLibraryPaths)
            {
                if (!Directory.Exists(libPath))
                    continue;

                var acfFiles = Directory.GetFiles(libPath, "appmanifest_*.acf");
                foreach (var acfFile in acfFiles)
                {
                    var content = await File.ReadAllTextAsync(acfFile);
                    
                    // Parse the name from ACF file
                    var nameMatch = Regex.Match(content, @"""name""\s*""([^""]+)""", RegexOptions.IgnoreCase);
                    var installDirMatch = Regex.Match(content, @"""installdir""\s*""([^""]+)""", RegexOptions.IgnoreCase);
                    
                    if ((nameMatch.Success && nameMatch.Groups[1].Value.Equals(gameName, StringComparison.OrdinalIgnoreCase)) ||
                        (installDirMatch.Success && installDirMatch.Groups[1].Value.Equals(gameName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Extract app ID from filename
                        var fileName = Path.GetFileNameWithoutExtension(acfFile);
                        var appIdMatch = Regex.Match(fileName, @"appmanifest_(\d+)");
                        if (appIdMatch.Success)
                        {
                            return appIdMatch.Groups[1].Value;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error finding Steam app ID for {gameName}: {ex.Message}");
        }
        
        return null;
    }

    private static async Task<List<Game>> DiscoverEpicGames()
    {
        var games = new List<Game>();
        
        try
        {
            var epicManifestsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Epic", "EpicGamesLauncher", "Data", "Manifests");

            if (!Directory.Exists(epicManifestsPath))
                return games;

            var manifestFiles = Directory.GetFiles(epicManifestsPath, "*.item");
            
            foreach (var manifestFile in manifestFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(manifestFile);
                    var manifest = JsonSerializer.Deserialize(content, GameJsonContext.Default.JsonElement);
                    
                    if (manifest.TryGetProperty("DisplayName", out var displayName) &&
                        manifest.TryGetProperty("InstallLocation", out var installLocation))
                    {
                        var gameName = displayName.GetString() ?? "Unknown Game";
                        var installPath = installLocation.GetString() ?? "";
                        
                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        {
                            var executable = FindGameExecutable(installPath, gameName);
                            var gameInfo = new DirectoryInfo(installPath);
                            var playtime = await GetEpicPlaytimeAsync(gameName, installPath);
                            
                            games.Add(new Game
                            {
                                Name = gameName,
                                Platform = "Epic Games",
                                InstallPath = installPath,
                                Executable = executable,
                                InstallDate = gameInfo.CreationTime,
                                SizeMB = await GetDirectorySizeAsync(installPath) / (1024 * 1024),
                                PlayTimeHours = playtime.Item1,
                                LastPlayed = playtime.Item2
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error reading Epic manifest {manifestFile}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering Epic Games: {ex.Message}");
        }

        return games;
    }

    private static async Task<List<Game>> DiscoverGogGames()
    {
        var games = new List<Game>();
        
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games") ??
                           Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GOG.com\Games");
            
            if (key == null)
                return games;

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                try
                {
                    using var gameKey = key.OpenSubKey(subKeyName);
                    if (gameKey == null) continue;

                    var gameName = gameKey.GetValue("gameName")?.ToString();
                    var path = gameKey.GetValue("path")?.ToString();
                    var exe = gameKey.GetValue("exe")?.ToString();

                    if (!string.IsNullOrEmpty(gameName) && !string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        var executable = string.IsNullOrEmpty(exe) ? 
                            FindGameExecutable(path, gameName) : 
                            Path.Combine(path, exe);

                        var gameInfo = new DirectoryInfo(path);
                        
                        var playtime = await GetGogPlaytimeAsync(gameName, path);
                        games.Add(new Game
                        {
                            Name = gameName,
                            Platform = "GOG",
                            InstallPath = path,
                            Executable = executable,
                            InstallDate = gameInfo.CreationTime,
                            SizeMB = await GetDirectorySizeAsync(path) / (1024 * 1024),
                            PlayTimeHours = playtime.Item1,
                            LastPlayed = playtime.Item2
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error reading GOG game registry: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering GOG games: {ex.Message}");
        }

        return games;
    }

    private static async Task<List<Game>> DiscoverXboxGames()
    {
        var games = new List<Game>();
        
        try
        {
            // Method 1: Check Xbox Game Pass / Microsoft Store registry
            await DiscoverXboxRegistryGames(games);
            
            // Method 2: Check WindowsApps folder (requires permissions)
            await DiscoverWindowsAppsGames(games);
            
            // Method 3: Check alternative Xbox install locations
            await DiscoverAlternativeXboxLocations(games);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering Xbox games: {ex.Message}");
        }
        
        return games;
    }
    
    private static async Task DiscoverXboxRegistryGames(List<Game> games)
    {
        try
        {
            // Check Microsoft Store / Xbox app installations in registry
            var registryPaths = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications",
                @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppxAppType",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            
            foreach (var registryPath in registryPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(registryPath);
                    if (key == null) continue;
                    
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var gameKey = key.OpenSubKey(subKeyName);
                            if (gameKey == null) continue;
                            
                            var displayName = gameKey.GetValue("DisplayName")?.ToString();
                            var publisher = gameKey.GetValue("Publisher")?.ToString();
                            var installLocation = gameKey.GetValue("InstallLocation")?.ToString();
                            
                            // Look for Xbox/Microsoft games
                            if (!string.IsNullOrEmpty(displayName) && 
                                !string.IsNullOrEmpty(publisher) &&
                                (publisher.ToLowerInvariant().Contains("microsoft") || 
                                 publisher.ToLowerInvariant().Contains("xbox") ||
                                 subKeyName.Contains("Microsoft.")) &&
                                IsLikelyGame(displayName) &&
                                !string.IsNullOrEmpty(installLocation) &&
                                Directory.Exists(installLocation))
                            {
                                var executable = FindGameExecutable(installLocation, displayName);
                                var gameInfo = new DirectoryInfo(installLocation);
                                
                                // Avoid duplicates
                                if (!games.Any(g => g.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    games.Add(new Game
                                    {
                                        Name = displayName,
                                        Platform = "Xbox Games",
                                        InstallPath = installLocation,
                                        Executable = executable,
                                        InstallDate = gameInfo.CreationTime,
                                        SizeMB = await GetDirectorySizeAsync(installLocation) / (1024 * 1024)
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error reading Xbox registry entry {subKeyName}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error accessing Xbox registry path {registryPath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering Xbox registry games: {ex.Message}");
        }
    }
    
    private static async Task DiscoverWindowsAppsGames(List<Game> games)
    {
        try
        {
            // Xbox/Windows Store games are installed in WindowsApps folder
            var windowsAppsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
            
            if (!Directory.Exists(windowsAppsPath))
                return;

            try
            {
                var directories = Directory.GetDirectories(windowsAppsPath);
                
                foreach (var dir in directories)
                {
                    try
                    {
                        var dirName = Path.GetFileName(dir);
                        
                        // Skip system apps and look for game-like patterns
                        if (dirName.Contains("Microsoft.") && !IsLikelyGame(dirName))
                            continue;

                        var manifestPath = Path.Combine(dir, "AppxManifest.xml");
                        if (File.Exists(manifestPath))
                        {
                            var content = await File.ReadAllTextAsync(manifestPath);
                            var displayNameMatch = Regex.Match(content, @"DisplayName=""([^""]+)""");
                            
                            if (displayNameMatch.Success)
                            {
                                var gameName = displayNameMatch.Groups[1].Value;
                                
                                // Skip if it's clearly not a game, auxiliary content, or already exists
                                if (IsLikelyGame(gameName) && 
                                    !IsXboxAuxiliaryContent(gameName) &&
                                    !games.Any(g => g.Name.Equals(gameName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    var executable = FindGameExecutable(dir, gameName);
                                    var sizeMB = await GetDirectorySizeAsync(dir) / (1024 * 1024);
                                    
                                    // Only include if it has an executable and is reasonably sized (> 50MB)
                                    if (!string.IsNullOrEmpty(executable) && sizeMB > 50)
                                    {
                                        var gameInfo = new DirectoryInfo(dir);
                                        
                                        games.Add(new Game
                                        {
                                            Name = gameName,
                                            Platform = "Xbox Games",
                                            InstallPath = dir,
                                            Executable = executable,
                                            InstallDate = gameInfo.CreationTime,
                                            SizeMB = sizeMB
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip directories we can't access
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error processing Xbox app directory {dir}: {ex.Message}");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Cannot access WindowsApps directory - this is normal due to permissions");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering WindowsApps games: {ex.Message}");
        }
    }
    
    private static async Task DiscoverAlternativeXboxLocations(List<Game> games)
    {
        try
        {
            // Check alternative Xbox Game Pass installation locations
            var alternativePaths = new[]
            {
                @"C:\Program Files\ModifiableWindowsApps",
                @"C:\Program Files (x86)\ModifiableWindowsApps",
                @"C:\XboxGames",
                @"D:\XboxGames",
                @"E:\XboxGames"
            };
            
            foreach (var path in alternativePaths.Where(Directory.Exists))
            {
                try
                {
                    var gameDirectories = Directory.GetDirectories(path)
                        .Where(dir => !IsNonGameDirectory(Path.GetFileName(dir)))
                        .ToArray();
                    
                    foreach (var gameDir in gameDirectories)
                    {
                        var gameName = Path.GetFileName(gameDir);
                        
                        // Avoid duplicates and filter auxiliary content
                        if (!games.Any(g => g.Name.Equals(gameName, StringComparison.OrdinalIgnoreCase)) &&
                            !IsXboxAuxiliaryContent(gameName))
                        {
                            var executable = FindGameExecutable(gameDir, gameName);
                            var sizeMB = await GetDirectorySizeAsync(gameDir) / (1024 * 1024);
                            
                            // Only include if it has an executable and is reasonably sized (> 50MB)
                            if (!string.IsNullOrEmpty(executable) && sizeMB > 50)
                            {
                                var gameInfo = new DirectoryInfo(gameDir);
                                
                                games.Add(new Game
                                {
                                    Name = gameName,
                                    Platform = "Xbox Games",
                                    InstallPath = gameDir,
                                    Executable = executable,
                                    InstallDate = gameInfo.CreationTime,
                                    SizeMB = sizeMB
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error scanning Xbox directory {path}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering alternative Xbox locations: {ex.Message}");
        }
    }

    private static async Task<List<Game>> DiscoverRockstarGames()
    {
        var games = new List<Game>();
        
        try
        {
            // Try to find Rockstar Games Launcher installation from registry
            string? rockstarPath = null;
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Rockstar Games\Launcher") ??
                               Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Rockstar Games\Launcher");
                rockstarPath = key?.GetValue("InstallFolder")?.ToString();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error reading Rockstar registry: {ex.Message}");
            }

            // Also check common installation paths
            var possiblePaths = new List<string>();
            if (!string.IsNullOrEmpty(rockstarPath))
                possiblePaths.Add(rockstarPath);
            
            possiblePaths.AddRange(new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Rockstar Games"),
                @"C:\Program Files\Rockstar Games",
                @"C:\Program Files (x86)\Rockstar Games",
                @"E:\Games\Rockstar Games",
                @"D:\Games\Rockstar Games",
                @"F:\Games\Rockstar Games"
            });

            foreach (var basePath in possiblePaths.Where(Directory.Exists))
            {
                try
                {
                    var gameDirectories = Directory.GetDirectories(basePath);

                    foreach (var gameDir in gameDirectories)
                    {
                        var gameName = Path.GetFileName(gameDir);
                        var gameInfo = new DirectoryInfo(gameDir);
                        
                        // Skip non-game directories
                        if (IsNonGameDirectory(gameName))
                            continue;

                        var executable = FindGameExecutable(gameDir, gameName);
                        
                        games.Add(new Game
                        {
                            Name = gameName,
                            Platform = "Rockstar Games",
                            InstallPath = gameDir,
                            Executable = executable,
                            InstallDate = gameInfo.CreationTime,
                            SizeMB = await GetDirectorySizeAsync(gameDir) / (1024 * 1024)
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error scanning Rockstar directory {basePath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering Rockstar games: {ex.Message}");
        }

        return games;
    }

    private static async Task<List<Game>> DiscoverUbisoftGames()
    {
        var games = new List<Game>();
        
        try
        {
            // Try to find Ubisoft Connect installation path
            string? ubisoftPath = null;
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher") ??
                               Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Ubisoft\Launcher");
                ubisoftPath = key?.GetValue("InstallDir")?.ToString();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error reading Ubisoft registry: {ex.Message}");
            }

            // Check common Ubisoft Connect paths
            var possiblePaths = new List<string>();
            if (!string.IsNullOrEmpty(ubisoftPath))
            {
                var gamesPath = Path.Combine(ubisoftPath, "games");
                if (Directory.Exists(gamesPath))
                    possiblePaths.Add(gamesPath);
            }

            possiblePaths.AddRange(new[]
            {
                @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games",
                @"C:\Program Files\Ubisoft\Ubisoft Game Launcher\games",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Ubisoft\Ubisoft Game Launcher\games"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Ubisoft\Ubisoft Game Launcher\games")
            });

            // Also check for games installed in custom locations via registry
            try
            {
                using var uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstallKey != null)
                {
                    foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                    {
                        try
                        {
                            using var gameKey = uninstallKey.OpenSubKey(subKeyName);
                            if (gameKey == null) continue;

                            var publisher = gameKey.GetValue("Publisher")?.ToString();
                            var displayName = gameKey.GetValue("DisplayName")?.ToString();
                            var installLocation = gameKey.GetValue("InstallLocation")?.ToString();

                            if (!string.IsNullOrEmpty(publisher) && !string.IsNullOrEmpty(displayName) && 
                                !string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation) &&
                                publisher.Contains("Ubisoft", StringComparison.OrdinalIgnoreCase) &&
                                !IsLauncherApplication(displayName))
                            {
                                var executable = FindGameExecutable(installLocation, displayName);
                                var gameInfo = new DirectoryInfo(installLocation);
                                var playtime = await GetUbisoftPlaytimeAsync(displayName, installLocation);
                                
                                games.Add(new Game
                                {
                                    Name = displayName,
                                    Platform = "Ubisoft Connect",
                                    InstallPath = installLocation,
                                    Executable = executable,
                                    InstallDate = gameInfo.CreationTime,
                                    SizeMB = await GetDirectorySizeAsync(installLocation) / (1024 * 1024),
                                    PlayTimeHours = playtime.Item1,
                                    LastPlayed = playtime.Item2
                                });
                            }
                            

                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error reading Ubisoft registry entry {subKeyName}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error scanning Ubisoft registry: {ex.Message}");
            }

            // Scan directory-based installations
            foreach (var gamesPath in possiblePaths.Where(Directory.Exists))
            {
                try
                {
                    var gameDirectories = Directory.GetDirectories(gamesPath);
                    
                    foreach (var gameDir in gameDirectories)
                    {
                        var gameName = Path.GetFileName(gameDir);
                        var gameInfo = new DirectoryInfo(gameDir);
                        
                        var executable = FindGameExecutable(gameDir, gameName);
                        
                        // Skip non-game directories, launchers, and avoid duplicates from registry scan
                        if (!IsNonGameDirectory(gameName) && !IsLauncherApplication(gameName) &&
                            !games.Any(g => g.InstallPath.Equals(gameDir, StringComparison.OrdinalIgnoreCase)))
                        {
                            var playtime = await GetUbisoftPlaytimeAsync(gameName, gameDir);
                            
                            games.Add(new Game
                            {
                                Name = gameName,
                                Platform = "Ubisoft Connect",
                                InstallPath = gameDir,
                                Executable = executable,
                                InstallDate = gameInfo.CreationTime,
                                SizeMB = await GetDirectorySizeAsync(gameDir) / (1024 * 1024),
                                PlayTimeHours = playtime.Item1,
                                LastPlayed = playtime.Item2
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error scanning Ubisoft directory {gamesPath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering Ubisoft games: {ex.Message}");
        }

        return games;
    }

    private static async Task<List<Game>> DiscoverRegistryGames()
    {
        var games = new List<Game>();
        
        try
        {
            // Check Windows uninstall registry for games
            var uninstallKeys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var uninstallKeyPath in uninstallKeys)
            {
                try
                {
                    using var uninstallKey = Registry.LocalMachine.OpenSubKey(uninstallKeyPath);
                    if (uninstallKey == null) continue;

                    foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                    {
                        try
                        {
                            using var programKey = uninstallKey.OpenSubKey(subKeyName);
                            if (programKey == null) continue;

                            var displayName = programKey.GetValue("DisplayName")?.ToString();
                            var installLocation = programKey.GetValue("InstallLocation")?.ToString();
                            var publisher = programKey.GetValue("Publisher")?.ToString();
                            
                            // Skip if no display name or if it's clearly not a game
                            if (string.IsNullOrEmpty(displayName) || !IsLikelyGame(displayName, publisher))
                                continue;

                            // Skip if we can't find the install location
                            if (string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation))
                                continue;

                            var executable = FindGameExecutable(installLocation, displayName);
                            var gameInfo = new DirectoryInfo(installLocation);
                            
                            games.Add(new Game
                            {
                                Name = displayName,
                                Platform = "Installed Program",
                                InstallPath = installLocation,
                                Executable = executable,
                                InstallDate = gameInfo.CreationTime,
                                SizeMB = await GetDirectorySizeAsync(installLocation) / (1024 * 1024)
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error reading registry entry {subKeyName}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error accessing registry key {uninstallKeyPath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error discovering registry games: {ex.Message}");
        }

        return games;
    }

    private static string? GetSteamPath()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam") ??
                           Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            
            return key?.GetValue("InstallPath")?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string? FindGameExecutable(string gameDirectory, string gameName)
    {
        try
        {
            var exeFiles = Directory.GetFiles(gameDirectory, "*.exe", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).StartsWith("Unins", StringComparison.OrdinalIgnoreCase))
                .Where(f => !Path.GetFileName(f).Contains("redist", StringComparison.OrdinalIgnoreCase))
                .Where(f => !Path.GetFileName(f).Contains("vcredist", StringComparison.OrdinalIgnoreCase))
                .Where(f => !Path.GetFileName(f).Contains("crashreport", StringComparison.OrdinalIgnoreCase))
                .Where(f => !Path.GetFileName(f).Contains("unitycrashdhandler", StringComparison.OrdinalIgnoreCase))
                .Where(f => !Path.GetFileName(f).Contains("bssndtpt", StringComparison.OrdinalIgnoreCase))
                .Where(f => !Path.GetFileName(f).Contains("7za", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (!exeFiles.Any())
                return null;

            // Try multiple strategies to find the main game executable
            var gameWords = gameName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Strategy 1: Look for executable containing any of the main game words
            var mainExe = exeFiles.FirstOrDefault(f =>
            {
                var fileName = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                return gameWords.Any(word => fileName.Contains(word.ToLowerInvariant()) && word.Length > 2);
            });
            
            if (mainExe != null) return mainExe;
            
            // Strategy 2: For Assassin's Creed games, look for AC prefix pattern
            if (gameName.StartsWith("Assassin's Creed", StringComparison.OrdinalIgnoreCase))
            {
                var acExe = exeFiles.FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).StartsWith("AC", StringComparison.OrdinalIgnoreCase));
                if (acExe != null) return acExe;
            }
            
            // Strategy 3: Look for the largest executable (likely the main game)
            var largestExe = exeFiles.OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();
            if (largestExe != null && new FileInfo(largestExe).Length > 50 * 1024 * 1024) // > 50MB
                return largestExe;

            // Strategy 4: Return first executable that doesn't look like a utility
            return exeFiles.FirstOrDefault(f =>
            {
                var fileName = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                var utilityKeywords = new[] { "launcher", "updater", "patcher", "installer", "config", "setup", "guide", "tool" };
                return !utilityKeywords.Any(keyword => fileName.Contains(keyword));
            }) ?? exeFiles.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsNonGameDirectory(string directoryName)
    {
        var nonGameKeywords = new[] 
        { 
            "launcher", "social", "redistributable", "redist", "thirdparty", "third party",
            "connect", "client", "setup", "installer", "uninstall", "crash", "error",
            "support", "log", "temp", "cache", "data", "config", "settings",
            "nvidia", "shadowplay", "battle.net", "battlenet",
            // Xbox-specific non-game patterns
            "gamesave", "save", "saves", "skin", "dlc", "addon", "pack", "content pack",
            "soundtrack", "wallpaper", "theme", "demo", "trailer", "preview",
            // Launcher and utility patterns
            "desktop", "controller", "configs", "shared", "steamworks"
        };
        
        var lowerName = directoryName.ToLowerInvariant();
        return nonGameKeywords.Any(keyword => lowerName.Contains(keyword));
    }
    
    private static bool IsXboxAuxiliaryContent(string gameName)
    {
        var lowerName = gameName.ToLowerInvariant();
        
        // Check for Xbox/Microsoft Store auxiliary content patterns
        var auxiliaryPatterns = new[]
        {
            " skin", " dlc", " pack", " addon", " expansion", " soundtrack",
            " theme", " wallpaper", "content pack", "season pass", " bundle",
            "demo", "trailer", "preview", "beta", "alpha", "test",
            "void ", "premium ", "deluxe ", "collector", "edition",
            "cosmetic", "weapon pack", "character pack"
        };
        
        // Special case: if it's clearly auxiliary content based on patterns
        return auxiliaryPatterns.Any(pattern => lowerName.Contains(pattern)) ||
               // Or if it's a very specific pattern like "GameName Something Skin/DLC"
               (lowerName.Split(' ').Length > 2 && 
                (lowerName.EndsWith(" skin") || lowerName.EndsWith(" dlc") || 
                 lowerName.EndsWith(" pack") || lowerName.EndsWith(" addon")));
    }
    
    private static bool IsLauncherApplication(string gameName)
    {
        var lowerName = gameName.ToLowerInvariant();
        
        // Specific launcher applications to exclude - use exact matches or word boundaries
        var exactLauncherNames = new[]
        {
            "ea desktop", "ubisoft connect", "steam controller configs", "steamworks shared",
            "battle.net", "epic games launcher", "gog galaxy", "origin",
            "nvidia geforce experience", "nvidia shadowplay", "rockstar games launcher"
        };
        
        // Check for exact matches first
        if (exactLauncherNames.Any(launcher => lowerName.Equals(launcher)))
            return true;
            
        // Check for launcher names that should only match as whole words
        // Split into words and check for exact word matches
        var words = lowerName.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
        var singleWordLaunchers = new[] { "origin" }; // Only match "origin" as a standalone word
        
        return singleWordLaunchers.Any(launcher => words.Contains(launcher)) ||
               exactLauncherNames.Any(launcher => launcher.Contains(" ") && lowerName.Contains(launcher));
    }

    private static async Task<List<Game>> DiscoverBattleNetGames()
    {
        return await Task.Run(() =>
        {
            var games = new List<Game>();
            
            try
            {
                // Check Battle.net installation path
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Blizzard Entertainment\Battle.net");
                var battleNetPath = key?.GetValue("InstallPath") as string;
                
                if (string.IsNullOrEmpty(battleNetPath) || !Directory.Exists(battleNetPath))
                {
                    // Try alternative paths
                    var altPaths = new[]
                    {
                        @"C:\Program Files (x86)\Battle.net",
                        @"C:\Program Files\Battle.net"
                    };
                    
                    battleNetPath = altPaths.FirstOrDefault(Directory.Exists);
                }
                
                if (string.IsNullOrEmpty(battleNetPath))
                    return games;
                
                // Check for game installations in registry
                var gameKeys = new[]
                {
                    @"SOFTWARE\WOW6432Node\Blizzard Entertainment\World of Warcraft",
                    @"SOFTWARE\WOW6432Node\Blizzard Entertainment\Overwatch",
                    @"SOFTWARE\WOW6432Node\Blizzard Entertainment\Diablo IV",
                    @"SOFTWARE\WOW6432Node\Blizzard Entertainment\Diablo III",
                    @"SOFTWARE\WOW6432Node\Blizzard Entertainment\Diablo II Resurrected",
                    @"SOFTWARE\WOW6432Node\Blizzard Entertainment\StarCraft II",
                    @"SOFTWARE\WOW6432Node\Blizzard Entertainment\Hearthstone",
                    @"SOFTWARE\WOW6432Node\Blizzard Entertainment\Call of Duty"
                };
                
                foreach (var gameKeyPath in gameKeys)
                {
                    try
                    {
                        using var gameKey = Registry.LocalMachine.OpenSubKey(gameKeyPath);
                        var installPath = gameKey?.GetValue("InstallPath") as string;
                        
                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        {
                            var gameName = Path.GetFileName(installPath);
                            if (string.IsNullOrEmpty(gameName))
                            {
                                gameName = gameKeyPath.Split('\\').Last();
                            }
                            
                            var executable = FindGameExecutable(installPath, gameName);
                            var gameInfo = new DirectoryInfo(installPath);
                            
                            games.Add(new Game
                            {
                                Name = gameName,
                                Platform = "Battle.net",
                                InstallPath = installPath,
                                Executable = executable,
                                InstallDate = gameInfo.CreationTime,
                                SizeMB = GetDirectorySizeSync(installPath) / (1024 * 1024),
                                LastPlayed = null,
                                PlayTimeHours = null
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error reading Battle.net game registry {gameKeyPath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error discovering Battle.net games: {ex.Message}");
            }
            
            return games;
        });
    }

    private static async Task<List<Game>> DiscoverEAGames()
    {
        return await Task.Run(() =>
        {
            var games = new List<Game>();
            
            try
            {
                // Check EA Desktop and Origin installations
                var eaPaths = new[]
                {
                    @"C:\Program Files\Electronic Arts",
                    @"C:\Program Files (x86)\Electronic Arts",
                    @"C:\Program Files\Origin Games",
                    @"C:\Program Files (x86)\Origin Games",
                    @"C:\Program Files\EA Games",
                    @"C:\Program Files (x86)\EA Games"
                };
                
                foreach (var eaPath in eaPaths.Where(Directory.Exists))
                {
                    try
                    {
                        var gameDirectories = Directory.GetDirectories(eaPath)
                            .Where(dir => !IsNonGameDirectory(Path.GetFileName(dir)))
                            .ToArray();
                        
                        foreach (var gameDir in gameDirectories)
                        {
                            var gameName = Path.GetFileName(gameDir);
                            var executable = FindGameExecutable(gameDir, gameName);
                            var gameInfo = new DirectoryInfo(gameDir);
                            
                            // Exclude EA Desktop launcher
                            if (!IsLauncherApplication(gameName))
                            {
                                games.Add(new Game
                                {
                                    Name = gameName,
                                    Platform = "EA Games",
                                    InstallPath = gameDir,
                                    Executable = executable,
                                    InstallDate = gameInfo.CreationTime,
                                    SizeMB = GetDirectorySizeSync(gameDir) / (1024 * 1024),
                                    LastPlayed = null,
                                    PlayTimeHours = null
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error scanning EA directory {eaPath}: {ex.Message}");
                    }
                }
                
                // Also check EA games in registry
                try
                {
                    using var uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                    if (uninstallKey != null)
                    {
                        foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                        {
                            try
                            {
                                using var gameKey = uninstallKey.OpenSubKey(subKeyName);
                                var displayName = gameKey?.GetValue("DisplayName") as string;
                                var publisher = gameKey?.GetValue("Publisher") as string;
                                var installLocation = gameKey?.GetValue("InstallLocation") as string;
                                
                                if (!string.IsNullOrEmpty(displayName) && 
                                    !string.IsNullOrEmpty(publisher) && 
                                    publisher.ToLowerInvariant().Contains("electronic arts") &&
                                    !string.IsNullOrEmpty(installLocation) && 
                                    Directory.Exists(installLocation) &&
                                    !IsNonGameDirectory(displayName))
                                {
                                    var executable = FindGameExecutable(installLocation, displayName);
                                    var gameInfo = new DirectoryInfo(installLocation);
                                    
                                    // Avoid duplicates
                                    if (!games.Any(g => g.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        games.Add(new Game
                                        {
                                            Name = displayName,
                                            Platform = "EA Games",
                                            InstallPath = installLocation,
                                            Executable = executable,
                                            InstallDate = gameInfo.CreationTime,
                                            SizeMB = GetDirectorySizeSync(installLocation) / (1024 * 1024),
                                            LastPlayed = null,
                                            PlayTimeHours = null
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"Error reading EA registry entry {subKeyName}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error scanning EA registry: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error discovering EA games: {ex.Message}");
            }
            
            return games;
        });
    }

    private static bool IsLikelyGame(string name, string? publisher = null)
    {
        // Game-related keywords
        var gameKeywords = new[] { "game", "play", "adventure", "action", "rpg", "strategy", "simulation", "racing", "sports", "puzzle" };
        var gamePublishers = new[] { "valve", "epic", "ubisoft", "ea", "activision", "blizzard", "steam", "xbox", "microsoft games", "bethesda", "rockstar" };
        
        // Non-game keywords to filter out
        var nonGameKeywords = new[] { "runtime", "redistributable", "driver", "framework", "service", "tool", "utility", "update", "patch", "launcher" };

        var lowerName = name.ToLowerInvariant();
        var lowerPublisher = publisher?.ToLowerInvariant() ?? "";

        // Check if it's clearly not a game
        if (nonGameKeywords.Any(keyword => lowerName.Contains(keyword)))
            return false;

        // Check if it has game-related keywords or is from a known game publisher
        return gameKeywords.Any(keyword => lowerName.Contains(keyword)) ||
               gamePublishers.Any(pub => lowerPublisher.Contains(pub));
    }

    private static async Task<long> GetDirectorySizeAsync(string path)
    {
        try
        {
            return await Task.Run(() =>
            {
                var directory = new DirectoryInfo(path);
                return directory.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            });
        }
        catch
        {
            return 0;
        }
    }

    private static long GetDirectorySizeSync(string path)
    {
        try
        {
            var directory = new DirectoryInfo(path);
            return directory.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }

    private static async Task<(double?, DateTime?)> GetSteamPlaytimeAsync(string appId)
    {
        try
        {
            if (string.IsNullOrEmpty(appId))
                return (null, null);

            var steamPath = GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
                return (null, null);

            // First, try to find the current user's Steam ID from loginusers.vdf
            var loginUsersFile = Path.Combine(steamPath, "config", "loginusers.vdf");
            var currentUserId = await GetCurrentSteamUserId(loginUsersFile);

            var userdataPath = Path.Combine(steamPath, "userdata");
            if (!Directory.Exists(userdataPath))
                return (null, null);

            // Look for user directories (either the specific user or all users)
            var userDirs = Directory.GetDirectories(userdataPath);
            
            // Prioritize the current user's directory if we found their ID
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var currentUserDir = Path.Combine(userdataPath, currentUserId);
                if (Directory.Exists(currentUserDir))
                {
                    var result = await TryGetPlaytimeFromUserDir(currentUserDir, appId);
                    if (result.Item1.HasValue || result.Item2.HasValue)
                        return result;
                }
            }

            // If we didn't find data for the current user, try all user directories
            foreach (var userDir in userDirs)
            {
                var result = await TryGetPlaytimeFromUserDir(userDir, appId);
                if (result.Item1.HasValue || result.Item2.HasValue)
                    return result;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting Steam playtime for {appId}: {ex.Message}");
        }

        return (null, null);
    }

    private static async Task<string?> GetCurrentSteamUserId(string loginUsersFile)
    {
        try
        {
            if (!File.Exists(loginUsersFile))
                return null;

            var content = await File.ReadAllTextAsync(loginUsersFile);
            // Look for the most recent login (MostRecent = "1")
            var userMatch = Regex.Match(content, @"""(\d+)""\s*\{[^}]*""MostRecent""\s*""1""", RegexOptions.Singleline);
            return userMatch.Success ? userMatch.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<(double?, DateTime?)> TryGetPlaytimeFromUserDir(string userDir, string appId)
    {
        try
        {
            double? playtimeHours = null;
            DateTime? lastPlayed = null;

            // Method 1: Check localconfig.vdf for playtime and last played
            var localConfigFile = Path.Combine(userDir, "config", "localconfig.vdf");
            if (File.Exists(localConfigFile))
            {
                var content = await File.ReadAllTextAsync(localConfigFile);
                
                // Look for the app section in Software\Valve\Steam\Apps\{appId}
                var appSectionPattern = $@"""{appId}""\s*\{{([^}}]*)}}";
                var appMatch = Regex.Match(content, appSectionPattern, RegexOptions.Singleline);
                
                if (appMatch.Success)
                {
                    var appSection = appMatch.Groups[1].Value;
                    
                    // Extract playtime (in minutes)
                    var playtimeMatch = Regex.Match(appSection, @"""Playtime""\s*""(\d+)""");
                    if (playtimeMatch.Success)
                    {
                        var totalMinutes = int.Parse(playtimeMatch.Groups[1].Value);
                        if (totalMinutes > 0)
                            playtimeHours = totalMinutes / 60.0;
                    }
                    
                    // Extract last played timestamp
                    var lastPlayedMatch = Regex.Match(appSection, @"""LastPlayed""\s*""(\d+)""");
                    if (lastPlayedMatch.Success)
                    {
                        var timestamp = long.Parse(lastPlayedMatch.Groups[1].Value);
                        if (timestamp > 0)
                            lastPlayed = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    }
                }
            }

            // Method 2: Also check sharedconfig.vdf as a fallback
            if (!playtimeHours.HasValue)
            {
                var sharedConfigFile = Path.Combine(userDir, "7", "remote", "sharedconfig.vdf");
                if (File.Exists(sharedConfigFile))
                {
                    var content = await File.ReadAllTextAsync(sharedConfigFile);
                    var playtimeMatch = Regex.Match(content, $@"""{appId}""\s*\{{\s*""Playtime2wks""\s*""(\d+)""\s*""Playtime""\s*""(\d+)""");
                    if (playtimeMatch.Success)
                    {
                        var totalMinutes = int.Parse(playtimeMatch.Groups[2].Value);
                        if (totalMinutes > 0)
                            playtimeHours = totalMinutes / 60.0;
                    }
                }
            }

            return (playtimeHours, lastPlayed);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading Steam user data from {userDir}: {ex.Message}");
            return (null, null);
        }
    }

    private static async Task<(double?, DateTime?)> GetEpicPlaytimeAsync(string gameName, string installPath)
    {
        try
        {
            // Epic Games stores playtime in different locations depending on the game
            // Check for common save game locations that might contain playtime data
            var epicDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicGamesLauncher", "Saved");
            if (Directory.Exists(epicDataPath))
            {
                var configPath = Path.Combine(epicDataPath, "Config", "Windows", "GameUserSettings.ini");
                if (File.Exists(configPath))
                {
                    var content = await File.ReadAllTextAsync(configPath);
                    // Epic doesn't store playtime in easily accessible format, return null for now
                }
            }

            // Check executable last modified time as a proxy for last played
            if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
            {
                var exeFiles = Directory.GetFiles(installPath, "*.exe", SearchOption.AllDirectories);
                var mainExe = exeFiles.FirstOrDefault(f => !Path.GetFileName(f).ToLower().Contains("unins"));
                if (mainExe != null)
                {
                    var lastWrite = File.GetLastWriteTime(mainExe);
                    var installDate = Directory.GetCreationTime(installPath);
                    // If exe was modified after install, it might indicate recent play
                    if (lastWrite > installDate.AddDays(1))
                    {
                        return (null, lastWrite);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting Epic playtime for {gameName}: {ex.Message}");
        }

        return (null, null);
    }

    private static Task<(double?, DateTime?)> GetGogPlaytimeAsync(string gameName, string installPath)
    {
        try
        {
            // GOG Galaxy stores playtime in database files
            var gogDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GOG.com", "Galaxy");
            if (Directory.Exists(gogDataPath))
            {
                // GOG uses SQLite databases which would require additional dependencies to read
                // For now, return null - could be enhanced with SQLite support
            }

            // Use similar logic as Epic - check for recent executable modifications
            if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
            {
                var exeFiles = Directory.GetFiles(installPath, "*.exe", SearchOption.AllDirectories);
                var mainExe = exeFiles.FirstOrDefault(f => !Path.GetFileName(f).ToLower().Contains("unins"));
                if (mainExe != null)
                {
                    var lastAccess = File.GetLastAccessTime(mainExe);
                    var installDate = Directory.GetCreationTime(installPath);
                    if (lastAccess > installDate.AddDays(1))
                    {
                        return Task.FromResult<(double?, DateTime?)>((null, lastAccess));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting GOG playtime for {gameName}: {ex.Message}");
        }

        return Task.FromResult<(double?, DateTime?)>((null, null));
    }

    private static async Task<(double?, DateTime?)> GetUbisoftPlaytimeAsync(string gameName, string installPath)
    {
        try
        {
            // Ubisoft Connect stores playtime and game statistics in multiple locations:
            // 1. Local AppData for the launcher
            // 2. Ubisoft Connect database files
            // 3. Game save files and logs

            var ubisoftDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ubisoft Game Launcher");
            if (!Directory.Exists(ubisoftDataPath))
            {
                // Try alternative path
                ubisoftDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ubisoft");
            }

            DateTime? lastPlayed = null;
            double? playtimeHours = null;

            // Method 1: Check Ubisoft Connect database files for playtime
            if (Directory.Exists(ubisoftDataPath))
            {
                try
                {
                    // Look for database files that might contain playtime data
                    var dbFiles = Directory.GetFiles(ubisoftDataPath, "*.db", SearchOption.AllDirectories);
                    var logFiles = Directory.GetFiles(ubisoftDataPath, "*.log", SearchOption.AllDirectories);
                    
                    // Ubisoft Connect uses SQLite databases, but without external dependencies we can't read them directly
                    // Look for text-based log files or configuration files instead
                    var configFiles = Directory.GetFiles(ubisoftDataPath, "*.json", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(ubisoftDataPath, "*.cfg", SearchOption.AllDirectories))
                        .Concat(Directory.GetFiles(ubisoftDataPath, "*.ini", SearchOption.AllDirectories));

                    foreach (var configFile in configFiles)
                    {
                        try
                        {
                            var content = await File.ReadAllTextAsync(configFile);
                            // Look for game name and potential playtime data in JSON or config format
                            if (content.Contains(gameName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Try to extract playtime from various possible formats
                                var playtimePattern = @"(?i)(?:playtime|time_played|total_time)[\s""':=]*(\d+)";
                                var playtimeMatch = Regex.Match(content, playtimePattern);
                                if (playtimeMatch.Success && double.TryParse(playtimeMatch.Groups[1].Value, out var time))
                                {
                                    // Time could be in seconds, minutes, or hours - heuristic determination
                                    if (time > 10000) // Likely seconds
                                        playtimeHours = time / 3600.0;
                                    else if (time > 600) // Likely minutes
                                        playtimeHours = time / 60.0;
                                    else // Likely hours
                                        playtimeHours = time;
                                }

                                // Try to extract last played date
                                var datePattern = @"(?i)(?:last_played|lastplayed|date)[\s""':=]*[""']?(\d{4}-\d{2}-\d{2}|\d{10,13})[""']?";
                                var dateMatch = Regex.Match(content, datePattern);
                                if (dateMatch.Success)
                                {
                                    var dateStr = dateMatch.Groups[1].Value;
                                    if (long.TryParse(dateStr, out var timestamp) && timestamp > 1000000000)
                                    {
                                        // Unix timestamp
                                        if (timestamp > 10000000000) // Milliseconds
                                            timestamp /= 1000;
                                        lastPlayed = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                                    }
                                    else if (DateTime.TryParse(dateStr, out var date))
                                    {
                                        lastPlayed = date;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error reading Ubisoft config file {configFile}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error scanning Ubisoft data directory: {ex.Message}");
                }
            }

            // Method 2: Check game installation directory for save files or logs
            if (!playtimeHours.HasValue && !string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
            {
                try
                {
                    // Look for save files or log files that might contain playtime
                    var saveFiles = Directory.GetFiles(installPath, "*.sav", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(installPath, "*.save", SearchOption.AllDirectories))
                        .Concat(Directory.GetFiles(installPath, "*.log", SearchOption.AllDirectories))
                        .Concat(Directory.GetFiles(installPath, "*.txt", SearchOption.AllDirectories));

                    foreach (var saveFile in saveFiles.Take(10)) // Limit to first 10 files to avoid performance issues
                    {
                        try
                        {
                            if (new FileInfo(saveFile).Length > 1024 * 1024) // Skip files larger than 1MB
                                continue;

                            var content = await File.ReadAllTextAsync(saveFile);
                            var playtimePattern = @"(?i)(?:playtime|time_played|total_time|hours_played)[\s""':=]*(\d+\.?\d*)";
                            var match = Regex.Match(content, playtimePattern);
                            if (match.Success && double.TryParse(match.Groups[1].Value, out var time))
                            {
                                if (time > 10000) // Likely seconds
                                    playtimeHours = time / 3600.0;
                                else if (time > 600) // Likely minutes
                                    playtimeHours = time / 60.0;
                                else // Likely hours
                                    playtimeHours = time;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error reading save file {saveFile}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error scanning game directory for saves: {ex.Message}");
                }
            }

            // Method 3: Fallback - check executable modification times as a proxy for activity
            if (!lastPlayed.HasValue && !string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
            {
                try
                {
                    var exeFiles = Directory.GetFiles(installPath, "*.exe", SearchOption.AllDirectories);
                    var mainExe = exeFiles.FirstOrDefault(f => !Path.GetFileName(f).ToLower().Contains("unins") && 
                                                              !Path.GetFileName(f).ToLower().Contains("setup"));
                    if (mainExe != null)
                    {
                        var lastAccess = File.GetLastAccessTime(mainExe);
                        var installDate = Directory.GetCreationTime(installPath);
                        if (lastAccess > installDate.AddDays(1))
                        {
                            lastPlayed = lastAccess;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error checking executable times: {ex.Message}");
                }
            }

            return (playtimeHours, lastPlayed);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting Ubisoft playtime for {gameName}: {ex.Message}");
        }

        return (null, null);
    }

    private static Task<(double?, DateTime?)> GetRegistryPlaytimeAsync(string gameName, string installPath)
    {
        try
        {
            // Some games store playtime in registry
            // This is a generic fallback method
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + gameName.Replace(" ", ""));
            if (key != null)
            {
                var playtime = key.GetValue("PlayTime");
                if (playtime != null && double.TryParse(playtime.ToString(), out var hours))
                {
                    return Task.FromResult<(double?, DateTime?)>((hours, null));
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting registry playtime for {gameName}: {ex.Message}");
        }

        return Task.FromResult<(double?, DateTime?)>((null, null));
    }
}

/// <summary>
/// Represents information about an installed game
/// </summary>
public class Game
{
    public string Name { get; set; } = "";
    public string Platform { get; set; } = "";
    public string InstallPath { get; set; } = "";
    public string? Executable { get; set; }
    public DateTime? InstallDate { get; set; }
    public long SizeMB { get; set; }
    public DateTime? LastPlayed { get; set; }
    public double? PlayTimeHours { get; set; }
}

/// <summary>
/// Response classes for JSON serialization
/// </summary>
public class ErrorResponse
{
    public string error { get; set; } = "";
}

public class GameDiscoveryResult
{
    public int total_games { get; set; }
    public Dictionary<string, int> games_by_platform { get; set; } = new();
    public List<GameInfo> games { get; set; } = new();
}

public class GameInfo
{
    public string name { get; set; } = "";
    public string platform { get; set; } = "";
    public string install_path { get; set; } = "";
    public string? executable { get; set; }
    public string? install_date { get; set; }
    public long size_mb { get; set; }
    public string? last_played { get; set; }
    public double? play_time_hours { get; set; }
}

public class LaunchResponse
{
    public bool success { get; set; }
    public string message { get; set; } = "";
    public string? executable { get; set; }
}

/// <summary>
/// JSON serialization context for source generation
/// </summary>
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(GameDiscoveryResult))]
[JsonSerializable(typeof(GameInfo[]))]
[JsonSerializable(typeof(LaunchResponse))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class GameJsonContext : JsonSerializerContext
{
}