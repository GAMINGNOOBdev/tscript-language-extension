import * as path from 'path';
import { workspace, ExtensionContext, commands, window } from 'vscode';
import {
    ServerOptions,
    TransportKind,
    SettingMonitor,
    LanguageClient,
    InitializeParams,
    LanguageClientOptions,
} from 'vscode-languageclient/node';


interface ICommandResult {
    dotnetPath: string;
}

let client : LanguageClient;
const REQUIRED_DOTNET_VERSION = "8.0";

async function getDotnetPath() : Promise<string> {
    let dotnetPath = await commands.executeCommand<ICommandResult>(
        'dotnet.acquire',
        {
            version: REQUIRED_DOTNET_VERSION,
            requestingExtensionId: 'GAMINGNOOBdev.tscript-programming-language',
            acquisitionContext: "runtime"
        }
    );

    if (!dotnetPath || !dotnetPath?.dotnetPath)
    {
        window.showErrorMessage(`Failed to install or locate .NET Runtime ${REQUIRED_DOTNET_VERSION}. Please install it manually or check your extension logs.`);
        return "dotnet";
    }

    return dotnetPath.dotnetPath;
}

export async function activate(context: ExtensionContext)
{
    let serverCommand = await getDotnetPath();
    let serverPath = context.asAbsolutePath(
        path.join('server', 'bin', 'Debug', 'net8.0', 'TScriptLanguageServer.dll')
    );
    let config = workspace.getConfiguration('tscript');
    let logFileLocation : string = config.get("log_file_location", "");
    let enableDebugLog : boolean = config.get("enable_debug_log", false);

    let serverOptions: ServerOptions = {
        run: { command: serverCommand, args: [serverPath, context.asAbsolutePath("stdlib"), enableDebugLog ? "true" : "false", logFileLocation] },
        debug: { command: serverCommand, args: [serverPath, context.asAbsolutePath("stdlib"), enableDebugLog ? "true" : "false", logFileLocation] }
    };

    let clientOptions: LanguageClientOptions = {
        documentSelector: [
            {
                scheme: 'file',
                language: 'tscript'
            }
        ],
        synchronize: {
            fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
        }
    };

    client = new LanguageClient(
        'tscriptLanguageServer',
        'TScript Language Server',
        serverOptions,
        clientOptions
    );
    client.start();
}

export function deactivate(): Thenable<void> | undefined
{
    if (!client)
        return undefined;

    return client.stop();
}
