using System.Collections;
using DjMixOrganizer.Core.Models;

namespace DjMixOrganizer.App.Controls;

public partial class TrackListPanel : ContentView
{
    public static readonly BindableProperty TracksSourceProperty =
        BindableProperty.Create(nameof(TracksSource), typeof(IEnumerable), typeof(TrackListPanel));

    public static readonly BindableProperty SelectedTrackProperty =
        BindableProperty.Create(nameof(SelectedTrack), typeof(Track), typeof(TrackListPanel),
            defaultBindingMode: BindingMode.TwoWay);

    public TrackListPanel()
    {
        InitializeComponent();
    }

    public IEnumerable? TracksSource
    {
        get => (IEnumerable?)GetValue(TracksSourceProperty);
        set => SetValue(TracksSourceProperty, value);
    }

    public Track? SelectedTrack
    {
        get => (Track?)GetValue(SelectedTrackProperty);
        set => SetValue(SelectedTrackProperty, value);
    }
}
