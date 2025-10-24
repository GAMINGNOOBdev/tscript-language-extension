using System.Text;
using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Text.RegularExpressions;

namespace TScriptLanguageServer;

public class BufferManager
{
    private readonly ConcurrentDictionary<string, string> _buffers = new();
    private readonly ConcurrentDictionary<string, List<Token>> _tokens = new();

    public void UpdateBuffer(string path, string? buffer)
    {
        if (buffer == null)
            return;

        _buffers.AddOrUpdate(path, buffer, (key, value) => buffer);
        Parse(path, buffer);
    }

    public void UpdateBuffer(string path, string? buffer, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range? range)
    {
        if (buffer == null || range == null)
            return;

        string data = GetBuffer(path);
        StringBuilder builder = new(data);

        int startIndex = GetPositionInBuffer(data, range.Start);
        int endIndex = GetPositionInBuffer(data, range.End);

        builder.Remove(startIndex, endIndex - startIndex);
        builder.Insert(startIndex, buffer);
        string newVal = builder.ToString();
        _buffers.AddOrUpdate(path, data, (key, value) => newVal);
        Parse(path, newVal);
    }

    public string GetBuffer(string path) => _buffers.TryGetValue(path, out var buffer) ? buffer : "";
    public List<Token> GetTokens(string path) => _tokens.TryGetValue(path, out var tokens) ? tokens : [];

    public static int GetPositionInBuffer(string data, Position position)
    {
        int index = 0;
        int lineNumber = 0;

        foreach (string line in data.Split(["\n", "\r\n"], StringSplitOptions.None))
        {
            if (position.Line == lineNumber)
                break;

            index += line.Length;
            if (data[index] == '\r')
                index++;
            if (data[index] == '\n')
                index++;

            lineNumber++;
        }

        index += position.Character;
        return index;
    }


    private void Parse(string path, string text)
    {
        List<Token> tokens = [];
        string[] lines = text.Split([ "\n", "\r\n" ], StringSplitOptions.None);

        HashSet<string> keywords = [ "var", "function", "if", "return", "while" ];
        int lineIndex = 0;

        foreach (string line in lines)
        {
            Regex regex = new(@"""([^""]*)""|(\w+)");

            foreach (Match match in regex.Matches(line))
            {
                TokenType tokenType = TokenType.Variable;
                string value = "";

                Position startPos = new(lineIndex, match.Index);
                Position endPos = new(lineIndex, match.Index + match.Length);

                if (match.Groups[1].Success) // String literal match
                {
                    tokenType = TokenType.String;
                    value = match.Value;
                }
                else if (match.Groups[2].Success) // Word match
                {
                    value = match.Value;
                    if (keywords.Contains(value))
                    {
                        tokenType = TokenType.Keyword;
                    }
                    else if (value.EndsWith("Message")) // Heuristic for function calls
                    {
                        tokenType = TokenType.Function;
                    }
                    else if (double.TryParse(value, out _)) // Simple number check
                    {
                        tokenType = TokenType.Number;
                    }
                }

                if (!string.IsNullOrEmpty(value))
                    tokens.Add(new Token(value, startPos, endPos, tokenType));
            }

            lineIndex++;
        }

        _tokens.AddOrUpdate(path, tokens, (key, value) => tokens);
    }
}
