namespace TScriptLanguageServer;

using TScriptLanguageServer.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

public class TooltipHandler : IHoverHandler
{
    private readonly ILanguageServerFacade _router;
    private readonly BufferManager _bufferManager;

    private readonly TextDocumentSelector _documentSelector = new(
        new TextDocumentFilter()
        {
            Language = "tscript"
        }
    );

    public TooltipHandler(ILanguageServerFacade router, BufferManager bufferManager)
    {
        _router = router;
        _bufferManager = bufferManager;
    }

    public HoverRegistrationOptions GetRegistrationOptions()
    {
        return new()
        {
            DocumentSelector = _documentSelector
        };
    }

    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
    {
        return GetRegistrationOptions();
    }

    public Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        try
        {
            string documentPath = request.TextDocument.Uri.ToString();
            Buffer? buffer = _bufferManager.GetBuffer(documentPath);
            if (buffer == null)
                return Task.FromResult<Hover?>(null);

            Tuple<SourceToken, SourceTokenIterator> tokenPair = buffer.Tokenizer.GetTokenAtPosition(request.Position.Line, request.Position.Character);
            SourceToken token = tokenPair.Item1;
            SourceTokenIterator iter = tokenPair.Item2;
            if (iter.Current.Type == SourceTokenType.DOT)
            {
                token = iter.Current;
                _ = iter.Prev;
            }
            ICompletable completable = token;
            List<ICompletable> parsedTokens = buffer.TokenParser.GetCompletions(token, iter);
            if (parsedTokens.Count > 0)
                completable = buffer.TokenParser.GetCompletion(token);

            if (completable.Description == null)
                return Task.FromResult<Hover?>(null);

            MarkupContent contents = new()
            {
                Kind = MarkupKind.Markdown,
                Value = $"```tscript\n{completable.Name} ({completable.TypeName})\n```\n{completable.Description}"
            };

            Hover h = new()
            {
                Contents = new(contents),
                Range = new(
                    new Position(token.Line-1, token.StartPos),
                    new Position(token.Line-1, token.EndPos)
                )
            };
            return Task.FromResult<Hover?>(h);
        }
        catch (Exception e)
        {
            Logging.LogError($"error: {e}");
        }

        return Task.FromResult<Hover?>(null);
    }
}
