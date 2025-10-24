import * as path from 'path';
import { workspace, ExtensionContext } from 'vscode';
import {
    ServerOptions,
    TransportKind,
    SettingMonitor,
    LanguageClient,
    InitializeParams,
    LanguageClientOptions,
} from 'vscode-languageclient/node';


let client : LanguageClient;

export function activate(context: ExtensionContext)
{
    let serverCommand = 'dotnet';
    let serverPath = context.asAbsolutePath(
        path.join('server', 'bin', 'Debug', 'net8.0', 'TScriptLanguageServer.dll')
    );

    let serverOptions: ServerOptions = {
        run: { command: serverCommand, args: [serverPath] },
        debug: { command: serverCommand, args: [serverPath] }
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
