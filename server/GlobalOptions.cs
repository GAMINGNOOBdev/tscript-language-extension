using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace TScriptLanguageServer;

public struct GlobalOptions
{
    public static readonly List<CompletionItem> Completions = new()
    {
        new CompletionItem { Label = "var", Kind = CompletionItemKind.Keyword, Detail = "Variable declaration" },
        new CompletionItem { Label = "function", Kind = CompletionItemKind.Function, Detail = "Function declaration" },
        new CompletionItem { Label = "if", Kind = CompletionItemKind.Keyword, Detail = "Conditional statement" },
        new CompletionItem { Label = "return", Kind = CompletionItemKind.Keyword, Detail = "Return from function" },
        new CompletionItem { Label = "while", Kind = CompletionItemKind.Keyword, Detail = "Loop statement" },
        new CompletionItem { Label = "for", Kind = CompletionItemKind.Keyword, Detail = "Loop statement" },
    };
}