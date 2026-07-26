using System.Text.RegularExpressions;

namespace DjMixOrganizer.Core.Models;

/// <summary>
/// Canonical classical letter key (e.g. C, Am, F#m). Rejects Camelot, Open Key, and junk.
/// </summary>
public readonly partial record struct MusicalKey
{
    /// <summary>Preferred enharmonic spellings used when persisting and displaying keys.</summary>
    private static readonly string[] CanonicalRoots =
    [
        "C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B",
    ];

    /// <summary>Maps alternate spellings onto <see cref="CanonicalRoots"/>.</summary>
    private static readonly Dictionary<string, string> RootAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["C"] = "C",
            ["B#"] = "C",
            ["C#"] = "Db",
            ["DB"] = "Db",
            ["D"] = "D",
            ["D#"] = "Eb",
            ["EB"] = "Eb",
            ["E"] = "E",
            ["FB"] = "E",
            ["F"] = "F",
            ["E#"] = "F",
            ["F#"] = "F#",
            ["GB"] = "F#",
            ["G"] = "G",
            ["G#"] = "Ab",
            ["AB"] = "Ab",
            ["A"] = "A",
            ["A#"] = "Bb",
            ["BB"] = "Bb",
            ["B"] = "B",
            ["CB"] = "B",
        };

    /// <summary>All 24 canonical keys (12 major + 12 minor) for UI pickers.</summary>
    public static IReadOnlyList<string> All { get; } = BuildAll();

    private static readonly HashSet<string> CanonicalValues =
        new(All, StringComparer.Ordinal);

    /// <summary>Gets the canonical letter form, e.g. <c>Am</c> or <c>F#</c>.</summary>
    public string Value { get; }

    /// <summary>Creates a key from an already-canonical value.</summary>
    /// <param name="value">Canonical letter key such as <c>C</c> or <c>F#m</c>.</param>
    public MusicalKey(string value)
    {
        if (!CanonicalValues.Contains(value))
        {
            throw new ArgumentException(
                $"'{value}' is not a canonical musical key.",
                nameof(value));
        }

        Value = value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>
    /// Parses free-form key text into a canonical letter key.
    /// Accepts variants like <c>am</c>, <c>A minor</c>, <c>Cmaj</c>; rejects Camelot and junk.
    /// </summary>
    /// <param name="input">Raw key text from UI, ID3, or import.</param>
    /// <param name="key">Parsed canonical key when the method returns <c>true</c>.</param>
    /// <returns><c>true</c> when <paramref name="input"/> is a recognizable letter key.</returns>
    public static bool TryParse(string? input, out MusicalKey key)
    {
        key = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var collapsed = WhitespaceRegex().Replace(input.Trim(), " ");

        if (CamelotOrOpenKeyRegex().IsMatch(collapsed))
        {
            return false;
        }

        if (!TryNormalize(collapsed, out var canonical))
        {
            return false;
        }

        key = new MusicalKey(canonical);
        return true;
    }

    /// <summary>
    /// Parses a key that is already known to be valid (e.g. a DB column value).
    /// </summary>
    /// <param name="input">Canonical or parseable letter key.</param>
    /// <returns>The parsed <see cref="MusicalKey"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="input"/> cannot be parsed.</exception>
    public static MusicalKey Parse(string input)
    {
        if (TryParse(input, out var key))
        {
            return key;
        }

        throw new FormatException($"'{input}' is not a valid musical key.");
    }

    private static bool TryNormalize(string input, out string canonical)
    {
        canonical = string.Empty;

        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length is 0 or > 2)
        {
            return false;
        }

        string rootToken;
        bool isMinor;

        if (tokens.Length == 2)
        {
            rootToken = tokens[0];
            var mode = tokens[1].ToLowerInvariant();
            if (mode is "major" or "maj")
            {
                isMinor = false;
            }
            else if (mode is "minor" or "min")
            {
                isMinor = true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            var token = tokens[0];
            var lower = token.ToLowerInvariant();

            if (lower.EndsWith("major", StringComparison.Ordinal) ||
                lower.EndsWith("maj", StringComparison.Ordinal))
            {
                isMinor = false;
                rootToken = TrimModeSuffix(token, lower.EndsWith("major", StringComparison.Ordinal) ? 5 : 3);
            }
            else if (lower.EndsWith("minor", StringComparison.Ordinal) ||
                     lower.EndsWith("min", StringComparison.Ordinal))
            {
                isMinor = true;
                rootToken = TrimModeSuffix(token, lower.EndsWith("minor", StringComparison.Ordinal) ? 5 : 3);
            }
            else if (TryResolveRoot(token, out _))
            {
                // Whole token is already a root ("C", "Bb", "F#") — major.
                isMinor = false;
                rootToken = token;
            }
            else if (lower.EndsWith('m') && lower.Length > 1)
            {
                // "Am", "F#m", "Bbm" — strip the trailing minor marker.
                isMinor = true;
                rootToken = token[..^1];
            }
            else
            {
                isMinor = false;
                rootToken = token;
            }
        }

        if (string.IsNullOrWhiteSpace(rootToken))
        {
            return false;
        }

        // German H = B is intentionally rejected; stick to A–G letter names.
        if (rootToken.Equals("H", StringComparison.OrdinalIgnoreCase) ||
            rootToken.Equals("H#", StringComparison.OrdinalIgnoreCase) ||
            rootToken.Equals("Hb", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryResolveRoot(rootToken, out var canonicalRoot))
        {
            return false;
        }

        canonical = isMinor ? canonicalRoot + "m" : canonicalRoot;
        return CanonicalValues.Contains(canonical);
    }

    private static bool TryResolveRoot(string rootToken, out string canonicalRoot)
    {
        var aliasKey = rootToken
            .Replace("♯", "#", StringComparison.Ordinal)
            .Replace("♭", "b", StringComparison.Ordinal);

        return RootAliases.TryGetValue(aliasKey, out canonicalRoot!);
    }

    private static string TrimModeSuffix(string token, int suffixLength)
    {
        if (token.Length <= suffixLength)
        {
            return string.Empty;
        }

        return token[..^suffixLength];
    }

    private static List<string> BuildAll()
    {
        var keys = new List<string>(24);
        foreach (var root in CanonicalRoots)
        {
            keys.Add(root);
        }

        foreach (var root in CanonicalRoots)
        {
            keys.Add(root + "m");
        }

        return keys;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    /// <summary>Camelot (8A) or Open Key wheel codes (6m / 12d).</summary>
    [GeneratedRegex(@"^\d{1,2}[ABabDdMm]$")]
    private static partial Regex CamelotOrOpenKeyRegex();
}
