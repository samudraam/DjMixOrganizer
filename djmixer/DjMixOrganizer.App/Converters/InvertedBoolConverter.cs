using System.Globalization;

namespace DjMixOrganizer.App.Converters;

/// <summary>Flips a bool for bindings like IsVisible when the ViewModel flag means the opposite.</summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag && !flag;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag && !flag;
}
