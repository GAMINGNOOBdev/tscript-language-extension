namespace TScriptLanguageServer.Language;

using TScriptLanguageServer.Language.Tokens;

public class TokenParser
{
    public List<Token> Classes { get; private set; } = [];
    public List<Token> Variables { get; private set; } = [];
    public List<Token> Functions { get; private set; } = [];
    public List<Token> Namespaces { get; private set; } = [];

    public SourceToken? NextVariableType { get; set; } = null;

    public ICompletable GetCompletion(SourceToken token)
    {
        foreach(Token element in Classes)
        {
            ICompletable result = ((ClassToken)element).GetCompletion(token);
            if (result != token)
                return result;

            if (element.Name == token.Name)
                return element;
        }

        foreach(Token element in Variables)
            if (element.Name == token.Name)
                return element;

        foreach(Token element in Functions)
        {
            ICompletable result = ((FunctionToken)element).GetCompletion(token);
            if (result != token)
                return result;

            if (element.Name == token.Name)
                return element;
        }

        foreach(Token element in Namespaces)
        {
            ICompletable result = ((NamespaceToken)element).GetCompletion(token);
            if (result != token)
                return result;

            if (element.Name == token.Name)
                return element;
        }

        return token;
    }

    public List<ICompletable> GetCompletions(SourceToken token, SourceTokenIterator sourceIterator)
    {
        List<ICompletable> result = [];

        Logging.LogDebug($"token {token}");
        Logging.LogDebug($"curr {sourceIterator.Current}");
        Logging.LogDebug($"last {sourceIterator.Last}");
        if (token.Type == SourceTokenType.DOT)
        {
            VariableToken? var = FindVariable(sourceIterator.Current.Name);
            return GetSubCompletions(sourceIterator.Current, sourceIterator, var);
        }

        foreach(Token element in Namespaces)
        {
            foreach (ICompletable element2 in ((NamespaceToken)element).GetCompletions(token, sourceIterator))
                result.Add(element2);

            if (element.Name.Contains(token.Name))
                result.Add(element);
        }

        foreach(Token element in Classes)
        {
            foreach (ICompletable element2 in ((ClassToken)element).GetCompletions(token, sourceIterator))
                result.Add(element2);

            if (element.Name.Contains(token.Name))
                result.Add(element);
        }

        foreach(Token element in Functions)
        {
            foreach (ICompletable element2 in ((FunctionToken)element).GetCompletions(token))
                result.Add(element2);

            if (element.Name.Contains(token.Name))
                result.Add(element);
        }

        foreach(Token element in Variables)
            if (element.Name.Contains(token.Name))
                result.Add(element);

        return result;
    }

    private List<ICompletable> GetSubCompletions(SourceToken token, SourceTokenIterator sourceIterator, VariableToken? variable)
    {
        foreach(Token element in Classes)
        {
            ClassToken classToken = (ClassToken)element;
            if (sourceIterator.Current.Name != classToken.Name && variable?.TypeName != classToken.Name && variable?.TypeName != classToken.GetFullPath())
                continue;

            return classToken.GetCompletions(token, sourceIterator, true);
        }

        foreach(Token element in Namespaces)
        {
            NamespaceToken namespaceToken = (NamespaceToken)element;
            if (sourceIterator.Current.Name != namespaceToken.Name && !namespaceToken.ContainsType(variable?.TypeName))
                continue;

            return namespaceToken.GetCompletions(token, sourceIterator, true);
        }

        return [];
    }

    public void Parse(Tokenizer tokenizer)
    {
        SourceTokenIterator iter = tokenizer.Tokens;
        while (iter.HasNext)
        {
            SourceToken token = iter.Next;

            if (token.Name.StartsWith("#type="))
            {
                NextVariableType = new SourceToken(SourceTokenType.TEXT, token.Name[6..]);
                continue;
            }

            if (token.IsInternalType)
            {
                SourceToken typeToken = new();
                typeToken.Copy(token);
                if (NextVariableType != null)
                {
                    typeToken = NextVariableType;
                    NextVariableType = null;
                }
                token = iter.Next;
                if (token.Type != SourceTokenType.TEXT)
                    continue;
                SourceToken next = iter.Current;
                if (next.Type != SourceTokenType.EQUALSIGN && next.Type != SourceTokenType.SEMICOLON)
                    continue;
                Variables.Add(new VariableToken(token.Line, token.StartPos, token.Name){
                    VariableType = typeToken,
                    ParserInstance = this
                });
                continue;
            }

            if (token.Name == "function")
            {
                token = iter.Next;
                FunctionToken functionToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = this
                };
                functionToken.Parse(iter);
                Functions.Add(functionToken);
                continue;
            }

            if (token.Name == "class")
            {
                token = iter.Next;
                ClassToken classToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = this
                };
                classToken.Parse(iter);
                Classes.Add(classToken);
                continue;
            }

            if (token.Name == "namespace")
            {
                token = iter.Next;
                NamespaceToken namespaceToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = this
                };
                namespaceToken.Parse(iter);
                Namespaces.Add(namespaceToken);
                continue;
            }
        }
    }

    public bool IsStruct(SourceToken token) => Classes.Find((tok) => { return tok.Name == token.Name; }) != null;
    public VariableToken? FindVariable(string name) => (VariableToken?)Variables.Find((token) => { return token.Name == name; });

}
