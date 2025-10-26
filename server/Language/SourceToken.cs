using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace TScriptLanguageServer.Language;

public enum SourceTokenType
{
    INVALID,
    NUMBER,
    KEYWORD,
    TEXT,
    NULL,
    FIELD,
    DOT,
    COLON,
    COMMA,
    PARENTHESISOPEN,
    PARENTHESISCLOSE,
    BRACKETOPEN,
    BRACKETCLOSE,
    CURLYBRACKETOPEN,
    CURLYBRACKETCLOSE,
    EXCLAMATIONPOINT,
    SEMICOLON,
    PLUS,
    MINUS,
    ASTERISK,
    SLASH,
    MODULO,
    AND,
    OR,
    TILDE,
    CARET,
    EQUALSIGN,
    EQUALTO,
    NOTEQUAL,
    LESSEQUAL,
    GREATEREQUAL,
    MODEQUAL,
    DIVEQUAL,
    MULEQUAL,
    MINUSEQUAL,
    PLUSEQUAL,
    ANDEQUAL,
    OREQUAL,
    XOREQUAL,
    ANDAND,
    OROR,
    MINUSMINUS,
    PLUSPLUS,
    BITSHIFTLEFT,
    BITSHIFTRIGHT,
    QUOTE,
    DOUBLEQUOTE,
    LESSTHAN,
    GREATERTHAN,
};

public class SourceToken(SourceTokenType type = SourceTokenType.INVALID, string name = "", int line = int.MinValue, int startpos = int.MinValue) : ICompletable
{
    public SourceTokenType Type { get; private set; } = type;
    public string Name { get; private set; } = name;
    public int Line { get; private set; } = line;
    public int StartPos { get; private set; } = startpos;
    public int EndPos => StartPos + Name.Length;

    public bool IsInternalType() => Specifications.DataTypes.Contains(Name);
    public override string ToString() => $"SourceToken({Enum.GetName(typeof(SourceTokenType), Type)} - \"{Name}\" - '{Line}:{StartPos})'";
    public bool InRange(int line, int pos) => line == Line && StartPos <= pos && pos <= EndPos;

    public string? Description{ get
    {
        if (IsInternalType())
            return "Internal Data Type";

        if (Type == SourceTokenType.KEYWORD)
            return "Internal Keyword";

        if (Type == SourceTokenType.NUMBER)
            return "Number";

        if (TokenManager.Instance?.FindFunction(this) == true)
            return "Function";

        if (Type != SourceTokenType.TEXT && Name.Length >= 1 && Name.Length <= 2)
            return null;

        return "Unknown Token / Text Token";
    }}

    public string? TypeName => Enum.GetName(typeof(SourceTokenType), Type);
    public CompletionItemKind CompletionKind => Type == SourceTokenType.KEYWORD ? CompletionItemKind.Keyword : CompletionItemKind.Text;

    public List<ICompletable> GetCompletions()
    {
        if (string.IsNullOrEmpty(Name))
            return [];

        List<ICompletable> result = [];
        Logging.LogDebug($"token name: {Name}");
        foreach (var keyword in Specifications.Keywords)
        {
            if (!keyword.Name.StartsWith(Name))
                continue;

            result.Add(new SourceToken(keyword.Type, keyword.Name));
            Logging.LogDebug($"\tcompletion: {keyword.Name}");
        }
        return result;
    }

    internal void Copy(SourceToken token)
    {
        Type = token.Type;
        Name = token.Name;
        Line = token.Line;
        StartPos = token.StartPos;
    }
}
