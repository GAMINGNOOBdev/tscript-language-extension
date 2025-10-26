namespace TScriptLanguageServer.Language;

public class SourceTokenIterator(List<SourceToken> tokens)
{
    public List<SourceToken> Tokens = tokens;
    public int Index = 0;

    public bool HasNext => Index < Tokens.Count;

    public SourceToken Next {
        get
        {
            if (Index >= Tokens.Count)
                return new();
            
            return Tokens[Index++];
        }
    }

    public SourceToken Current {
        get
        {
            if (Index >= Tokens.Count)
                return new();

            return Tokens[Index];
        }
    }

    public SourceToken Last {
        get
        {
            if (Index == 0)
                return new();

            return Tokens[Index-1];
        }
    }

    public SourceToken Prev {
        get
        {
            if (Index == 0)
                return new();
            
            return Tokens[--Index];
        }
    }

    public SourceToken Peek {
        get
        {
            if (Index >= Tokens.Count)
                return new();

            return Tokens[Index+1];
        }
    }
}
