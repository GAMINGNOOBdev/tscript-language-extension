namespace TScriptLanguageServer.Language;

public class Tokenizer
{
    private readonly List<SourceToken> mSourceTokens = [];

    public SourceTokenIterator Tokens { get => new(mSourceTokens); }

    public Tuple<SourceToken, SourceTokenIterator> GetTokenAtPosition(int line, int pos)
    {
        line++;
        Logging.LogDebug($"Searching for token at line {line}:{pos}");
        SourceTokenIterator iter = Tokens;
        SourceToken tmpResult = new();
        int index = 0;
        while (iter.HasNext)
        {
            if (iter.Current.InRange(line, pos))
            {
                tmpResult = iter.Current;
                index = iter.Index;
            }

            _ = iter.Next;
        }

        Logging.LogDebug($"\tResult: {tmpResult}");
        iter.Index = index-1;
        return new (tmpResult, iter);
    }

    public void ParseFile(string filedata)
    {
        string[] lines = filedata.Split(["\n", "\r\n"], StringSplitOptions.None);
        ParseFile(lines);
    }

    public void ParseFile(string[] lines)
    {
        if (mSourceTokens.Count > 0)
            mSourceTokens.Clear();

        int lineNumber = 0;
        foreach (string line in lines)
        {
            lineNumber++;

            if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                continue;

            if (line.Contains("#type="))
            {
                mSourceTokens.Add(new(SourceTokenType.TEXT, line.Trim()));
                continue;
            }

            if (line.StartsWith('#'))
                continue;

            ParseLine(line, lineNumber);
        }
    }

    private void ParseLine(string line, int lineNumber)
    {
        if (line.Length < 1)
            return;

        int start = line.Length - line.TrimStart().Length;
        int end = start;
        char[] tmpStr = ['\0','\0'];
        bool inString = false;
        char lastStringChar = '\0';
        char chr;

        if (line.TrimStart().StartsWith('#'))
            return;

        if (line.Length < 2)
        {
            ParseChar(line[0], lineNumber, 0);
            return;
        }

        try
        {
            for (int index = start; index < line.Length; index++)
            {
                chr = line[index];
                tmpStr[0] = chr;

                if (chr == '\\' && inString && index < line.Length - 1)
                {
                    index++;
                    end = index;
                    continue;
                }

                if (char.IsWhiteSpace(chr) && !inString)
                {
                    end++;
                    ParseBuffer(line[start..end], lineNumber, inString, start);
                    start = index + 1;
                    continue;
                }

                if (chr == lastStringChar && inString)
                    inString = false;

                if (!inString)
                {
                    int specialCharIndex = Specifications.IsSpecialChar(chr);
                    if (specialCharIndex != -1)
                    {
                        if (chr == '"' && lastStringChar == '\0')
                        {
                            inString = true;
                            lastStringChar = chr;
                        }

                        end++;
                        string buffer = line[start..end];
                        if (!string.IsNullOrEmpty(buffer) && !string.IsNullOrWhiteSpace(buffer))
                            if (!buffer.StartsWith($"{tmpStr[0]}{tmpStr[1]}"))
                                ParseBuffer(buffer, lineNumber, !inString && chr == '"' && lastStringChar != 0, start);
                        start = index + 1;

                        if (!inString && chr == '"' && lastStringChar != '\0')
                            lastStringChar = '\0';

                        int specialOperatorIndex = -1;
                        if (mSourceTokens.Count > 0)
                        {
                            SourceToken lastToken = mSourceTokens.Last();
                            if (lastToken.Name.Length == 0)
                                return;
                            specialOperatorIndex = Specifications.IsSpecialOperator(lastToken.Name[0], chr);
                            if (Specifications.IsSpecialChar(lastToken.Name[0]) != -1 && specialOperatorIndex != -1)
                            {
                                mSourceTokens.RemoveAt(mSourceTokens.Count - 1);
                                SourceToken token = new(
                                    Specifications.SpecialOperators[specialOperatorIndex].Type,
                                    Specifications.SpecialOperators[specialOperatorIndex].Name,
                                    lineNumber,
                                    line.IndexOf(Specifications.SpecialOperators[specialOperatorIndex].Name)
                                );
                                mSourceTokens.Add(token);
                            }
                        }
                        if (specialOperatorIndex == -1)
                        {
                            SourceToken token = new(
                                Specifications.SpecialCharacters[specialCharIndex].Type,
                                Specifications.SpecialCharacters[specialCharIndex].Name,
                                lineNumber,
                                line.IndexOf(Specifications.SpecialCharacters[specialCharIndex].Name)
                            );
                            mSourceTokens.Add(token);
                        }
                    }
                }

                end = index;
            }

            if (start == 0 && end == line.Length - 1)
                ParseBuffer(line, lineNumber, inString, 0);
            if (start != 0 && end == line.Length - 1)
                ParseBuffer(line.Substring(start, end - start + 1), lineNumber, inString, start);
        }
        catch (Exception e)
        {
            Logging.LogError($"couldn't parse line '{line}'({lineNumber} with idx {start}:{end}): {e}");
        }
    }

    private void ParseBuffer(string buffer, int lineNumber, bool inString, int startpos)
    {
        if (!inString)
            buffer = buffer.Trim();

        if (string.IsNullOrEmpty(buffer) || string.IsNullOrWhiteSpace(buffer))
            return;

        if (buffer.Length < 2)
        {
            ParseChar(buffer[0], lineNumber, startpos);
            return;
        }

        string contents = buffer;
        SourceTokenType type = SourceTokenType.TEXT;
        int keywordIndex = Specifications.IsKeyword(buffer);
        
        if (keywordIndex != -1)
        {
            contents = Specifications.Keywords[keywordIndex].Name;
            type = Specifications.Keywords[keywordIndex].Type;
        }
        else if (Specifications.IsNumeric(buffer))
            type = SourceTokenType.NUMBER;
        else
            contents = Specifications.ConnectEscapeSequences(buffer);

        mSourceTokens.Add(new(type, contents, lineNumber, startpos));
    }

    private void ParseChar(char chr, int lineNumber, int startpos)
    {
        if (char.IsDigit(chr))
        {
            mSourceTokens.Add(new(SourceTokenType.NUMBER, $"{chr}", lineNumber, startpos));
            return;
        }
        int specialCharIndex = Specifications.IsSpecialChar(chr);

        if (specialCharIndex != -1)
        {
            SpecialCharacter specialchr = Specifications.SpecialCharacters[specialCharIndex];
            mSourceTokens.Add(new(specialchr.Type, specialchr.Name, lineNumber, startpos));
            return;
        }

        mSourceTokens.Add(new(SourceTokenType.TEXT, $"{chr}", lineNumber, startpos));
    }
}