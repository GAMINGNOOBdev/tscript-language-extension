using System.Text;
using System.Collections.Concurrent;
using TScriptLanguageServer.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace TScriptLanguageServer;

public class Buffer
{
    public string Contents { get; set; } = "";
    public Tokenizer Tokenizer { get; private set; } = new();
    public TokenParser TokenParser { get; private set; } = new();
}

public class BufferManager
{
    private readonly ConcurrentDictionary<string, Buffer> _buffers = new();

    public void UpdateBuffer(string path, string? buffer)
    {
        if (buffer == null)
            return;

        Buffer b = new()
        {
            Contents = buffer
        };
        b.Tokenizer.ParseFile(b.Contents);
        b.TokenParser.Parse(b.Tokenizer);

        _buffers.AddOrUpdate(path, b, (key, value) => b);
    }

    public void UpdateBuffer(string path, string? buffer, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range? range)
    {
        if (buffer == null || range == null)
            return;

        Buffer? data = GetBuffer(path);
        if (data == null)
            return;

        StringBuilder builder = new(data.Contents);

        int startIndex = GetPositionInBuffer(data.Contents, range.Start);
        int endIndex = GetPositionInBuffer(data.Contents, range.End);

        builder.Remove(startIndex, endIndex - startIndex);
        builder.Insert(startIndex, buffer);
        string newVal = builder.ToString();

        Buffer b = new()
        {
            Contents = newVal
        };
        b.Tokenizer.ParseFile(b.Contents);
        b.TokenParser.Parse(b.Tokenizer);

        _buffers.AddOrUpdate(path, data, (key, value) => b);
    }

    public Buffer? GetBuffer(string path) => _buffers.TryGetValue(path, out var buffer) ? buffer : null;

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
}
