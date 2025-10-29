namespace TScriptLanguageServer.Language;

public class SourceTokenIterator(List<SourceToken> tokens)
{
    public List<SourceToken> Tokens = tokens;
    public int Index = 0;

    public bool HasNext => Index < Tokens.Count;

    public SourceToken Next {
        get
        {
            if (Index >= Tokens.Count || Tokens.Count == 0)
                return new();
            
            return Tokens[Index++];
        }
    }

    public SourceToken Current {
        get
        {
            if (Index >= Tokens.Count || Index < 0 || Tokens.Count == 0)
                return new();

            return Tokens[Index];
        }
    }

    public SourceToken Last {
        get
        {
            if (Index <= 0 || Tokens.Count == 0)
                return new();

            return Tokens[Index-1];
        }
    }

    public SourceToken Prev {
        get
        {
            if (Index == 0 || Tokens.Count == 0)
                return new();
            
            return Tokens[--Index];
        }
    }

    public SourceToken Peek {
        get
        {
            if (Index >= Tokens.Count || Tokens.Count == 0)
                return new();

            return Tokens[Index+1];
        }
    }

    public SourceTokenIterator SubTokens(SourceTokenType startType, SourceTokenType endType, out bool success)
    {
        List<SourceToken> output = [];
        SourceToken nextToken = new();
        int depth = 1;
        while (depth > 0 && Index+1 < Tokens.Count)
        {
            nextToken = Tokens[++Index];

            if (nextToken.Type == startType)
            {
                Logging.LogInfo($"depth + 1: {depth + 1} token: {nextToken}");
                depth++;
            }

            if (nextToken.Type == endType)
            {
                Logging.LogInfo($"depth - 1: {depth - 1} token: {nextToken}");
                depth--;
            }

            output.Add(nextToken);
        }

        // remove the last pushed closing type token
        // if (nextToken.Type == endType)
        //     output.RemoveAt(output.Count-1);

        success = depth == 0;

        return new(output);
    }
}
