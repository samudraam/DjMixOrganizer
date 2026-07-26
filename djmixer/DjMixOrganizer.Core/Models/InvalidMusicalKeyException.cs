namespace DjMixOrganizer.Core.Models;

/// <summary>
/// Thrown when song upload/update supplies a key that is not a classical letter key.
/// </summary>
public sealed class InvalidMusicalKeyException : Exception
{
    /// <summary>Gets the raw key text that failed validation.</summary>
    public string AttemptedKey { get; }

    /// <summary>Creates an exception for a rejected musical-key string.</summary>
    /// <param name="attemptedKey">The raw value that could not be normalized.</param>
    public InvalidMusicalKeyException(string attemptedKey)
        : base(
            $"'{attemptedKey}' is not a valid letter musical key. " +
            "Use forms like C, Am, or F#m — not Camelot (8A) or Open Key (6m).")
    {
        AttemptedKey = attemptedKey;
    }
}
