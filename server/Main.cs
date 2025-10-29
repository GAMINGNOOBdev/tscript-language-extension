namespace TScriptLanguageServer;

using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;
using TScriptLanguageServer.Language;

public class Program
{
    // public static void DebuggingMain(string[] args)
    // {
    //     string? currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    //     string logFileName = "tscript.language.server.log";
    //     string logFilePath = Path.Combine(currentDir ?? "", logFileName);
    //     Logging.OutputStream = new(logFilePath);
    //     Logging.DebugMessagesEnabled = true;

    //     Tokenizer tokenizer = new();
    //     string buffer = File.ReadAllText("audio.tscript");
    //     tokenizer.ParseFile(buffer);

    //     TokenParser p = new();
    //     p.Parse(tokenizer);

    //     Logging.OutputStream.Close();
    // }

    public static async Task Main(string[] args)
    {

        string? currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string logFileName = "tscript.language.server.log";
        string logFilePath = Path.Combine(currentDir ?? "", logFileName);
        Logging.DebugMessagesEnabled = true;
        if (args.Length >= 1)
            Specifications.StdLibPath = args[0];

        if (args.Length == 3)
        {
            _ = bool.TryParse(args[1], out Logging.DebugMessagesEnabled);
            if (!string.IsNullOrEmpty(args[2]))
                logFilePath = args[2];
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
                .WithHandler<TooltipHandler>()
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