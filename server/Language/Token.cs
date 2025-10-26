namespace TScriptLanguageServer.Language;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public class Token(int line = int.MinValue, int startpos = int.MinValue, string name = "", CompletionItemKind kind = CompletionItemKind.Text) : ICompletable
{
    public virtual CompletionItemKind CompletionKind { get; private set; } = kind;
    public virtual string Name { get; private set; } = name;
    public virtual string? Description => Name;
    public virtual string? TypeName => Name;

    public int StartPos { get; private set; } = startpos;
    public int Line { get; private set; } = line;

    internal void Copy(Token token)
    {
        CompletionKind = token.CompletionKind;
        Name = token.Name;
        Line = token.Line;
        StartPos = token.StartPos;
    }
}
