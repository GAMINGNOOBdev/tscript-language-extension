namespace TScriptLanguageServer.Language.Tokens;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public class VariableToken(int line = int.MinValue, int startpos = int.MinValue, string name = "") : Token(line, startpos, name, CompletionItemKind.Variable)
{
    public TokenParser? ParserInstance = null;
    public NamespaceToken? ParentNamespace = null;
    public ClassToken? Parent = null;
    public SourceToken VariableType = new();

    public override string? TypeName => VariableType.Name;
    public override string? Description => $"{TypeName} {Name} {GetOriginString()}";
    public string GetOriginString()
    {
        if (ParentNamespace != null)
            return $"( namespace {ParentNamespace.GetFullPath()} )";

        if (Parent != null)
            return $"( class {Parent.GetFullPath()} )";

        return "";
    }
}
