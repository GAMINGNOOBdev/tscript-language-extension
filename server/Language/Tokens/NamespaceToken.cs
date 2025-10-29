namespace TScriptLanguageServer.Language.Tokens;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public class NamespaceToken(int line = int.MinValue, int startpos = int.MinValue, string name = "") : Token(line, startpos, name, CompletionItemKind.Struct)
{
    public TokenParser? ParserInstance = null;
    public NamespaceToken? ParentNamespace = null;
    public List<NamespaceToken> Namespaces { get; private set; } = [];
    public List<VariableToken> Variables { get; private set; } = [];
    public List<FunctionToken> Functions { get; private set; } = [];
    public List<ClassToken> Classes { get; private set; } = [];

    public override string? TypeName => "namespace";
    public override string? Description => $"namespace {GetFullPath()}";
    public override CompletionItemKind CompletionKind => CompletionItemKind.Interface;
    public string GetFullPath() => ParentNamespace != null ? $"{ParentNamespace.GetFullPath()}.{Name}" : Name;

    private SourceToken Start = new(), End = new();

    public void Parse(SourceTokenIterator iter)
    {
        SourceToken token = iter.Current;
        if (token.Type != SourceTokenType.CURLYBRACKETOPEN)
            return;

        SourceTokenIterator subIter = iter.SubTokens(SourceTokenType.CURLYBRACKETOPEN, SourceTokenType.CURLYBRACKETCLOSE, out bool success);
        if (!success)
        {
            Logging.LogError("Failed to parse namespace: couldnt find valid depths/curly brackets");
            return;
        }

        Start = token;
        while (subIter.HasNext)
        {
            if (token.Name.StartsWith("#type="))
            {
                if (ParserInstance != null)
                    ParserInstance.NextVariableType = new SourceToken(SourceTokenType.TEXT, token.Name[6..]);
                token = subIter.Next;
                continue;
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
                    Parent = null,
                    ParentNamespace = this
                });
            }

            if (token.Name == "function")
            {
                token = subIter.Next;
                FunctionToken functionToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    Parent = null,
                    ParentNamespace = this
                };
                functionToken.Parse(subIter);
                Functions.Add(functionToken);
            }

            if (token.Name == "class")
            {
                token = subIter.Next;
                ClassToken classToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    ParentNamespace = this,
                };
                classToken.Parse(subIter);
                Classes.Add(classToken);
            }

            if (token.Name == "namespace")
            {
                token = subIter.Next;
                NamespaceToken namespaceToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    ParentNamespace = this,
                };
                namespaceToken.Parse(subIter);
                Namespaces.Add(namespaceToken);
            }

            token = subIter.Next;
        }
        End = subIter.Last;
    }

    public bool ContainsType(string? name)
    {
        Logging.LogDebug($"checking for {name} in {Description}");
        foreach(Token element in Classes)
            if (element.Name == name || name == ((ClassToken)element).GetFullPath())
                return true;

        foreach(Token element in Namespaces)
            if (element.Name == name || name == ((NamespaceToken)element).GetFullPath() || ((NamespaceToken)element).ContainsType(name))
                return true;

        return false;
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

        foreach(Token element in Classes)
            if (element.Name == token.Name)
                return element;
        
        foreach(Token element in Namespaces)
            if (element.Name == token.Name)
                return element;

        return token;
    }

    public List<ICompletable> GetCompletions(SourceToken token, SourceTokenIterator sourceIterator, bool ignorePositioning = false)
    {
        if (ignorePositioning)
        {
            VariableToken? var = ParserInstance?.FindVariable(sourceIterator.Current.Name);
            Logging.LogDebug($"Searching for '{var?.Description}' inside {Description}");
            foreach(Token element in Classes)
            {
                ClassToken classToken = (ClassToken)element;
                if (sourceIterator.Current.Name != classToken.Name && var?.TypeName != classToken.Name && var?.TypeName != classToken.GetFullPath())
                    continue;

                return classToken.GetCompletions(token, sourceIterator, true);
            }

            foreach(Token element in Namespaces)
            {
                NamespaceToken namespaceToken = (NamespaceToken)element;
                if (sourceIterator.Current.Name != namespaceToken.Name && !namespaceToken.ContainsType(var?.TypeName))
                    continue;

                return namespaceToken.GetCompletions(token, sourceIterator, true);
            }

            List<ICompletable> result1 = [];

            foreach(Token element in Variables)
                result1.Add(element);
            foreach(Token element in Functions)
                result1.Add(element);
            foreach(Token element in Classes)
                result1.Add(element);

            return result1;
        }

        if (token.Line < Start.Line || token.Line > End.Line)
            return [];

        List<ICompletable> result = [];

        foreach(Token element in Variables)
            if (element.Name.Contains(token.Name))
                result.Add(element);

        foreach(Token element in Functions)
            if (element.Name.Contains(token.Name))
                result.Add(element);

        foreach(Token element in Classes)
            if (element.Name.Contains(token.Name))
                result.Add(element);

        return result;
    }
}