using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;

namespace TScriptLanguageServer;

public class Program
{
    public static async Task Main(string[] args)
    {

        string? currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string logFileName = "tscript.language.server.log";
        string logFilePath = Path.Combine(currentDir ?? "", logFileName);
        Logging.DebugMessagesEnabled = true;
        if (args.Length == 2)
        {
            _ = bool.TryParse(args[0], out Logging.DebugMessagesEnabled);
            if (!string.IsNullOrEmpty(args[1]))
                logFilePath = args[1];
        }
        Logging.OutputStream = new(logFilePath);
        for (int i = 0; i < args.Length; i++)
            Logging.LogInfo($"launch argument {i}: '{args[i]}'");

        try
        {
            var server = await LanguageServer.From(options => options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .ConfigureLogging(x => x
                    .AddLanguageProtocolLogging()
                    .SetMinimumLevel(LogLevel.Debug))
                .WithHandler<DocumentSyncHandler>()
                .WithHandler<ScriptCompletionHandler>()
                .WithServices(ConfigureServices)
            );
            await server.WaitForExit;
        }
        catch (Exception e)
        {
            Logging.LogError(e.ToString());
        }

        Logging.OutputStream.Close();
    }

    static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<BufferManager>();
    }
}