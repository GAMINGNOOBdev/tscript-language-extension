using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;

namespace TScriptLanguageServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var server = await LanguageServer.From(options => options
            .WithInput(System.Console.OpenStandardInput())
            .WithOutput(System.Console.OpenStandardOutput())
            .ConfigureLogging(x => x
                .AddLanguageProtocolLogging()
                .SetMinimumLevel(LogLevel.Debug))
            .WithHandler<TScriptDocumentSyncHandler>()
            .WithHandler<TScriptCompletionHandler>()
            .WithServices(ConfigureServices)
        );
        await server.WaitForExit;
    }

    static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<BufferManager>();
    }
}