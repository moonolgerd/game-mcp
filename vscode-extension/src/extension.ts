import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import * as os from 'os';

// Relative path to the bundled game-mcp executable inside the extension
const GAME_MCP_BIN_WINDOWS = ['bin', 'win-x64', 'game-mcp.exe'];

/**
 * Registers the Game Discovery MCP server definition provider with VS Code.
 * VS Code will automatically start the server process when Copilot agent mode
 * needs it, and shut it down when the extension deactivates.
 */
function registerMcpServerDefinitionProvider(context: vscode.ExtensionContext): void {
    const provider = vscode.lm.registerMcpServerDefinitionProvider(
        'game-mcp.server',
        {
            provideMcpServerDefinitions: async () => {
                // Resolve the path to the bundled executable
                const exePath = vscode.Uri.joinPath(
                    context.extensionUri,
                    ...GAME_MCP_BIN_WINDOWS
                ).fsPath;

                if (!fs.existsSync(exePath)) {
                    vscode.window.showWarningMessage(
                        `Game Discovery MCP: executable not found at ${exePath}. ` +
                        `Please rebuild the extension or reinstall it.`
                    );
                    return [];
                }

                return [
                    new vscode.McpStdioServerDefinition(
                        // Display name shown in the Copilot MCP UI
                        'Game Discovery MCP',
                        // Binary to invoke
                        exePath,
                        // No additional arguments needed – the server uses stdio
                        [],
                        // No extra environment variables required
                        {},
                        // Version string reported to MCP clients
                        context.extension.packageJSON.version as string
                    )
                ];
            },

            resolveMcpServerDefinition: async (server: vscode.McpServerDefinition) => {
                // No additional setup needed; return the definition as-is
                return server;
            }
        }
    );

    context.subscriptions.push(provider);
}

export function activate(context: vscode.ExtensionContext): void {
    if (os.platform() !== 'win32') {
        vscode.window.showInformationMessage(
            'Game Discovery MCP is only supported on Windows.'
        );
        return;
    }

    registerMcpServerDefinitionProvider(context);
}

export function deactivate(): void {
    // Cleanup is handled automatically via context.subscriptions
}
