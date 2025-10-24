using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace TScriptLanguageServer;

public interface ICompletable
{
    string Name { get; }
    CompletionItemKind CompletionKind { get; }
}
