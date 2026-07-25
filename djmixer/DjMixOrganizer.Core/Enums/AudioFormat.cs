// File location: DjMixOrganizer.Core/Enums/AudioFormat.cs
//
// A plain enum. Nothing fancy — but worth noting WHY it lives in Core:
// both the Data layer (which needs to know how to parse each format) and
// the App layer (which might show a format icon in the UI) depend on this
// same definition. Putting shared vocabulary in Core, with zero
// dependencies of its own, is what makes it safe for every other project
// to reference it without creating circular references.

namespace DjMixOrganizer.Core.Enums;

public enum AudioFormat
{
    Mp3,
    Wav,
    Flac,
    Aiff
}
