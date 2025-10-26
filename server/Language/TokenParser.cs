using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace TScriptLanguageServer.Language;

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
    public string GetFullPath() => ParentNamespace != null ? $"{ParentNamespace.GetFullPath()}.{Name}" : Name;

    private SourceToken Start = new(), End = new();

    public void Parse(SourceTokenIterator iter)
    {
        SourceToken token = iter.Next;
        if (token.Type != SourceTokenType.CURLYBRACKETOPEN)
            return;

        Start = token;
        while (iter.HasNext && token.Type != SourceTokenType.CURLYBRACKETCLOSE)
        {
            if (token.Name.StartsWith("#type="))
            {
                if (ParserInstance != null)
                    ParserInstance.NextVariableType = new SourceToken(SourceTokenType.TEXT, token.Name[6..]);
                token = iter.Next;
                continue;
            }

            if (token.IsInternalType())
            {
                SourceToken typeToken = new();
                typeToken.Copy(token);
                if (ParserInstance?.NextVariableType != null)
                {
                    typeToken = ParserInstance.NextVariableType;
                    ParserInstance.NextVariableType = null;
                }
                token = iter.Next;
                if (token.Type != SourceTokenType.TEXT)
                    continue;
                SourceToken next = iter.Current;
                if (next.Type != SourceTokenType.EQUALSIGN && next.Type != SourceTokenType.SEMICOLON)
                    continue;
                while (iter.HasNext)
                {
                    if (next.Type == SourceTokenType.SEMICOLON)
                        break;

                    next = iter.Next;
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
                token = iter.Next;
                FunctionToken functionToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    Parent = null,
                    ParentNamespace = this
                };
                functionToken.Parse(iter);
                Functions.Add(functionToken);
            }

            if (token.Name == "class")
            {
                token = iter.Next;
                ClassToken classToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    ParentNamespace = this,
                };
                classToken.Parse(iter);
                Classes.Add(classToken);
            }

            if (token.Name == "namespace")
            {
                token = iter.Next;
                NamespaceToken namespaceToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    ParentNamespace = this,
                };
                namespaceToken.Parse(iter);
                Namespaces.Add(namespaceToken);
            }

            token = iter.Next;
        }
        End = iter.Current;
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

public class ClassToken(int line = int.MinValue, int startpos = int.MinValue, string name = "") : Token(line, startpos, name, CompletionItemKind.Struct)
{
    public TokenParser? ParserInstance = null;
    public NamespaceToken? ParentNamespace = null;
    public List<VariableToken> Variables = [];
    public List<FunctionToken> Functions = [];

    public override string? TypeName => "class";
    public override string? Description => $"class {GetFullPath()}";
    public string GetFullPath() => ParentNamespace != null ? $"{ParentNamespace.GetFullPath()}.{Name}" : Name;

    private SourceToken Start = new(), End = new();

    public void Parse(SourceTokenIterator iter)
    {
        SourceToken token = iter.Next;
        if (token.Type != SourceTokenType.CURLYBRACKETOPEN)
            return;

        Start = token;
        while (iter.HasNext && token.Type != SourceTokenType.CURLYBRACKETCLOSE)
        {
            if (token.Name.StartsWith("#type="))
            {
                if (ParserInstance != null)
                    ParserInstance.NextVariableType = new SourceToken(SourceTokenType.TEXT, token.Name[6..]);
                token = iter.Next;
                continue;
            }

            if (token.IsInternalType())
            {
                SourceToken typeToken = new();
                typeToken.Copy(token);
                if (ParserInstance?.NextVariableType != null)
                {
                    typeToken = ParserInstance.NextVariableType;
                    ParserInstance.NextVariableType = null;
                }
                token = iter.Next;
                if (token.Type != SourceTokenType.TEXT)
                    continue;
                SourceToken next = iter.Current;
                if (next.Type != SourceTokenType.EQUALSIGN && next.Type != SourceTokenType.SEMICOLON)
                    continue;
                while (iter.HasNext)
                {
                    if (next.Type == SourceTokenType.SEMICOLON)
                        break;

                    next = iter.Next;
                }
                Variables.Add(new VariableToken(token.Line, token.StartPos, token.Name){
                    VariableType = typeToken,
                    ParserInstance = ParserInstance,
                    Parent = this
                });
            }

            if (token.Name == "function")
            {
                token = iter.Next;
                FunctionToken functionToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    Parent = this,
                };
                functionToken.Parse(iter);
                Functions.Add(functionToken);
            }

            if (token.Name == "constructor")
            {
                FunctionToken constructorToken = new(token.Line, token.StartPos, token.Name){
                    ParserInstance = ParserInstance,
                    Parent = this,
                };
                constructorToken.Parse(iter);
                Functions.Add(constructorToken);
            }

            token = iter.Next;
        }
        End = iter.Current;
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

    public List<ICompletable> GetCompletions(SourceToken token, SourceTokenIterator sourceIterator, bool ignorePositioning = false)
    {
        if (ignorePositioning)
        {
            List<ICompletable> result1 = [];

            foreach(Token element in Variables)
                result1.Add(element);
            foreach(Token element in Functions)
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

        return result;
    }
}

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
        SourceToken token = iter.Next;
        if (token.Type != SourceTokenType.PARENTHESISOPEN)
            return;

        ParseParameters(iter);

        Start = token = iter.Current;
        if (token.Type != SourceTokenType.CURLYBRACKETOPEN)
            return;

        while (iter.HasNext && token.Type != SourceTokenType.CURLYBRACKETCLOSE)
        {
            if (token.Name.StartsWith("#type="))
            {
                if (ParserInstance != null)
                    ParserInstance.NextVariableType = new SourceToken(SourceTokenType.TEXT, token.Name[6..]);
                token = iter.Next;
                continue;
            }

            if (token.IsInternalType() || ParserInstance?.IsStruct(token) == true)
            {
                SourceToken typeToken = new();
                typeToken.Copy(token);
                if (ParserInstance?.NextVariableType != null)
                {
                    typeToken = ParserInstance.NextVariableType;
                    ParserInstance.NextVariableType = null;
                }
                token = iter.Next;
                if (token.Type != SourceTokenType.TEXT)
                    continue;
                SourceToken next = iter.Current;
                if (next.Type != SourceTokenType.EQUALSIGN && next.Type != SourceTokenType.SEMICOLON)
                    continue;
                Variables.Add(new VariableToken(token.Line, token.StartPos, token.Name){
                    VariableType = typeToken,
                    ParserInstance = ParserInstance
                });
            }

            token = iter.Next;
        }
        End = iter.Current;
    }

    private void ParseParameters(SourceTokenIterator iter)
    {
        SourceToken token = iter.Next;
        while (iter.HasNext && token.Type != SourceTokenType.PARENTHESISCLOSE)
        {
            AddParameter(iter, token);
            token = iter.Next;
        }
    }

    private void AddParameter(SourceTokenIterator iter, SourceToken nameToken)
    {
        SourceToken next = iter.Current;
        if (next.Type != SourceTokenType.PARENTHESISCLOSE && next.Type != SourceTokenType.COMMA)
            return;

        Parameters.Add(new VariableToken(nameToken.Line, nameToken.StartPos, nameToken.Name){
            VariableType = iter.Last
        });
    }
}

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

            if (token.IsInternalType())
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
