using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RungerHeimdall.Engine.Json;

internal sealed class JsonParser
{
    private readonly string _text;
    private int _position;

    public JsonParser(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public JsonValue ParseDocument()
    {
        SkipWhitespace();
        var value = ParseValue();

        SkipWhitespace();
        if (_position != _text.Length)
        {
            throw Error("Unexpected trailing content");
        }

        return value;
    }

    private JsonValue ParseValue()
    {
        if (_position >= _text.Length)
        {
            throw Error("Unexpected end of JSON");
        }

        var current = _text[_position];
        switch (current)
        {
            case '{':
                return ParseObject();
            case '[':
                return ParseArray();
            case '"':
                return new JsonString(ParseString());
            case 't':
                ExpectLiteral("true");
                return new JsonBoolean(true);
            case 'f':
                ExpectLiteral("false");
                return new JsonBoolean(false);
            case 'n':
                ExpectLiteral("null");
                return JsonNull.Instance;
            default:
                return ParseNumber();
        }
    }

    private JsonObject ParseObject()
    {
        _position++;
        SkipWhitespace();

        var properties = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
        if (TryConsume('}'))
        {
            return new JsonObject(properties);
        }

        while (true)
        {
            SkipWhitespace();
            if (_position >= _text.Length || _text[_position] != '"')
            {
                throw Error("Expected a property name");
            }

            var name = ParseString();
            SkipWhitespace();
            if (!TryConsume(':'))
            {
                throw Error("Expected ':' after a property name");
            }

            SkipWhitespace();
            properties[name] = ParseValue();
            SkipWhitespace();

            if (TryConsume(','))
            {
                continue;
            }

            if (TryConsume('}'))
            {
                return new JsonObject(properties);
            }

            throw Error("Expected ',' or '}'");
        }
    }

    private JsonArray ParseArray()
    {
        _position++;
        SkipWhitespace();

        var items = new List<JsonValue>();
        if (TryConsume(']'))
        {
            return new JsonArray(items);
        }

        while (true)
        {
            items.Add(ParseValue());
            SkipWhitespace();

            if (TryConsume(','))
            {
                SkipWhitespace();
                continue;
            }

            if (TryConsume(']'))
            {
                return new JsonArray(items);
            }

            throw Error("Expected ',' or ']'");
        }
    }

    private string ParseString()
    {
        _position++;
        var builder = new StringBuilder();

        while (_position < _text.Length)
        {
            var current = _text[_position++];
            if (current == '"')
            {
                return builder.ToString();
            }

            if (current != '\\')
            {
                builder.Append(current);
                continue;
            }

            if (_position >= _text.Length)
            {
                throw Error("Unterminated escape sequence");
            }

            var escape = _text[_position++];
            switch (escape)
            {
                case '"':
                    builder.Append('"');
                    break;
                case '\\':
                    builder.Append('\\');
                    break;
                case '/':
                    builder.Append('/');
                    break;
                case 'b':
                    builder.Append('\b');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'u':
                    builder.Append(ParseUnicode());
                    break;
                default:
                    throw Error($"Unsupported escape '\\{escape}'");
            }
        }

        throw Error("Unterminated string");
    }

    private char ParseUnicode()
    {
        if (_position + 4 > _text.Length)
        {
            throw Error("Incomplete unicode escape");
        }

        var hex = _text.Substring(_position, 4);
        _position += 4;

        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
        {
            throw Error("Invalid unicode escape");
        }

        return (char)code;
    }

    private JsonNumber ParseNumber()
    {
        var start = _position;

        if (TryConsume('-'))
        {
            if (_position >= _text.Length)
            {
                throw Error("Incomplete number");
            }
        }

        while (_position < _text.Length && char.IsDigit(_text[_position]))
        {
            _position++;
        }

        if (_position < _text.Length && _text[_position] == '.')
        {
            _position++;
            while (_position < _text.Length && char.IsDigit(_text[_position]))
            {
                _position++;
            }
        }

        if (_position < _text.Length && (_text[_position] == 'e' || _text[_position] == 'E'))
        {
            _position++;
            if (_position < _text.Length && (_text[_position] == '+' || _text[_position] == '-'))
            {
                _position++;
            }

            while (_position < _text.Length && char.IsDigit(_text[_position]))
            {
                _position++;
            }
        }

        var token = _text.Substring(start, _position - start);
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw Error($"Invalid number '{token}'");
        }

        return new JsonNumber(value);
    }

    private void ExpectLiteral(string literal)
    {
        if (_position + literal.Length > _text.Length
            || string.CompareOrdinal(_text, _position, literal, 0, literal.Length) != 0)
        {
            throw Error($"Expected '{literal}'");
        }

        _position += literal.Length;
    }

    private void SkipWhitespace()
    {
        while (_position < _text.Length)
        {
            var current = _text[_position];
            if (current == ' ' || current == '\t' || current == '\r' || current == '\n')
            {
                _position++;
                continue;
            }

            break;
        }
    }

    private bool TryConsume(char expected)
    {
        if (_position < _text.Length && _text[_position] == expected)
        {
            _position++;
            return true;
        }

        return false;
    }

    private FormatException Error(string message)
    {
        return new FormatException($"{message} at position {_position}.");
    }
}