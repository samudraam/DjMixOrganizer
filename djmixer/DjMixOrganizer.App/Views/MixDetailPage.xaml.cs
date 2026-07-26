// File location: DjMixOrganizer.App/Views/MixDetailPage.xaml.cs
//
// TEACHING NOTES
// ---------------
// Dragging a node is done entirely with View.TranslationX/TranslationY —
// a visual transform the renderer applies on top of normal layout — rather
// than by re-binding AbsoluteLayout.LayoutBounds on every pointer-move
// event. Rebinding on every frame would mean re-running the whole binding
// pipeline (and, since TrackNode isn't observable on Position by design,
// wouldn't even work without extra plumbing). Translation is cheap, GPU
// side, and exactly what it's for. TrackNode.Position only gets updated
// once, when the drag completes — that's the "commit point," same idea as
// debouncing a text field instead of writing to a database on every
// keystroke.
//
// The GraphicsView connector lines are drawn by asking, for each pair of
// adjacent nodes, "where is this node's Border right now" — Position plus
// whatever TranslationX/TranslationY is currently in flight. That's why
// Draw() needs a live reference to each node's actual Border (_nodeBorders,
// populated via each card's Loaded event) instead of only reading the
// ViewModel's Nodes collection.

using DjMixOrganizer.App.ViewModels;
using DjMixOrganizer.Core.Models;

namespace DjMixOrganizer.App.Views;

public partial class MixDetailPage : ContentPage, IDrawable
{
    private const double CardWidth = Converters.CanvasPositionToBoundsConverter.CardWidth;
    private const double CardHeight = Converters.CanvasPositionToBoundsConverter.CardHeight;

    private readonly MixDetailViewModel _viewModel;
    private readonly Dictionary<Guid, Border> _nodeBorders = [];

    public MixDetailPage(MixDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        ConnectionsCanvas.Drawable = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadAvailableTracksCommand.Execute(null);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // Registers each node card's Border the moment it's rendered, so Draw()
    // has something to read a live TranslationX/Y from — including for
    // nodes that were just added and have never been dragged yet.
    private void OnNodeCardLoaded(object? sender, EventArgs e)
    {
        if (sender is Border { BindingContext: TrackNode node } border)
        {
            _nodeBorders[node.Id] = border;
            ConnectionsCanvas.Invalidate();
        }
    }

    private void OnNodePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[Drag] fired: sender={sender?.GetType().Name} status={e.StatusType} totalX={e.TotalX} totalY={e.TotalY}");

        if (sender is not PanGestureRecognizer { Parent: Border border } ||
            border.BindingContext is not TrackNode node)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Drag] bailing early: parentType={(sender as PanGestureRecognizer)?.Parent?.GetType().Name ?? "null"}");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[Drag] node={node.Track.Title}");

        if (e.StatusType == GestureStatus.Started)
        {
            _viewModel.SelectedNode = node;
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                border.TranslationX = e.TotalX;
                border.TranslationY = e.TotalY;
                ConnectionsCanvas.Invalidate();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                node.Position = new CanvasPosition(
                    node.Position.X + border.TranslationX,
                    node.Position.Y + border.TranslationY);
                border.TranslationX = 0;
                border.TranslationY = 0;
                AbsoluteLayout.SetLayoutBounds(border, new Rect(node.Position.X, node.Position.Y, CardWidth, CardHeight));
                ConnectionsCanvas.Invalidate();
                break;
        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var nodes = _viewModel.Nodes;
        for (var i = 0; i < nodes.Count - 1; i++)
        {
            if (!_nodeBorders.TryGetValue(nodes[i].Id, out var fromBorder) ||
                !_nodeBorders.TryGetValue(nodes[i + 1].Id, out var toBorder))
            {
                continue; // a card hasn't been laid out yet — skip this frame
            }

            var from = CenterOf(nodes[i], fromBorder);
            var to = CenterOf(nodes[i + 1], toBorder);

            canvas.StrokeColor = Color.FromArgb(nodes[i].AccentColorHex);
            canvas.StrokeSize = 3;
            canvas.DrawLine(from, to);

            canvas.FillColor = Color.FromArgb(nodes[i].AccentColorHex);
            canvas.FillCircle(from, 6);
            canvas.FillColor = Color.FromArgb(nodes[i + 1].AccentColorHex);
            canvas.FillCircle(to, 6);
        }
    }

    private static PointF CenterOf(TrackNode node, Border border) => new(
        (float)(node.Position.X + border.TranslationX + CardWidth / 2),
        (float)(node.Position.Y + border.TranslationY + CardHeight / 2));
}
