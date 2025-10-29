namespace TScriptLanguageServer.Language.Tokens;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public class ClassToken(int line = int.MinValue, int startpos = int.MinValue, string name = "") : Token(line, startpos, name, CompletionItemKind.Struct)
{
    public TokenParser? ParserInstance = null;
    public NamespaceToken? ParentNamespace = null;
    public List<VariableToken> Variables = [];
    public List<FunctionToken> Functions = [];

    public override string? TypeName => "class";
    public override string? Description => $"class {GetFullPath()}";
    public string GetFullPath() => ParentNamespace != null ? $"{ParentNamespace.GetFullPath()}.{Name}" : Name;

    private Visibility CurrentVisibility = Visibility.Private;
    private SourceToken Start = new(), End = new();
    private bool _nextTokenIsStatic = false;
    private bool NextTokenIsStatic {
        get {
            bool value = _nextTokenIsStatic;
            _nextTokenIsStatic = false;
            return value;
        }
        set => _nextTokenIsStatic = value;
    }

    public void Parse(SourceTokenIterator iter)
    {
        SourceToken token = iter.Current;
        if (token.Type != SourceTokenType.CURLYBRACKETOPEN)
            return;

        SourceTokenIterator subIter = iter.SubTokens(SourceTokenType.CURLYBRACKETOPEN, SourceTokenType.CURLYBRACKETCLOSE, out bool success);
        if (!success)
        {
            Logging.LogError("Failed to parse class: couldnt find valid depths/curly brackets");
            return;
        }

        Start = token;
        while (subIter.HasNext)
        {
            Logging.LogInfo($"curr token while parsing class: {token}");
            if (token.Name.StartsWith("#type="))
            {
                if (ParserInstance != null)
                    ParserInstance.NextVariableType = new SourceToken(SourceTokenType.TEXT, token.Name[6..]);
                token = subIter.Next;
                continue;
            }

            if (token.IsAccessModifier)
            {
                CurrentVisibility = GetVisibility(token.Name);
                token = subIter.Next;
                if (token.Type == SourceTokenType.COLON)
                    token = subIter.Next;
                continue;
            }

            if (token.Name == "static")
            {
                NextTokenIsStatic = true;
                token = subIter.Next;
            }

            if (token.IsInternalType)
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
                while (subIter.HasNext)
                {
                    if (next.Type == SourceTokenType.SEMICOLON)
                        break;

                    next = subIter.Next;
                }
                Variables.Add(new VariableToken(token.Line, token.StartPos, token.Name){
                    VariableType = typeToken,
                    ParserInstance = ParserInstance,
                    IsStatic = NextTokenIsStatic,
                    TokenVisibility = CurrentVisibility,
                    Parent = this
                });
            }

            if (token.Name == "function")
            {
                token = subIter.Next;
                FunctionToken functionToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    IsStatic = NextTokenIsStatic,
                    TokenVisibility = CurrentVisibility,
                    Parent = this,
                };
                functionToken.Parse(subIter);
                Functions.Add(functionToken);
            }

            if (token.Name == "constructor")
            {
                FunctionToken constructorToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    TokenVisibility = CurrentVisibility,
                    Parent = this,
                };
                constructorToken.Parse(subIter);
                Functions.Add(constructorToken);
            }

            token = subIter.Next;
        }
        End = subIter.Last;
        Logging.LogDebug($"ClassToken.End = {End} | iter.Last = {subIter.Last} | iter.Current = {subIter.Current} | iter.Peek = {subIter.Peek}");
    }

    public ICompletable GetCompletion(SourceToken token)
    {
        if (token.Line < Start.Line || token.Line > End.Line)
            return token;

        foreach(Token element in Variables)
            if (element.Name == token.Name)
                return element;

        foreach(Token element in Functions)
            if (element.Name == token.Name)
                return element;

        return token;
    }

    public List<ICompletable> GetCompletions(SourceToken token, SourceTokenIterator _, bool ignorePositioning = false)
    {
        Logging.LogDebug($"class start/end {Start}/{End} token: {token} ignorePositioning: {ignorePositioning}");
        List<ICompletable> result = [];

        if (ignorePositioning)
        {
            foreach(VariableToken element in Variables)
                if (element.TokenVisibility == Visibility.Public)
                    result.Add(element);

            foreach(FunctionToken element in Functions)
                if (element.TokenVisibility == Visibility.Public)
                    result.Add(element);

            return result;
        }

        if (token.Line < Start.Line || token.Line > End.Line)
            return [];

        foreach(Token element in Variables)
            if (element.Name.Contains(token.Name))
                result.Add(element);

        foreach(Token element in Functions)
            if (element.Name.Contains(token.Name))
                result.Add(element);

        return result;
    }
}
