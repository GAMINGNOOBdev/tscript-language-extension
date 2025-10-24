using System.Text.RegularExpressions;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace TScriptLanguageServer;

public class TScriptDocumentSyncHandler : ITextDocumentSyncHandler
{
    private readonly ILanguageServerFacade _router;
    private readonly BufferManager _bufferManager;

    private readonly TextDocumentSelector _documentSelector = new(
        new TextDocumentFilter()
        {
            Language = "tscript"
        }
    );

    private TextSynchronizationCapability _capability = new();

    public TScriptDocumentSyncHandler(ILanguageServerFacade router, BufferManager bufferManager)
    {
        _router = router;
        _bufferManager = bufferManager;
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Incremental;

    public TextDocumentChangeRegistrationOptions GetRegistrationOptions()
    {
        return new()
        {
            DocumentSelector = _documentSelector,
            SyncKind = Change,
        };
    }

    public TextDocumentChangeRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return GetRegistrationOptions();
    }

    public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
    {
        return new(uri, "tscript");
    }

    public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new(uri, "tscript");
    }

    public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        string documentPath = request.TextDocument.Uri.ToString();
        string? text = request.ContentChanges.FirstOrDefault()?.Text;

        OmniSharp.Extensions.LanguageServer.Protocol.Models.Range? range = request.ContentChanges.FirstOrDefault()?.Range;
        _bufferManager.UpdateBuffer(documentPath, text, range);

        _router.Window.LogInfo($"Updated buffer for document: {documentPath}");
        //ValidateDocument(documentPath);

        return Unit.Task;
    }

    public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        string documentPath = request.TextDocument.Uri.ToString();
        string text = request.TextDocument.Text;
        _bufferManager.UpdateBuffer(documentPath, text);
        //ValidateDocument(documentPath);
        return Unit.Task;
    }

    public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        string message = "\"public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)\" is not implemented!";
        _router.Window.LogWarning(message);
        return Unit.Task;
    }

    public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        string message = "\"public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)\" is not implemented!";
        _router.Window.LogWarning(message);
        return Unit.Task;
    }

    TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        string message = "\"TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)\" is not implemented!";
        _router.Window.LogWarning(message);
        return new();
    }

    TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        string message = "\"TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)\" is not implemented!";
        _router.Window.LogWarning(message);
        return new();
    }

    TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        string message = "\"TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)\" is not implemented!";
        _router.Window.LogWarning(message);
        return new();
    }

    private void ValidateDocument(string documentPath)
    {
        string text = _bufferManager.GetBuffer(documentPath);
        string[] lines = text.Split([ "\n", "\r\n" ], StringSplitOptions.None);
        List<Diagnostic> diagnostics = [];
        
        HashSet<string> declaredIdentifiers = [];
        HashSet<string> keywords = [ 
            "and", "break", "catch", "class", "constructor", "continue", "do", 
            "else", "false", "for", "from", "function", "if", "namespace", "not", 
            "null", "or", "private", "protected", "public", "return", "static", 
            "super", "then", "this", "throw", "true", "try", "use", "var", "while", "xor"
        ];
        
        Regex varDeclarationRegex = new(@"\bvar\s+([a-zA-Z_][a-zA-Z0-9_]*)");
        Regex funcDeclarationRegex = new(@"\bfunction\s+([a-zA-Z_][a-zA-Z0-9_]*)");
        Regex identifierInLineRegex = new(@"\b[a-zA-Z_][a-zA-Z0-9_]*\b");

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmedLine = line.Trim();

            int quoteCount = line.Count(c => c == '"');
            if (quoteCount % 2 != 0)
            {
                diagnostics.Add(new Diagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, line.IndexOf('"'), i, line.Length),
                    Message = "Unterminated string literal.",
                    Source = "tscript-lsp"
                });
            }

            MatchCollection varMatches = varDeclarationRegex.Matches(line);
            foreach (Match match in varMatches)
            {
                string identifier = match.Groups[1].Value;
                if (declaredIdentifiers.Contains(identifier))
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, match.Index, i, match.Index + match.Length),
                        Message = $"Identifier '{identifier}' has already been declared.",
                        Source = "tscript-ls"
                    });
                }
                else
                    declaredIdentifiers.Add(identifier);
            }

            Match funcMatch = funcDeclarationRegex.Match(line);
            if (funcMatch.Success)
            {
                string identifier = funcMatch.Groups[1].Value;
                if (declaredIdentifiers.Contains(identifier))
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, funcMatch.Index, i, funcMatch.Index + funcMatch.Length),
                        Message = $"Function '{identifier}' has already been declared.",
                        Source = "tscript-ls"
                    });
                }
                else
                    declaredIdentifiers.Add(identifier);
            }

            bool needsSemicolon = Regex.IsMatch(trimmedLine, @"\b(var|return|break|continue|throw|do)\b") ||
                                 Regex.IsMatch(trimmedLine, @"^\s*[a-zA-Z_][a-zA-Z0-9_]*\s*(=|\+=|-=|\*=|\/=)");
            
            if (needsSemicolon && !trimmedLine.EndsWith(";") && !trimmedLine.EndsWith("{") && !string.IsNullOrEmpty(trimmedLine))
            {
                diagnostics.Add(new Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, line.Length, i, line.Length + 1),
                    Message = "Missing semicolon.",
                    Source = "tscript-ls"
                });
            }
        }
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            MatchCollection identifierMatches = identifierInLineRegex.Matches(line);

            foreach (Match match in identifierMatches)
            {
                string identifier = match.Value;
                if (!declaredIdentifiers.Contains(identifier) && 
                    !keywords.Contains(identifier) && 
                    !IsDeclaration(line, identifier))
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, match.Index, i, match.Index + identifier.Length),
                        Message = $"Identifier '{identifier}' is not defined.",
                        Source = "tscript-ls"
                    });
                }
            }
        }
        
        _router.Client.SendNotification(TextDocumentNames.PublishDiagnostics, 
            new PublishDiagnosticsParams { Uri = documentPath, Diagnostics = new Container<Diagnostic>(diagnostics) });
    }

    private static bool IsDeclaration(string line, string identifier)
    {
        Regex varPattern = new($@"\bvar\s+.*{identifier}");
        Regex funcPattern = new($@"\bfunction\s+{identifier}");
        return varPattern.IsMatch(line) || funcPattern.IsMatch(line);
    }
}
