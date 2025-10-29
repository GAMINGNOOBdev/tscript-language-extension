namespace TScriptLanguageServer.Language;

using System.Collections.Concurrent;

public class TokenManager
{
    public static TokenManager? Instance { get; private set; }

    private readonly ConcurrentBag<Token> tokens = [];

    public TokenManager()
    {
        Instance = this;
    }

    public void AddToken(Token token)
    {
        if (token == null)
            return;

        if (tokens.Contains(token))
        {
            int index = tokens.ToList().IndexOf(token);
            tokens.ElementAt(index).Copy(token);
            return;
        }

        tokens.Add(token);
    }

    public void RemoveToken(string name)
    {
        int index = tokens.ToList().FindIndex((Token t) => {
            return t.Name == name;
        });
    }

    public Token GetToken(string name)
    {
        foreach (Token token in tokens)
        {
            if (token.Name == name)
                return token;
        }

        return new();
    }

    public bool FindFunction(SourceToken token)
    {
        return false;
    }
}
