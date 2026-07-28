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
    private const double MinCardHeight = 180;

    private readonly MixDetailViewModel _viewModel;
    private readonly Dictionary<Guid, Border> _nodeBorders = [];

    // Card height is resized by dragging the corner handle (see the
    // resize-handle Label in the DataTemplate). Like Position, this is
    // presentation state only — it isn't part of TrackNode/persisted on
    // save — so it lives here rather than on the model, keyed by node Id
    // so it survives the card being re-templated (e.g. when Nodes changes).
    private readonly Dictionary<Guid, double> _nodeCardHeights = [];
    private double _resizeStartHeight;

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

            if (_nodeCardHeights.TryGetValue(node.Id, out var savedHeight))
            {
                AbsoluteLayout.SetLayoutBounds(border, new Rect(node.Position.X, node.Position.Y, CardWidth, savedHeight));
            }
            else
            {
                _nodeCardHeights[node.Id] = CardHeight;
            }

            ConnectionsCanvas.Invalidate();
        }
    }

    // Fires from the small corner handle inside each node card, kept
    // separate from OnNodePanUpdated (which moves the whole card) so
    // dragging the handle resizes instead of dragging the card.
    private void OnResizeHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[Resize] fired: sender={sender?.GetType().Name} status={e.StatusType} totalX={e.TotalX} totalY={e.TotalY}");

        if (sender is not PanGestureRecognizer { Parent: View handle })
        {
            System.Diagnostics.Debug.WriteLine("[Resize] bailing early: sender is not a PanGestureRecognizer with a Parent view");
            return;
        }

        if (FindAncestorBorder(handle) is not { BindingContext: TrackNode node } border)
        {
            System.Diagnostics.Debug.WriteLine("[Resize] bailing early: no ancestor Border with a TrackNode BindingContext");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[Resize] node={node.Track.Title}");

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _resizeStartHeight = _nodeCardHeights.TryGetValue(node.Id, out var height) ? height : CardHeight;
                System.Diagnostics.Debug.WriteLine($"[Resize] started at height={_resizeStartHeight}");
                break;

            case GestureStatus.Running:
                var liveHeight = Math.Max(MinCardHeight, _resizeStartHeight + e.TotalY);
                AbsoluteLayout.SetLayoutBounds(border, new Rect(node.Position.X, node.Position.Y, CardWidth, liveHeight));
                ConnectionsCanvas.Invalidate();
                System.Diagnostics.Debug.WriteLine($"[Resize] running, liveHeight={liveHeight}");
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _nodeCardHeights[node.Id] = Math.Max(MinCardHeight, _resizeStartHeight + e.TotalY);
                System.Diagnostics.Debug.WriteLine($"[Resize] committed height={_nodeCardHeights[node.Id]}");
                break;
        }
    }

    private static Border? FindAncestorBorder(Element element)
    {
        for (var current = element.Parent; current is not null; current = current.Parent)
        {
            if (current is Border border)
            {
                return border;
            }
        }

        return null;
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
                var cardHeight = _nodeCardHeights.TryGetValue(node.Id, out var height) ? height : CardHeight;
                AbsoluteLayout.SetLayoutBounds(border, new Rect(node.Position.X, node.Position.Y, CardWidth, cardHeight));
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

    private PointF CenterOf(TrackNode node, Border border)
    {
        var height = _nodeCardHeights.TryGetValue(node.Id, out var h) ? h : CardHeight;
        return new PointF(
            (float)(node.Position.X + border.TranslationX + CardWidth / 2),
            (float)(node.Position.Y + border.TranslationY + height / 2));
    }
}
