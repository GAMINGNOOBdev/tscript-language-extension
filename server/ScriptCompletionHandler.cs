using TScriptLanguageServer.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace TScriptLanguageServer;

public class ScriptCompletionHandler : ICompletionHandler, ICompletionResolveHandler
{
    private readonly ILanguageServerFacade _router;
    private readonly BufferManager _bufferManager;

    private readonly TextDocumentSelector _documentSelector = new(
        new TextDocumentFilter()
        {
            Language = "tscript"
        }
    );

    public ScriptCompletionHandler(ILanguageServerFacade router, BufferManager bufferManager)
    {
        _router = router;
        _bufferManager = bufferManager;
    }

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

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities) => GetRegistrationOptions();

    public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        Logging.LogDebug($"calling \"public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)\"");
        try
        {
            string documentPath = request.TextDocument.Uri.ToString();
            Buffer? buffer = _bufferManager.GetBuffer(documentPath);

            if (buffer == null)
                return await Task.FromResult(new CompletionList());

            Tuple<SourceToken, SourceTokenIterator> tokenPair = buffer.Tokenizer.GetTokenAtPosition(request.Position.Line, request.Position.Character);
            SourceToken token = tokenPair.Item1;

            List<ICompletable> completions = token.GetCompletions();
            foreach(ICompletable i in buffer.TokenParser.GetCompletions(token, tokenPair.Item2))
                completions.Add(i);

            Logging.LogDebug($"completion count: {completions.Count}");
            Logging.LogDebug($"completions:");
            foreach (var i in completions)
                Logging.LogDebug($"\t{i.Name} - {i.Description} - {Enum.GetName(typeof(CompletionItemKind), i.CompletionKind)}");

            if (request.Context?.TriggerKind == CompletionTriggerKind.TriggerCharacter)
                return await Task.FromResult(new CompletionList(completions.Select(x => new CompletionItem
                {
                    Label = x.Name,
                    Kind = x.CompletionKind,
                    InsertText = x.Name,
                }), isIncomplete: completions.Count > 1));

            return await Task.FromResult(new CompletionList(completions.Select(x => new CompletionItem
            {
                Label = x.Name,
                Kind = x.CompletionKind,
                TextEdit = new TextEdit
                {
                    NewText = x.Name,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        new Position
                        {
                            Line = token.Line-1,
                            Character = token.StartPos
                        }, new Position
                        {
                            Line = request.Position.Line,
                            Character = request.Position.Character
                        })
                }
            }), isIncomplete: completions.Count > 1));
        }
        catch (Exception e)
        {
            Logging.LogError($"error: {e}");
            return await Task.FromResult(new CompletionList());
        }
    }

    public Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        string message = "\"public Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)\" is not implemented!";
        Logging.LogWarning(message);
        Logging.LogDebug($"request: {request} cancellation: {cancellationToken}");
        return Task.FromResult(new CompletionItem());
    }

    public void SetCapability(CompletionCapability capability, ClientCapabilities clientCapabilities)
    {
        string message = "\"public void SetCapability(CompletionCapability capability, ClientCapabilities clientCapabilities)\" is not implemented!";
        Logging.LogWarning(message);
    }
}
