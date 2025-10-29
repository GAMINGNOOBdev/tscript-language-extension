namespace TScriptLanguageServer.Language.Tokens;

using System.Security.Cryptography;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public class FunctionToken(int line = int.MinValue, int startpos = int.MinValue, string name = "") : Token(line, startpos, name, CompletionItemKind.Function)
{
    public TokenParser? ParserInstance = null;
    public List<VariableToken> Parameters = [];
    public List<VariableToken> Variables = [];
    public NamespaceToken? ParentNamespace = null;
    public ClassToken? Parent = null;

    private SourceToken Start = new(), End = new();

    public override string? TypeName => "function";
    public override string? Description => $"function {GetOriginString()}{Name}()";
    public string GetOriginString()
    {
        if (ParentNamespace != null)
            return $"{ParentNamespace.GetFullPath()}.";

        if (Parent != null)
            return $"{Parent.GetFullPath()}.";

        return "";
    }

    public ICompletable GetCompletion(SourceToken token)
    {
        if (token.Line < Start.Line || token.Line > End.Line)
            return token;

        ICompletable? result = Parent?.GetCompletion(token);
        if (result != token && result != null)
            return result;

        foreach(Token element in Parameters)
            if (element.Name == token.Name)
                return element;

        foreach(Token element in Variables)
            if (element.Name == token.Name)
                return element;

        return token;
    }

    public List<ICompletable> GetCompletions(SourceToken token)
    {
        if (token.Line < Start.Line || token.Line > End.Line)
            return [];

        List<ICompletable> result = [];

        foreach(Token element in Parameters)
            if (element.Name.Contains(token.Name))
                result.Add(element);

        foreach(Token element in Variables)
            if (element.Name.Contains(token.Name))
                result.Add(element);

        return result;
    }

    public void Parse(SourceTokenIterator iter)
    {
        SourceToken token = iter.Current;
        if (token.Type != SourceTokenType.PARENTHESISOPEN)
            return;

        ParseParameters(iter);

        Start = token = iter.Current;
        if (token.Type != SourceTokenType.CURLYBRACKETOPEN)
            return;

        SourceTokenIterator subIter = iter.SubTokens(SourceTokenType.CURLYBRACKETOPEN, SourceTokenType.CURLYBRACKETCLOSE, out bool success);
        if (!success)
        {
            Logging.LogError("Failed to parse function: couldnt find valid depths/curly brackets");
            return;
        }

        while (subIter.HasNext)
        {
            if (token.Name.StartsWith("#type="))
            {
                if (ParserInstance != null)
                    ParserInstance.NextVariableType = new SourceToken(SourceTokenType.TEXT, token.Name[6..]);
                token = subIter.Next;
                continue;
            }

            if (token.IsInternalType || ParserInstance?.IsStruct(token) == true)
            {
                SourceToken typeToken = new();
                typeToken.Copy(token);
                if (ParserInstance?.NextVariableType != null)
                {
                    typeToken = ParserInstance.NextVariableType;
                    ParserInstance.NextVariableType = null;
                }
                token = subIter.Next;
                if (token.Type != SourceTokenType.TEXT)
                    continue;
                SourceToken next = subIter.Current;
                if (next.Type != SourceTokenType.EQUALSIGN && next.Type != SourceTokenType.SEMICOLON)
                    continue;
                Variables.Add(new VariableToken(token.Line, token.StartPos, token.Name){
                    VariableType = typeToken,
                    ParserInstance = ParserInstance
                });
            }

            token = subIter.Next;
        }
        End = token;
    }

    private void ParseParameters(SourceTokenIterator iter)
    {
        SourceTokenIterator subIter = iter.SubTokens(SourceTokenType.PARENTHESISOPEN, SourceTokenType.PARENTHESISCLOSE, out bool success);
        if (!success)
        {
            Logging.LogError("Failed to parse function: couldnt find valid depths/curly brackets");
            return;
        }
        SourceToken token = new();
        while (subIter.HasNext && token.Type != SourceTokenType.PARENTHESISCLOSE)
        {
            AddParameter(subIter, token);
            token = subIter.Next;
        }
        if (iter.Current.Type == SourceTokenType.PARENTHESISCLOSE)
            _ = iter.Next;
    }

    private void AddParameter(SourceTokenIterator iter, SourceToken nameToken)
    {
        SourceToken next = iter.Current;
        if (next.Type == SourceTokenType.EQUALSIGN)
        {
            while (iter.HasNext && next.Type != SourceTokenType.PARENTHESISCLOSE && next.Type != SourceTokenType.COMMA)
                next = iter.Next;
        }
        if (next.Type != SourceTokenType.PARENTHESISCLOSE && next.Type != SourceTokenType.COMMA)
            return;

        Parameters.Add(new VariableToken(nameToken.Line, nameToken.StartPos, nameToken.Name){
            VariableType = iter.Last
        });
    }
}
