using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace af0.Adblock
{
    public class Glob
    {
        abstract class Token
        {
            public static Token Create(char t)
            {
                switch (t)
                {
                    case '?':
                        return new Qmark();
                    case '*':
                        return new Star();
                    default:
                        throw new ArgumentException("t");
                }
            }
        }
        class Star : Token { }
        class Qmark : Token { }
        class Str : Token { public string Pattern;}

        Token[] _pattern;

        public Glob(string pattern)
        {
            List<Token> tokens = new List<Token>();
            StringBuilder sb = new StringBuilder();
            bool escaped = false;

            foreach (char c in pattern)
            {
                switch (c)
                {
                    case '\\':
                        if (escaped)
                        {
                            sb.Append('\\');
                            escaped = false;
                        }
                        else
                            escaped = true;
                        continue;
                    case '?':
                    case '*':
                        if (escaped)
                        {
                            sb.Append(c);
                            escaped = false;
                        }
                        else
                        {
                            if (sb.Length > 0)
                            {
                                tokens.Add(new Str { Pattern = sb.ToString() });
                                sb.Remove(0, sb.Length);
                            }
                            tokens.Add(Token.Create(c));
                        }
                        continue;
                    default:
                        if (escaped) // if they use a \ by itself, just treat it by itself
                        {
                            sb.Append('\\');
                            escaped = false;
                        }
                        sb.Append(c);
                        continue;
                }
            }
            if (sb.Length > 0)
                tokens.Add(new Str { Pattern = sb.ToString() });
            _pattern = tokens.ToArray();
            if (_pattern.Length == 0)
                throw new ArgumentException("Must specify nonempty pattern");
        }

        // Like regular string.StartsWith, but case insensitive
        private static bool StartsWith(string haystack, string needle)
        {
            int i = 0;
            foreach (char c in haystack)
            {
                if (i == needle.Length)
                    return true;
                else if (Char.ToUpperInvariant(c) == Char.ToUpperInvariant(needle[i]))
                    i++;
                else
                    return false;
            }
            return i == needle.Length; ;
        }

        // Like regular string.IndexOf, only can be given a start index. Perhaps we should do boyer-moore here?
        private static int IndexOf(string haystack, string needle, int startidx)
        {
            if (startidx < 0)
                throw new ArgumentException("startidx");
            int i = 0;
            for (; startidx < haystack.Length; startidx++)
            {
                if (i == needle.Length)
                    return startidx - i;
                else if (Char.ToUpperInvariant(haystack[startidx]) == Char.ToUpperInvariant(needle[i]))
                    i++;
                else
                    i = 0;
            }
            if (i == needle.Length)
                return startidx - i;
            return -1;
        }

        public bool IsMatch(string haystack)
        {
            int i = 0;
            bool lastWasStar = false;
            foreach (Token tok in _pattern)
            {
                if (tok is Star)
                {
                    lastWasStar = true;
                }
                else if (tok is Qmark) // just advance 1 character, and leave lastWasStar alone, so that it can be consumed later...
                {
                    i += 1;
                    if (i > haystack.Length)
                        return false;
                }
                else if (tok is Str)
                {
                    if (lastWasStar)
                    {
                        lastWasStar = false;
                        int newi = IndexOf(haystack, (tok as Str).Pattern, i);
                        if (newi == -1)
                        {
                            return false;
                        }
                        else
                        {
                            i = newi + (tok as Str).Pattern.Length;
                        }
                    }
                    else if (!StartsWith(haystack, (tok as Str).Pattern))
                    {
                        return false;
                    }
                    else
                    {
                        i += (tok as Str).Pattern.Length;
                    }
                }
            }
            return lastWasStar || i == haystack.Length;
        }
    }
}
