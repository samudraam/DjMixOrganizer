// File location: DjMixOrganizer.App/Converters/MixTrackFormattingConverters.cs
//
// TEACHING NOTES
// ---------------
// A Mix can hold any number of tracks, so building the colorized
// " x "-joined title isn't a fixed number of XAML <Span>s — it has to be
// built in code from however many tracks the Mix actually has. An
// IValueConverter is MAUI's way to run a small transform between a bound
// model value and what the View displays, without putting presentation
// logic (colors, joining strings) into the Mix model itself. Same
// separation-of-concerns reasoning as everywhere else in Core: Mix doesn't
// know it's being displayed, let alone in what color.

using System.Globalization;
using DjMixOrganizer.Core.Models;

namespace DjMixOrganizer.App.Converters;

public class MixTitleToFormattedStringConverter : IValueConverter
{
    private static readonly Color[] NodeColors =
    [
        Color.FromArgb("#C21E7A"), // NodeMagenta
        Color.FromArgb("#1E3FE0"), // NodeBlue
        Color.FromArgb("#1E9E3C"), // NodeGreen
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var formatted = new FormattedString();

        // Cards render on a white background (see MixListPage.xaml), so
        // separator/fallback text needs a dark color — not the White used
        // elsewhere in this app for dark-panel text.
        var neutralTextColor = Color.FromArgb("#404040"); // Gray600

        if (value is not Mix { Tracks.Count: > 0 } mix)
        {
            formatted.Spans.Add(new Span { Text = "Untitled Mix", TextColor = neutralTextColor });
            return formatted;
        }

        for (var i = 0; i < mix.Tracks.Count; i++)
        {
            if (i > 0)
            {
                formatted.Spans.Add(new Span { Text = " x ", TextColor = neutralTextColor });
            }

            formatted.Spans.Add(new Span
            {
                Text = mix.Tracks[i].Track.Title,
                TextColor = NodeColors[i % NodeColors.Length],
                FontAttributes = FontAttributes.Bold,
            });
        }

        return formatted;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class MixArtistsToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Mix { Tracks.Count: > 0 } mix)
        {
            return string.Empty;
        }

        return string.Join(" x ", mix.Tracks.Select(t => t.Track.Artist ?? "Artist"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
