namespace TScriptLanguageServer.Language;

public record Keyword(
    string Name,
    SourceTokenType Type
);

public record SpecialOperator(
    string Name,
    SourceTokenType Type
);

public record SpecialCharacter(
    string Name,
    SourceTokenType Type
);

public class Specifications
{
    public static string[] DataTypes { get; } = [
        "var",
    ];

    public static Keyword[] Keywords { get; } = [
        new("null", SourceTokenType.NULL),
        new("if", SourceTokenType.KEYWORD),
        new("in", SourceTokenType.KEYWORD),
        new("then", SourceTokenType.KEYWORD),
        new("else", SourceTokenType.KEYWORD),
        new("for", SourceTokenType.KEYWORD),
        new("do", SourceTokenType.KEYWORD),
        new("while", SourceTokenType.KEYWORD),
        new("break", SourceTokenType.KEYWORD),
        new("continue", SourceTokenType.KEYWORD),
        new("return", SourceTokenType.KEYWORD),
        new("try", SourceTokenType.KEYWORD),
        new("catch", SourceTokenType.KEYWORD),
        new("throw", SourceTokenType.KEYWORD),
        new("from", SourceTokenType.KEYWORD),
        new("use", SourceTokenType.KEYWORD),
        new("as", SourceTokenType.KEYWORD),
        new("var", SourceTokenType.KEYWORD),
        new("function", SourceTokenType.KEYWORD),
        new("class", SourceTokenType.KEYWORD),
        new("namespace", SourceTokenType.KEYWORD),
        new("constructor", SourceTokenType.KEYWORD),
        new("public", SourceTokenType.KEYWORD),
        new("protected", SourceTokenType.KEYWORD),
        new("private", SourceTokenType.KEYWORD),
        new("static", SourceTokenType.KEYWORD),
        new("and", SourceTokenType.KEYWORD),
        new("or", SourceTokenType.KEYWORD),
        new("not", SourceTokenType.KEYWORD),
        new("xor", SourceTokenType.KEYWORD),
        new("true", SourceTokenType.KEYWORD),
        new("false", SourceTokenType.KEYWORD),
        new("this", SourceTokenType.KEYWORD),
        new("super", SourceTokenType.KEYWORD),
        new("var", SourceTokenType.KEYWORD),
    ];

    public static SpecialOperator[] SpecialOperators {get;} = [
        new("==", SourceTokenType.EQUALTO),
        new("!=", SourceTokenType.NOTEQUAL),
        new("<=", SourceTokenType.LESSEQUAL),
        new(">=", SourceTokenType.GREATEREQUAL),
        new("%=", SourceTokenType.MODEQUAL),
        new("/=", SourceTokenType.DIVEQUAL),
        new("*=", SourceTokenType.MULEQUAL),
        new("-=", SourceTokenType.MINUSEQUAL),
        new("+=", SourceTokenType.PLUSEQUAL),
        new("&=", SourceTokenType.ANDEQUAL),
        new("|=", SourceTokenType.OREQUAL),
        new("^=", SourceTokenType.XOREQUAL),
        new("&&", SourceTokenType.ANDAND),
        new("||", SourceTokenType.OROR),
        new("--", SourceTokenType.MINUSMINUS),
        new("++", SourceTokenType.PLUSPLUS),
        new("<<", SourceTokenType.BITSHIFTLEFT),
        new(">>", SourceTokenType.BITSHIFTRIGHT),
    ];

    public static SpecialCharacter[] SpecialCharacters {get;} = [
        new("+", SourceTokenType.PLUS),
        new("-", SourceTokenType.MINUS),
        new("*", SourceTokenType.ASTERISK),
        new("/", SourceTokenType.SLASH),
        new("%", SourceTokenType.MODULO),
        new("&", SourceTokenType.AND),
        new("|", SourceTokenType.OR),
        new("~", SourceTokenType.TILDE),
        new("^", SourceTokenType.CARET),
        new("(", SourceTokenType.PARENTHESISOPEN),
        new(")", SourceTokenType.PARENTHESISCLOSE),
        new("=", SourceTokenType.EQUALSIGN),
        new("[", SourceTokenType.BRACKETOPEN),
        new("]", SourceTokenType.BRACKETCLOSE),
        new("{", SourceTokenType.CURLYBRACKETOPEN),
        new("}", SourceTokenType.CURLYBRACKETCLOSE),
        new(".", SourceTokenType.DOT),
        new(":", SourceTokenType.COLON),
        new(",", SourceTokenType.COMMA),
        new("<", SourceTokenType.LESSTHAN),
        new(">", SourceTokenType.GREATERTHAN),
        new("\"", SourceTokenType.DOUBLEQUOTE),
        new("\'", SourceTokenType.QUOTE),
        new("!", SourceTokenType.EXCLAMATIONPOINT),
        new(";", SourceTokenType.SEMICOLON),
    ];

    public static int IsKeyword(string buffer)
    {
        for (int i = 0; i < Keywords.Length; i++)
            if (Keywords[i].Name == buffer)
                return i;
        return -1;
    }

    public static int IsSpecialChar(char c)
    {
        for (int i = 0; i < SpecialCharacters.Length; i++)
            if (SpecialCharacters[i].Name[0] == c)
                return i;
        return -1;
    }

    public static int IsSpecialOperator(char a, char b)
    {
        string operatorName = $"{a}{b}";
        for (int i = 0; i < SpecialOperators.Length; i++)
            if (SpecialOperators[i].Name == operatorName)
                return i;
        return -1;
    }

    private readonly static Dictionary<char,char> EscapeCharacters = new(){
        { 'a',  '\a' },
        { 'b',  '\b'},
        { 'f',  '\f'},
        { 'n',  '\n'},
        { 'r',  '\r'},
        { 't',  '\t'},
        { 'v',  '\v'},
        { '\\', '\\'},
        { '\'', '\''},
        { '"',  '"' },
        { '?',  '?' },
        { '0',  '\0'},
    };

    public static string ConnectEscapeSequences(string str)
    {
        string result = "";

        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];

            if (c == '\\' && i + 1 < str.Length)
            {
                i++;
                c = EscapeCharacters[str[i]];
            }

            result = $"{result}{c}";
        }

        return result;
    }

    public static bool IsNumeric(string str)
    {
        bool isNumber = int.TryParse(str, out _);
        bool isHex = IsHex(str);
        bool isBin = IsBin(str);

        return isNumber || isHex || isBin;
    }

    private static bool IsHex(string str)
    {
        if (!str.StartsWith("0x") && !str.StartsWith("0X"))
            return false;

        for (int i = 2; i < str.Length; i++)
        {
            if (!Uri.IsHexDigit(str[i]))
                return false;
        }
        return true;
    }

    private static bool IsBin(string str)
    {
        if (!str.StartsWith("0b") && !str.StartsWith("0B"))
            return false;

        for (int i = 2; i < str.Length; i++)
        {
            if (str[i] != '0' && str[i] != '1')
                return false;
        }
        return true;
    }
}
