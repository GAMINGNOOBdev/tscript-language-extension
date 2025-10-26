using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace TScriptLanguageServer;

public interface ICompletable
{
    public string Name { get; }
    public string? TypeName { get; }
    public string? Description { get; }
    public CompletionItemKind CompletionKind { get; }
}
