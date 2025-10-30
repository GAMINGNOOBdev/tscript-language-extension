namespace TScriptLanguageServer.Language;

using System.Collections.Concurrent;
using TScriptLanguageServer.Language.Tokens;

public class TokenManager
{
    public static TokenManager? Instance { get; private set; }

    private readonly ConcurrentBag<TokenParser> tokenParsers = [];

    public TokenManager()
    {
        Instance = this;
    }

    public void ParseFile(string contents)
    {
        Tokenizer tk = new();
        tk.ParseFile(contents);
        TokenParser tp = new();
        tp.Parse(tk);
        tokenParsers.Add(tp);
    }

    public List<ICompletable> GetCompletions(SourceToken token, SourceTokenIterator sourceIterator)
    {
        List<ICompletable> results = [];

        foreach (TokenParser tp in tokenParsers)
            foreach(ICompletable ic in tp.GetCompletions(token, sourceIterator))
                results.Add(ic);

        return results;
    }

    public ICompletable GetCompletion(SourceToken token, ICompletable completable)
    {
        ICompletable? result = null;

        foreach (TokenParser tp in tokenParsers)
        {
            ICompletable ic = tp.GetCompletion(token);
            if (ic == token)
                continue;
            
            result = ic;
            break;
        }

        if (result == null)
            return completable;

        return result;
    }

    public bool FindFunction(SourceToken token)
    {
        foreach (TokenParser tp in tokenParsers)
            foreach (Token t in tp.Functions)
                if (t is FunctionToken function)
                    if (function.Name == token.Name)
                        return true;

        return false;
    }
}
