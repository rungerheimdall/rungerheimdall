using System;

namespace RungerHeimdall.Engine.Configuration;

public sealed class NamespacePattern
{
    private readonly string _text;
    private readonly bool _prefix;

    public NamespacePattern(string pattern)
    {
        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        if (pattern.Length == 0)
        {
            throw new ArgumentException("Pattern cannot be empty.", nameof(pattern));
        }

        if (pattern.EndsWith(".*", StringComparison.Ordinal))
        {
            _text = pattern.Substring(0, pattern.Length - 2);
            _prefix = true;
        }
        else
        {
            _text = pattern;
            _prefix = false;
        }
    }

    public string Value => _prefix ? _text + ".*" : _text;

    public bool Matches(string namespaceName)
    {
        if (namespaceName is null)
        {
            return false;
        }

        if (_prefix)
        {
            return namespaceName.Equals(_text, StringComparison.Ordinal)
                || namespaceName.StartsWith(_text + ".", StringComparison.Ordinal);
        }

        return namespaceName.Equals(_text, StringComparison.Ordinal);
    }

    public int GetMatchLength(string namespaceName)
    {
        if (!Matches(namespaceName))
        {
            return -1;
        }

        return _prefix ? _text.Length + 1 : _text.Length;
    }
}