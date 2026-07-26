// File location: DjMixOrganizer.App/Converters/MixEditorConverters.cs
//
// TEACHING NOTES
// ---------------
// Three small, single-purpose converters for the Mix editor canvas. Each
// one exists because XAML bindings can't run arbitrary code — a converter
// is the escape hatch for "the model has X, the View needs Y" without
// putting UI logic on the model (CanvasPositionToBoundsConverter,
// HexToColorConverter) or fabricating fake-but-stable per-node data
// (TrackNodeToWaveformBarsConverter).

using System.Globalization;
using DjMixOrganizer.Core.Models;

namespace DjMixOrganizer.App.Converters;

// AbsoluteLayout positions children via a Rect (X, Y, Width, Height), but
// the domain model only needs to know X/Y — Width/Height are a fixed node
// card size, a presentation detail. This converter is what bridges the two
// without teaching TrackNode/CanvasPosition anything about pixel sizes.
public class CanvasPositionToBoundsConverter : IValueConverter
{
    public const double CardWidth = 230;
    public const double CardHeight = 260;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is CanvasPosition pos
            ? new Rect(pos.X, pos.Y, CardWidth, CardHeight)
            : new Rect(0, 0, CardWidth, CardHeight);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// TrackNode.AccentColorHex is a plain string (Core has no reason to know
// about Microsoft.Maui.Graphics.Color) — this converter is the one place
// that string becomes an actual Color for the Border to render.
public class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string hex ? Color.FromArgb(hex) : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// "Static UI over fake data" for the waveform: seeding Random with the
// node's own Guid means the bars are stable across re-renders (the same
// node always looks the same) without storing fabricated waveform data on
// TrackNode itself — this stays entirely a View-layer concern.
public class TrackNodeToWaveformBarsConverter : IValueConverter
{
    private const int BarCount = 28;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TrackNode node)
        {
            return Array.Empty<double>();
        }

        var random = new Random(node.Id.GetHashCode());
        return Enumerable.Range(0, BarCount)
            .Select(_ => (double)random.Next(4, 26))
            .ToArray();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
