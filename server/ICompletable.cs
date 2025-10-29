namespace TScriptLanguageServer;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public interface ICompletable
{
    public string Name { get; }
    public string? TypeName { get; }
    public string? Description { get; }
    public CompletionItemKind CompletionKind { get; }
}
