using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace TScriptLanguageServer;

public class TScriptCompletionHandler(ILanguageServerFacade router, BufferManager bufferManager) : ICompletionHandler, ICompletionResolveHandler
{
    private readonly ILanguageServerFacade _router = router;
    private readonly BufferManager _bufferManager = bufferManager;

    private readonly TextDocumentSelector _documentSelector = new(
        new TextDocumentFilter()
        {
            Language = "tscript"
        }
    );

    public Guid Id { get; } = Guid.Empty;

    public CompletionRegistrationOptions GetRegistrationOptions()
    {
        return new()
        {
            DocumentSelector = _documentSelector,
            ResolveProvider = true,
            TriggerCharacters = new[] { "." }
        };
    }

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
    {
        return GetRegistrationOptions();
    }

    public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        string documentPath = request.TextDocument.Uri.ToString();
        List<Token> allTokens = _bufferManager.GetTokens(documentPath);
        string documentText = _bufferManager.GetBuffer(documentPath);

        string partialWord = GetWordAtPosition(documentText, request.Position);
        
        List<string> declaredIdentifiers = allTokens
            .Where(t => t.Type == TokenType.Variable || t.Type == TokenType.Function)
            .Select(t => t.Value)
            .Distinct()
            .ToList();
            
        IEnumerable<CompletionItem> declaredCompletions = declaredIdentifiers
            .Select(name => new CompletionItem
            {
                Label = name,
                Kind = allTokens.FirstOrDefault(t => t.Value == name)?.Type == TokenType.Function 
                       ? CompletionItemKind.Function 
                       : CompletionItemKind.Variable,
                Detail = $"Declared identifier: {name}"
            });

        List<CompletionItem> finalCompletions = declaredCompletions
            .Union(GlobalOptions.Completions.Where(g => !declaredIdentifiers.Contains(g.Label)))
            .ToList();

        if (string.IsNullOrEmpty(partialWord))
            return await Task.FromResult(new CompletionList(finalCompletions, isIncomplete: false));

        List<CompletionItem> filteredCompletions = finalCompletions
            .Where(item => item.Label.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
            .Where(item => 
                !string.Equals(item.Label, partialWord, StringComparison.OrdinalIgnoreCase) || // Keep if NOT an exact match
                declaredIdentifiers.Contains(item.Label) ||                                   // OR if it IS a declared identifier (like "i")
                GlobalOptions.Completions.Any(g => string.Equals(g.Label, item.Label, StringComparison.OrdinalIgnoreCase)) // OR if it's a global keyword/snippet
            )
            .ToList();

        return await Task.FromResult(new CompletionList(filteredCompletions, isIncomplete: false));
    }

    public Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        /// ignore for now
        return Task.FromResult(new CompletionItem());
    }

    public void SetCapability(CompletionCapability capability, ClientCapabilities clientCapabilities) {
        /// ignore for now
    }

    private string GetWordAtPosition(string text, Position position)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Convert the LSP Position to a simple string index
        int index = BufferManager.GetPositionInBuffer(text, position);
        if (index > text.Length) index = text.Length;

        // Step back to the start of the word
        int start = index;
        while (start > 0 && char.IsLetterOrDigit(text[start - 1]))
        {
            start--;
        }

        // The current word is the substring from 'start' to 'index'
        return text.Substring(start, index - start);
    }
}
