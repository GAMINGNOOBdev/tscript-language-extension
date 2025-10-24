using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace TScriptLanguageServer;

public enum TokenType
{
    Keyword = 0,
    Function = 1,
    Variable = 2,
    String = 3,
    Number = 4,
}

public record Token(
    string Value,
    Position Start,
    Position End,
    TokenType Type
);


