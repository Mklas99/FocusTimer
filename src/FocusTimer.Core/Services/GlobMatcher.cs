namespace FocusTimer.Core.Services;

/// <summary>Case-insensitive wildcard matching where only <c>*</c> and <c>?</c> are special.</summary>
public static class GlobMatcher
{
    /// <summary>Matches the whole value against a pattern without regular-expression semantics.</summary>
    /// <param name="pattern">The pattern containing optional <c>*</c> and <c>?</c> wildcards.</param>
    /// <param name="value">The value to test.</param>
    /// <returns>True when the pattern matches the entire value.</returns>
    public static bool IsMatch(string pattern, string value)
    {
        var p = 0;
        var v = 0;
        var starIndex = -1;
        var resumeValue = 0;
        while (v < value.Length)
        {
            if (p < pattern.Length && pattern[p] == '*')
            {
                starIndex = p++;
                resumeValue = v;
            }
            else if (p < pattern.Length && (pattern[p] == '?' || CharEquals(pattern[p], value[v])))
            {
                p++;
                v++;
            }
            else if (starIndex >= 0)
            {
                p = starIndex + 1;
                v = ++resumeValue;
            }
            else
            {
                return false;
            }
        }

        while (p < pattern.Length && pattern[p] == '*')
        {
            p++;
        }

        return p == pattern.Length;
    }

    private static bool CharEquals(char a, char b) => char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
}
