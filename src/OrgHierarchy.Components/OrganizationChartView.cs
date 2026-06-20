using System.ComponentModel;
using System.Data;
using System.Drawing.Drawing2D;

namespace OrgHierarchy.Components;

[DefaultEvent(nameof(NodeActivated))]
[DefaultProperty(nameof(DisplayColumn))]
[ToolboxItem(true)]
[Description("Displays organization or personnel hierarchy data as an organization chart.")]
public sealed class OrganizationChartView : UserControl
{
    private const float MinZoom = 0.35f;
    private const float MaxZoom = 2.5f;
    private const int CanvasPadding = 32;

    private readonly ToolStrip _toolStrip = new();
    private readonly ToolStripButton _zoomOutButton = new("Zoom -");
    private readonly ToolStripButton _zoomInButton = new("Zoom +");
    private readonly ToolStripButton _fitButton = new("Fit");
    private readonly ToolStripButton _refreshButton = new("Refresh");
    private readonly ChartCanvasPanel _canvas;

    private readonly List<ChartNode> _layoutRoots = new();
    private List<HierarchyNodeRecord> _records = new();
    private Func<CancellationToken, Task<DataTable>>? _dataLoader;
    private HierarchyNodeRecord? _selectedRecord;
    private SizeF _contentSize = SizeF.Empty;
    private float _zoom = 1f;

    public OrganizationChartView()
    {
        _canvas = new ChartCanvasPanel(this);
        InitializeComponent();
    }

    public event EventHandler<HierarchyNodeEventArgs>? NodeActivated;
    public event EventHandler? RefreshRequested;

    [Category("Data")]
    [DefaultValue("Id")]
    public string IdColumn { get; set; } = "Id";

    [Category("Data")]
    [DefaultValue("ParentId")]
    public string ParentIdColumn { get; set; } = "ParentId";

    [Category("Data")]
    [DefaultValue("DisplayName")]
    public string DisplayColumn { get; set; } = "DisplayName";

    [Category("Data")]
    [DefaultValue("Subtitle")]
    public string SubtitleColumn { get; set; } = "Subtitle";

    [Category("Data")]
    [DefaultValue("Kind")]
    public string KindColumn { get; set; } = "Kind";

    [Category("Data")]
    [DefaultValue("SortOrder")]
    public string SortColumn { get; set; } = "SortOrder";

    [Category("Layout")]
    [DefaultValue(190)]
    public int NodeWidth { get; set; } = 190;

    [Category("Layout")]
    [DefaultValue(72)]
    public int NodeHeight { get; set; } = 72;

    [Category("Layout")]
    [DefaultValue(42)]
    public int HorizontalSpacing { get; set; } = 42;

    [Category("Layout")]
    [DefaultValue(70)]
    public int VerticalSpacing { get; set; } = 70;

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ShowSubtitles { get; set; } = true;

    [Browsable(false)]
    public HierarchyNodeRecord? SelectedRecord => _selectedRecord;

    [Category("Layout")]
    [DefaultValue(1f)]
    public float Zoom
    {
        get => _zoom;
        set
        {
            var next = Math.Max(MinZoom, Math.Min(MaxZoom, value));
            if (Math.Abs(_zoom - next) < 0.001f)
                return;

            _zoom = next;
            UpdateCanvasSize();
            _canvas.Invalidate();
        }
    }

    public void SetDataLoader(Func<CancellationToken, Task<DataTable>> dataLoader)
    {
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (_dataLoader == null)
            return;

        UseWaitCursor = true;
        try
        {
            var table = await _dataLoader(cancellationToken).ConfigureAwait(true);
            LoadFromDataTable(table);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    public void LoadFromDataTable(DataTable table)
    {
        var records = DataTableHierarchyMapper.FromDataTable(
            table,
            IdColumn,
            ParentIdColumn,
            DisplayColumn,
            SubtitleColumn,
            KindColumn,
            SortColumn);

        LoadFromRecords(records);
    }

    public void LoadFromRecords(IEnumerable<HierarchyNodeRecord> records)
    {
        if (records == null)
            throw new ArgumentNullException(nameof(records));

        _records = records.ToList();
        _selectedRecord = null;
        BuildChart();
    }

    public void FitToView()
    {
        if (_contentSize.Width <= 0 || _contentSize.Height <= 0)
            return;

        var availableWidth = Math.Max(1, _canvas.ClientSize.Width - CanvasPadding);
        var availableHeight = Math.Max(1, _canvas.ClientSize.Height - CanvasPadding);
        Zoom = Math.Min(MaxZoom, Math.Max(MinZoom, Math.Min(availableWidth / _contentSize.Width, availableHeight / _contentSize.Height)));
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _zoomOutButton.Click += (_, _) => Zoom -= 0.1f;
        _zoomInButton.Click += (_, _) => Zoom += 0.1f;
        _fitButton.Click += (_, _) => FitToView();
        _refreshButton.Click += async (_, _) =>
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
            await ReloadAsync();
        };

        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.Items.Add(_zoomOutButton);
        _toolStrip.Items.Add(_zoomInButton);
        _toolStrip.Items.Add(_fitButton);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(_refreshButton);

        _canvas.Dock = DockStyle.Fill;
        _canvas.AutoScroll = true;
        _canvas.BackColor = Color.FromArgb(250, 251, 253);

        Controls.Add(_canvas);
        Controls.Add(_toolStrip);
        Dock = DockStyle.Fill;
        MinimumSize = new Size(420, 320);
        Name = nameof(OrganizationChartView);
        Size = new Size(760, 520);

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildChart()
    {
        _layoutRoots.Clear();

        var recordsById = _records
            .Where(record => !string.IsNullOrWhiteSpace(record.Id))
            .GroupBy(record => record.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToDictionary(record => record.Id, StringComparer.OrdinalIgnoreCase);

        var nodesById = recordsById.ToDictionary(pair => pair.Key, pair => new ChartNode(pair.Value), StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodesById.Values.OrderBy(node => node.Record.SortOrder).ThenBy(node => node.Record.DisplayName, StringComparer.CurrentCultureIgnoreCase))
        {
            var parentId = node.Record.ParentId;
            if (!string.IsNullOrWhiteSpace(parentId) && nodesById.TryGetValue(parentId, out var parent) && !ReferenceEquals(parent, node))
                parent.Children.Add(node);
            else
                _layoutRoots.Add(node);
        }

        var left = (float)CanvasPadding;
        var maxHeight = (float)CanvasPadding;
        foreach (var root in _layoutRoots)
        {
            MeasureSubtree(root);
            ArrangeSubtree(root, left, CanvasPadding);
            left += root.SubtreeWidth + HorizontalSpacing;
            maxHeight = Math.Max(maxHeight, GetMaxBottom(root));
        }

        _contentSize = new SizeF(Math.Max(left + CanvasPadding - HorizontalSpacing, CanvasPadding * 2), maxHeight + CanvasPadding);
        UpdateCanvasSize();
        _canvas.Invalidate();
    }

    private float MeasureSubtree(ChartNode node)
    {
        if (node.Children.Count == 0)
        {
            node.SubtreeWidth = NodeWidth;
            return node.SubtreeWidth;
        }

        var childWidth = 0f;
        foreach (var child in node.Children)
        {
            childWidth += MeasureSubtree(child);
            if (!ReferenceEquals(child, node.Children.Last()))
                childWidth += HorizontalSpacing;
        }

        node.SubtreeWidth = Math.Max(NodeWidth, childWidth);
        return node.SubtreeWidth;
    }

    private void ArrangeSubtree(ChartNode node, float left, float top)
    {
        var nodeLeft = left + (node.SubtreeWidth - NodeWidth) / 2f;
        node.Bounds = new RectangleF(nodeLeft, top, NodeWidth, NodeHeight);

        if (node.Children.Count == 0)
            return;

        var childrenWidth = node.Children.Sum(child => child.SubtreeWidth) + HorizontalSpacing * (node.Children.Count - 1);
        var childLeft = left + (node.SubtreeWidth - childrenWidth) / 2f;
        var childTop = top + NodeHeight + VerticalSpacing;

        foreach (var child in node.Children)
        {
            ArrangeSubtree(child, childLeft, childTop);
            childLeft += child.SubtreeWidth + HorizontalSpacing;
        }
    }

    private static float GetMaxBottom(ChartNode node)
    {
        var bottom = node.Bounds.Bottom;
        foreach (var child in node.Children)
            bottom = Math.Max(bottom, GetMaxBottom(child));
        return bottom;
    }

    private void UpdateCanvasSize()
    {
        _canvas.AutoScrollMinSize = new Size(
            (int)Math.Ceiling(_contentSize.Width * Zoom),
            (int)Math.Ceiling(_contentSize.Height * Zoom));
    }

    private void PaintChart(Graphics graphics)
    {
        graphics.Clear(_canvas.BackColor);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        graphics.TranslateTransform(_canvas.AutoScrollPosition.X, _canvas.AutoScrollPosition.Y);
        graphics.ScaleTransform(Zoom, Zoom);

        foreach (var root in _layoutRoots)
            DrawConnectors(graphics, root);

        foreach (var root in _layoutRoots)
            DrawNode(graphics, root);
    }

    private void DrawConnectors(Graphics graphics, ChartNode node)
    {
        if (node.Children.Count == 0)
            return;

        using var pen = new Pen(Color.FromArgb(45, 45, 48), 3f)
        {
            StartCap = LineCap.Square,
            EndCap = LineCap.Square
        };

        var parentCenter = new PointF(node.Bounds.Left + node.Bounds.Width / 2f, node.Bounds.Bottom);
        var childTop = node.Children[0].Bounds.Top;
        var midY = parentCenter.Y + (childTop - parentCenter.Y) * 0.45f;
        graphics.DrawLine(pen, parentCenter.X, parentCenter.Y, parentCenter.X, midY);

        var firstChildCenter = node.Children.First().Bounds.Left + NodeWidth / 2f;
        var lastChildCenter = node.Children.Last().Bounds.Left + NodeWidth / 2f;
        graphics.DrawLine(pen, firstChildCenter, midY, lastChildCenter, midY);

        foreach (var child in node.Children)
        {
            var childCenter = child.Bounds.Left + NodeWidth / 2f;
            graphics.DrawLine(pen, childCenter, midY, childCenter, child.Bounds.Top);
            DrawConnectors(graphics, child);
        }
    }

    private void DrawNode(Graphics graphics, ChartNode node)
    {
        var palette = GetPalette(node.Record.Kind);
        var selected = _selectedRecord != null && string.Equals(_selectedRecord.Id, node.Record.Id, StringComparison.OrdinalIgnoreCase);
        var bounds = node.Bounds;
        var shadowBounds = new RectangleF(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height);

        using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            graphics.FillRoundedRectangle(shadowBrush, Rectangle.Round(shadowBounds), 5);

        using (var background = new LinearGradientBrush(bounds, palette.Light, palette.Dark, LinearGradientMode.Vertical))
            graphics.FillRoundedRectangle(background, Rectangle.Round(bounds), 5);

        using (var border = new Pen(selected ? Color.FromArgb(23, 77, 143) : palette.Border, selected ? 3f : 1.5f))
            graphics.DrawRoundedRectangle(border, Rectangle.Round(bounds), 5);

        var textBounds = Rectangle.Round(RectangleF.Inflate(bounds, -12, -9));
        using var titleFont = new Font(Font.FontFamily, 10.5f, FontStyle.Bold);
        using var subtitleFont = new Font(Font.FontFamily, 8.5f, FontStyle.Regular);
        using var titleBrush = new SolidBrush(Color.FromArgb(24, 26, 31));
        using var subtitleBrush = new SolidBrush(Color.FromArgb(65, 70, 82));
        using var titleFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = ShowSubtitles && !string.IsNullOrWhiteSpace(node.Record.Subtitle) ? StringAlignment.Near : StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        if (ShowSubtitles && !string.IsNullOrWhiteSpace(node.Record.Subtitle))
        {
            var titleRect = new Rectangle(textBounds.Left, textBounds.Top + 5, textBounds.Width, 24);
            var subtitleRect = new Rectangle(textBounds.Left, textBounds.Top + 32, textBounds.Width, textBounds.Height - 32);
            graphics.DrawString(node.Record.DisplayName, titleFont, titleBrush, titleRect, titleFormat);

            using var subtitleFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter
            };
            graphics.DrawString(node.Record.Subtitle, subtitleFont, subtitleBrush, subtitleRect, subtitleFormat);
        }
        else
        {
            graphics.DrawString(node.Record.DisplayName, titleFont, titleBrush, textBounds, titleFormat);
        }

        foreach (var child in node.Children)
            DrawNode(graphics, child);
    }

    private void SelectAt(Point clientPoint)
    {
        var logicalPoint = new PointF(
            (clientPoint.X - _canvas.AutoScrollPosition.X) / Zoom,
            (clientPoint.Y - _canvas.AutoScrollPosition.Y) / Zoom);

        var hit = FindNodeAt(logicalPoint);
        if (hit == null)
            return;

        _selectedRecord = hit.Record;
        _canvas.Invalidate();
        NodeActivated?.Invoke(this, new HierarchyNodeEventArgs(hit.Record));
    }

    private ChartNode? FindNodeAt(PointF logicalPoint)
    {
        foreach (var root in _layoutRoots)
        {
            var hit = FindNodeAt(root, logicalPoint);
            if (hit != null)
                return hit;
        }

        return null;
    }

    private static ChartNode? FindNodeAt(ChartNode node, PointF logicalPoint)
    {
        if (node.Bounds.Contains(logicalPoint))
            return node;

        foreach (var child in node.Children)
        {
            var hit = FindNodeAt(child, logicalPoint);
            if (hit != null)
                return hit;
        }

        return null;
    }

    private static NodePalette GetPalette(HierarchyNodeKind kind)
    {
        switch (kind)
        {
            case HierarchyNodeKind.Organization:
                return new NodePalette(Color.FromArgb(255, 236, 239), Color.FromArgb(255, 172, 184), Color.FromArgb(142, 46, 55));
            case HierarchyNodeKind.Department:
                return new NodePalette(Color.FromArgb(245, 237, 255), Color.FromArgb(212, 194, 238), Color.FromArgb(93, 76, 124));
            case HierarchyNodeKind.Position:
                return new NodePalette(Color.FromArgb(230, 251, 252), Color.FromArgb(154, 231, 235), Color.FromArgb(55, 123, 128));
            case HierarchyNodeKind.Person:
                return new NodePalette(Color.FromArgb(232, 246, 255), Color.FromArgb(168, 216, 247), Color.FromArgb(57, 105, 150));
            default:
                return new NodePalette(Color.FromArgb(248, 249, 252), Color.FromArgb(221, 225, 235), Color.FromArgb(103, 111, 126));
        }
    }

    private sealed class ChartCanvasPanel : Panel
    {
        private readonly OrganizationChartView _owner;

        public ChartCanvasPanel(OrganizationChartView owner)
        {
            _owner = owner;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            _owner.PaintChart(e.Graphics);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            _owner.SelectAt(e.Location);
        }
    }

    private sealed class ChartNode
    {
        public ChartNode(HierarchyNodeRecord record)
        {
            Record = record;
        }

        public HierarchyNodeRecord Record { get; }
        public List<ChartNode> Children { get; } = new();
        public RectangleF Bounds { get; set; }
        public float SubtreeWidth { get; set; }
    }

    private readonly struct NodePalette
    {
        public NodePalette(Color light, Color dark, Color border)
        {
            Light = light;
            Dark = dark;
            Border = border;
        }

        public Color Light { get; }
        public Color Dark { get; }
        public Color Border { get; }
    }
}
