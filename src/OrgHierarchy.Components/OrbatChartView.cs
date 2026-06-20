using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace OrgHierarchy.Components;

[DefaultEvent(nameof(UnitActivated))]
[DefaultProperty(nameof(NameColumn))]
[ToolboxItem(true)]
[Description("Displays an order of battle chart with MIL-STD-style unit symbols.")]
public sealed class OrbatChartView : UserControl
{
    private const float MinZoom = 0.35f;
    private const float MaxZoom = 2.5f;
    private const int CanvasPadding = 36;
    private const float StackSpacing = 5.5f;

    private readonly ToolStrip _toolStrip = new();
    private readonly ToolStripButton _zoomOutButton = new("Zoom -");
    private readonly ToolStripButton _zoomInButton = new("Zoom +");
    private readonly ToolStripButton _fitButton = new("Fit");
    private readonly ToolStripButton _refreshButton = new("Refresh");
    private readonly OrbatCanvasPanel _canvas;
    private readonly List<OrbatLayoutNode> _layoutRoots = new();

    private List<OrbatUnitRecord> _units = new();
    private Func<CancellationToken, Task<DataTable>>? _dataLoader;
    private OrbatUnitRecord? _selectedUnit;
    private SizeF _contentSize = SizeF.Empty;
    private float _zoom = 1f;

    public OrbatChartView()
    {
        _canvas = new OrbatCanvasPanel(this);
        InitializeComponent();
    }

    public event EventHandler<OrbatUnitEventArgs>? UnitActivated;
    public event EventHandler<OrbatUnitEventArgs>? UnitContextRequested;
    public event EventHandler? RefreshRequested;

    [Category("Data")]
    [DefaultValue("Id")]
    public string IdColumn { get; set; } = "Id";

    [Category("Data")]
    [DefaultValue("ParentId")]
    public string ParentIdColumn { get; set; } = "ParentId";

    [Category("Data")]
    [DefaultValue("Name")]
    public string NameColumn { get; set; } = "Name";

    [Category("Data")]
    [DefaultValue("ShortName")]
    public string ShortNameColumn { get; set; } = "ShortName";

    [Category("Data")]
    [DefaultValue("UniqueDesignation")]
    public string UniqueDesignationColumn { get; set; } = "UniqueDesignation";

    [Category("Data")]
    [DefaultValue("Affiliation")]
    public string AffiliationColumn { get; set; } = "Affiliation";

    [Category("Data")]
    [DefaultValue("Echelon")]
    public string EchelonColumn { get; set; } = "Echelon";

    [Category("Data")]
    [DefaultValue("UnitType")]
    public string UnitTypeColumn { get; set; } = "UnitType";

    [Category("Data")]
    [DefaultValue("Sidc")]
    public string SidcColumn { get; set; } = "Sidc";

    [Category("Data")]
    [DefaultValue(true)]
    public bool SidcOverridesFields { get; set; } = true;

    [Category("Data")]
    [DefaultValue("SymbolText")]
    public string SymbolTextColumn { get; set; } = "SymbolText";

    [Category("Data")]
    [DefaultValue("Headquarters")]
    public string HeadquartersColumn { get; set; } = "Headquarters";

    [Category("Data")]
    [DefaultValue("TaskForce")]
    public string TaskForceColumn { get; set; } = "TaskForce";

    [Category("Data")]
    [DefaultValue("PlannedAnticipated")]
    public string PlannedAnticipatedColumn { get; set; } = "PlannedAnticipated";

    [Category("Data")]
    [DefaultValue("StackCount")]
    public string StackCountColumn { get; set; } = "StackCount";

    [Category("Data")]
    [DefaultValue("ReinforcedReduced")]
    public string ReinforcedReducedColumn { get; set; } = "ReinforcedReduced";

    [Category("Data")]
    [DefaultValue("Reinforced")]
    public string ReinforcedColumn { get; set; } = "Reinforced";

    [Category("Data")]
    [DefaultValue("Reduced")]
    public string ReducedColumn { get; set; } = "Reduced";

    [Category("Data")]
    [DefaultValue("SortOrder")]
    public string SortColumn { get; set; } = "SortOrder";

    [Category("Layout")]
    [DefaultValue(110)]
    public int SymbolWidth { get; set; } = 110;

    [Category("Layout")]
    [DefaultValue(76)]
    public int SymbolHeight { get; set; } = 76;

    [Category("Layout")]
    [DefaultValue(54)]
    public int HorizontalSpacing { get; set; } = 54;

    [Category("Layout")]
    [DefaultValue(78)]
    public int VerticalSpacing { get; set; } = 78;

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ShowUnitLabels { get; set; } = true;

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ShowUniqueDesignation { get; set; } = true;

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ShowLegend { get; set; } = true;

    [Browsable(false)]
    public OrbatUnitRecord? SelectedUnit => _selectedUnit;

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
        var records = OrbatDataTableMapper.FromDataTable(
            table,
            IdColumn,
            ParentIdColumn,
            NameColumn,
            ShortNameColumn,
            UniqueDesignationColumn,
            AffiliationColumn,
            EchelonColumn,
            UnitTypeColumn,
            SidcColumn,
            SymbolTextColumn,
            HeadquartersColumn,
            TaskForceColumn,
            PlannedAnticipatedColumn,
            StackCountColumn,
            ReinforcedReducedColumn,
            ReinforcedColumn,
            ReducedColumn,
            SortColumn,
            SidcOverridesFields);

        LoadFromUnits(records);
    }

    public void LoadFromUnits(IEnumerable<OrbatUnitRecord> units)
    {
        if (units == null)
            throw new ArgumentNullException(nameof(units));

        _units = units.ToList();
        _selectedUnit = null;
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
        _canvas.BackColor = Color.White;

        Controls.Add(_canvas);
        Controls.Add(_toolStrip);
        Dock = DockStyle.Fill;
        MinimumSize = new Size(460, 340);
        Name = nameof(OrbatChartView);
        Size = new Size(900, 620);

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildChart()
    {
        _layoutRoots.Clear();

        var unitsById = _units
            .Where(unit => !string.IsNullOrWhiteSpace(unit.Id))
            .GroupBy(unit => unit.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToDictionary(unit => unit.Id, StringComparer.OrdinalIgnoreCase);

        var nodesById = unitsById.ToDictionary(pair => pair.Key, pair => new OrbatLayoutNode(pair.Value), StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodesById.Values.OrderBy(node => node.Unit.SortOrder).ThenBy(node => node.Unit.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var parentId = node.Unit.ParentId;
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
            ArrangeSubtree(root, left, CanvasPadding + 28);
            left += root.SubtreeWidth + HorizontalSpacing;
            maxHeight = Math.Max(maxHeight, GetMaxBottom(root));
        }

        var legendHeight = ShowLegend ? 78 : 0;
        _contentSize = new SizeF(Math.Max(left + CanvasPadding - HorizontalSpacing, CanvasPadding * 2), maxHeight + CanvasPadding + legendHeight);
        UpdateCanvasSize();
        _canvas.Invalidate();
    }

    private float MeasureSubtree(OrbatLayoutNode node)
    {
        var nodeWidth = SymbolWidth + 30;
        if (node.Children.Count == 0)
        {
            node.SubtreeWidth = nodeWidth;
            return node.SubtreeWidth;
        }

        var childWidth = 0f;
        foreach (var child in node.Children)
        {
            childWidth += MeasureSubtree(child);
            if (!ReferenceEquals(child, node.Children.Last()))
                childWidth += HorizontalSpacing;
        }

        node.SubtreeWidth = Math.Max(nodeWidth, childWidth);
        return node.SubtreeWidth;
    }

    private void ArrangeSubtree(OrbatLayoutNode node, float left, float top)
    {
        var nodeWidth = SymbolWidth + 30;
        var nodeLeft = left + (node.SubtreeWidth - nodeWidth) / 2f;
        node.Bounds = new RectangleF(nodeLeft, top, nodeWidth, GetNodeHeight(node.Unit));

        if (node.Children.Count == 0)
            return;

        var childrenWidth = node.Children.Sum(child => child.SubtreeWidth) + HorizontalSpacing * (node.Children.Count - 1);
        var childLeft = left + (node.SubtreeWidth - childrenWidth) / 2f;
        var childTop = top + node.Bounds.Height + VerticalSpacing;

        foreach (var child in node.Children)
        {
            ArrangeSubtree(child, childLeft, childTop);
            childLeft += child.SubtreeWidth + HorizontalSpacing;
        }
    }

    private static float GetMaxBottom(OrbatLayoutNode node)
    {
        var bottom = node.Bounds.Bottom;
        foreach (var child in node.Children)
            bottom = Math.Max(bottom, GetMaxBottom(child));
        return bottom;
    }

    private float GetNodeHeight(OrbatUnitRecord unit)
    {
        return SymbolHeight + 34 + GetStackOffset(unit) + (unit.TaskForce ? 15 : 0);
    }

    private static float GetStackOffset(OrbatUnitRecord unit)
    {
        return (Math.Max(1, Math.Min(6, unit.StackCount)) - 1) * StackSpacing;
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

        using (var titleFont = new Font(Font.FontFamily, 18f, FontStyle.Bold))
        using (var titleBrush = new SolidBrush(Color.Black))
        using (var titleFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            var titleBounds = new RectangleF(0, 0, Math.Max(_contentSize.Width, _canvas.ClientSize.Width / Zoom), 36);
            graphics.DrawString("Order of Battle", titleFont, titleBrush, titleBounds, titleFormat);
        }

        foreach (var root in _layoutRoots)
            DrawConnectors(graphics, root);

        foreach (var root in _layoutRoots)
            DrawNode(graphics, root);

        if (ShowLegend)
            DrawLegend(graphics);
    }

    private void DrawConnectors(Graphics graphics, OrbatLayoutNode node)
    {
        if (node.Children.Count == 0)
            return;

        using var pen = new Pen(Color.FromArgb(60, 60, 60), 2f)
        {
            StartCap = LineCap.Square,
            EndCap = LineCap.Square
        };

        var symbolBounds = GetSymbolBounds(node);
        var parentCenter = new PointF(node.Bounds.Left + node.Bounds.Width / 2f, symbolBounds.Bottom + GetStackOffset(node.Unit));
        var childTop = GetSymbolBounds(node.Children[0]).Top;
        var midY = parentCenter.Y + (childTop - parentCenter.Y) * 0.45f;
        graphics.DrawLine(pen, parentCenter.X, parentCenter.Y, parentCenter.X, midY);

        var firstChildCenter = node.Children.First().Bounds.Left + node.Children.First().Bounds.Width / 2f;
        var lastChildCenter = node.Children.Last().Bounds.Left + node.Children.Last().Bounds.Width / 2f;
        graphics.DrawLine(pen, firstChildCenter, midY, lastChildCenter, midY);

        foreach (var child in node.Children)
        {
            var childCenter = child.Bounds.Left + child.Bounds.Width / 2f;
            graphics.DrawLine(pen, childCenter, midY, childCenter, GetConnectorEndY(child));
            DrawConnectors(graphics, child);
        }
    }

    private float GetConnectorEndY(OrbatLayoutNode node)
    {
        var symbolTop = GetSymbolBounds(node).Top;
        return HasEchelonMarker(node.Unit.Echelon) ? symbolTop - 22 : symbolTop;
    }

    private void DrawNode(Graphics graphics, OrbatLayoutNode node)
    {
        var selected = _selectedUnit != null && string.Equals(_selectedUnit.Id, node.Unit.Id, StringComparison.OrdinalIgnoreCase);
        var symbolBounds = GetSymbolBounds(node);
        var palette = GetPalette(node.Unit.Affiliation);
        var stackCount = Math.Max(1, Math.Min(6, node.Unit.StackCount));

        using (var fillBrush = new SolidBrush(palette.Fill))
        {
            DrawStack(graphics, symbolBounds, stackCount, fillBrush, node.Unit.PlannedAnticipated);
            graphics.FillRectangle(fillBrush, symbolBounds);
        }

        using (var borderPen = new Pen(Color.Black, 3f))
        {
            if (node.Unit.PlannedAnticipated)
                borderPen.DashStyle = DashStyle.Dash;

            graphics.DrawRectangle(borderPen, Rectangle.Round(symbolBounds));
        }

        if (selected)
        {
            using var selectionPen = new Pen(Color.FromArgb(255, 190, 0), 1.5f);
            var selectionBounds = RectangleF.Inflate(symbolBounds, 3f, 3f);
            graphics.DrawRectangle(selectionPen, Rectangle.Round(selectionBounds));
        }

        if (node.Unit.Affiliation == OrbatAffiliation.Hostile || node.Unit.Affiliation == OrbatAffiliation.Suspect)
        {
            using var slashPen = new Pen(Color.Black, 3f);
            graphics.DrawLine(slashPen, symbolBounds.Left + 2, symbolBounds.Bottom - 2, symbolBounds.Right - 2, symbolBounds.Top + 2);
        }

        DrawEchelon(graphics, node.Unit.Echelon, symbolBounds);
        DrawUnitIcon(graphics, node.Unit, symbolBounds);
        DrawAmplifiers(graphics, node.Unit, symbolBounds);

        if (ShowUnitLabels)
            DrawUnitText(graphics, node.Unit, node.Bounds, symbolBounds);

        foreach (var child in node.Children)
            DrawNode(graphics, child);
    }

    private static void DrawStack(Graphics graphics, RectangleF symbolBounds, int stackCount, Brush fillBrush, bool plannedAnticipated)
    {
        if (stackCount <= 1)
            return;

        using var stackPen = new Pen(Color.Black, 2f);
        if (plannedAnticipated)
            stackPen.DashStyle = DashStyle.Dash;

        for (var index = stackCount - 1; index >= 1; index--)
        {
            var offset = index * StackSpacing;
            var stackBounds = new RectangleF(symbolBounds.Left + offset, symbolBounds.Top + offset, symbolBounds.Width, symbolBounds.Height);
            graphics.FillRectangle(fillBrush, stackBounds);
            graphics.DrawRectangle(stackPen, Rectangle.Round(stackBounds));
        }
    }

    private RectangleF GetSymbolBounds(OrbatLayoutNode node)
    {
        return new RectangleF(
            node.Bounds.Left + (node.Bounds.Width - SymbolWidth) / 2f,
            node.Bounds.Top + 18,
            SymbolWidth,
            SymbolHeight);
    }

    private void DrawEchelon(Graphics graphics, OrbatEchelon echelon, RectangleF symbolBounds)
    {
        if (!TryGetEchelonGraphic(echelon, out var markKind, out var markCount))
            return;

        DrawGraphicEchelon(graphics, symbolBounds, markKind, markCount);
    }

    private static void DrawGraphicEchelon(Graphics graphics, RectangleF symbolBounds, EchelonMarkKind markKind, int markCount)
    {
        var markSize = markKind == EchelonMarkKind.Cross ? 8f : 6f;
        const float gap = 3f;
        var totalWidth = markCount * markSize + (markCount - 1) * gap;
        var left = symbolBounds.Left + (symbolBounds.Width - totalWidth) / 2f;
        var top = symbolBounds.Top - (markKind == EchelonMarkKind.Cross ? 18f : 16f);
        var penWidth = markKind == EchelonMarkKind.Cross ? 2.7f : markKind == EchelonMarkKind.VerticalLine ? 2.8f : 2f;

        using var pen = new Pen(Color.Black, penWidth)
        {
            StartCap = LineCap.Square,
            EndCap = LineCap.Square
        };

        for (var index = 0; index < markCount; index++)
        {
            var x = left + index * (markSize + gap);
            switch (markKind)
            {
                case EchelonMarkKind.OpenCircle:
                    graphics.DrawEllipse(pen, x, top, markSize, markSize);
                    break;
                case EchelonMarkKind.FilledDot:
                    using (var brush = new SolidBrush(Color.Black))
                        graphics.FillEllipse(brush, x, top, markSize, markSize);
                    break;
                case EchelonMarkKind.VerticalLine:
                    graphics.DrawLine(pen, x + markSize / 2f, top - 2f, x + markSize / 2f, top + markSize + 4f);
                    break;
                case EchelonMarkKind.Plus:
                    graphics.DrawLine(pen, x + markSize / 2f, top, x + markSize / 2f, top + markSize);
                    graphics.DrawLine(pen, x, top + markSize / 2f, x + markSize, top + markSize / 2f);
                    break;
                default:
                    graphics.DrawLine(pen, x, top, x + markSize, top + markSize);
                    graphics.DrawLine(pen, x + markSize, top, x, top + markSize);
                    break;
            }
        }
    }

    private static bool TryGetEchelonGraphic(OrbatEchelon echelon, out EchelonMarkKind markKind, out int markCount)
    {
        markKind = EchelonMarkKind.Cross;
        switch (echelon)
        {
            case OrbatEchelon.Team:
                markKind = EchelonMarkKind.OpenCircle;
                markCount = 1;
                return true;
            case OrbatEchelon.Squad:
                markKind = EchelonMarkKind.FilledDot;
                markCount = 1;
                return true;
            case OrbatEchelon.Section:
                markKind = EchelonMarkKind.FilledDot;
                markCount = 2;
                return true;
            case OrbatEchelon.Platoon:
                markKind = EchelonMarkKind.FilledDot;
                markCount = 3;
                return true;
            case OrbatEchelon.Company:
                markKind = EchelonMarkKind.VerticalLine;
                markCount = 1;
                return true;
            case OrbatEchelon.Battalion:
                markKind = EchelonMarkKind.VerticalLine;
                markCount = 2;
                return true;
            case OrbatEchelon.Regiment:
                markKind = EchelonMarkKind.VerticalLine;
                markCount = 3;
                return true;
            case OrbatEchelon.Brigade:
                markCount = 1;
                return true;
            case OrbatEchelon.Division:
                markCount = 2;
                return true;
            case OrbatEchelon.Corps:
                markCount = 3;
                return true;
            case OrbatEchelon.Army:
                markCount = 4;
                return true;
            case OrbatEchelon.ArmyGroup:
                markCount = 5;
                return true;
            case OrbatEchelon.Region:
                markCount = 6;
                return true;
            case OrbatEchelon.Command:
                markCount = 2;
                markKind = EchelonMarkKind.Plus;
                return true;
            default:
                markCount = 0;
                return false;
        }
    }

    private static bool HasEchelonMarker(OrbatEchelon echelon)
    {
        return TryGetEchelonGraphic(echelon, out _, out _);
    }

    private void DrawUnitIcon(Graphics graphics, OrbatUnitRecord unit, RectangleF bounds)
    {
        var icon = RectangleF.Inflate(bounds, -24, -20);
        icon.Y += 2;

        using var pen = new Pen(Color.Black, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(Color.Black);
        using var font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
        using var centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        if (!string.IsNullOrWhiteSpace(unit.SymbolText))
        {
            graphics.DrawString(unit.SymbolText, font, brush, icon, centerFormat);
            return;
        }

        switch (unit.UnitType)
        {
            case OrbatUnitType.Headquarters:
                graphics.DrawString("HQ", font, brush, icon, centerFormat);
                break;
            case OrbatUnitType.Infantry:
                graphics.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
                graphics.DrawLine(pen, bounds.Right, bounds.Top, bounds.Left, bounds.Bottom);
                break;
            case OrbatUnitType.Armor:
                DrawCapsule(graphics, pen, icon);
                break;
            case OrbatUnitType.MechanizedInfantry:
                DrawCapsule(graphics, pen, icon);
                graphics.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
                graphics.DrawLine(pen, bounds.Right, bounds.Top, bounds.Left, bounds.Bottom);
                break;
            case OrbatUnitType.Artillery:
                graphics.FillEllipse(brush, icon.Left + icon.Width / 2f - 12, icon.Top + icon.Height / 2f - 12, 24, 24);
                break;
            case OrbatUnitType.AirDefense:
                var arcRise = bounds.Height * 0.23f;
                graphics.DrawBezier(
                    pen,
                    bounds.Left,
                    bounds.Bottom,
                    bounds.Left + bounds.Width * 0.25f,
                    bounds.Bottom - arcRise,
                    bounds.Right - bounds.Width * 0.25f,
                    bounds.Bottom - arcRise,
                    bounds.Right,
                    bounds.Bottom);
                break;
            case OrbatUnitType.Aviation:
            case OrbatUnitType.Air:
                var plane = new[]
                {
                    new PointF(icon.Left + 6, icon.Top + icon.Height / 2f),
                    new PointF(icon.Right - 8, icon.Top + 6),
                    new PointF(icon.Right - 8, icon.Bottom - 6)
                };
                graphics.FillPolygon(brush, plane);
                break;
            case OrbatUnitType.Engineer:
                var engineerLeft = bounds.Left + bounds.Width * 0.28f;
                var engineerMiddle = bounds.Left + bounds.Width * 0.5f;
                var engineerRight = bounds.Left + bounds.Width * 0.72f;
                var engineerTop = bounds.Top + bounds.Height * 0.36f;
                var engineerBottom = bounds.Top + bounds.Height * 0.64f;
                graphics.DrawLine(pen, engineerLeft, engineerTop, engineerRight, engineerTop);
                graphics.DrawLine(pen, engineerLeft, engineerTop, engineerLeft, engineerBottom);
                graphics.DrawLine(pen, engineerMiddle, engineerTop, engineerMiddle, engineerBottom);
                graphics.DrawLine(pen, engineerRight, engineerTop, engineerRight, engineerBottom);
                break;
            case OrbatUnitType.Reconnaissance:
                graphics.DrawEllipse(pen, RectangleF.Inflate(icon, -12, -7));
                graphics.FillEllipse(brush, icon.Left + icon.Width / 2f - 4, icon.Top + icon.Height / 2f - 4, 8, 8);
                break;
            case OrbatUnitType.Signal:
                var lightning = new[]
                {
                    new PointF(bounds.Left, bounds.Top),
                    new PointF(bounds.Left + bounds.Width * 0.5f, bounds.Top + bounds.Height * 0.66f),
                    new PointF(bounds.Left + bounds.Width * 0.5f, bounds.Top + bounds.Height * 0.34f),
                    new PointF(bounds.Right, bounds.Bottom)
                };
                graphics.DrawLines(pen, lightning);
                break;
            case OrbatUnitType.MilitaryPolice:
                graphics.DrawString("MP", font, brush, icon, centerFormat);
                break;
            case OrbatUnitType.Medical:
                graphics.DrawLine(pen, bounds.Left + bounds.Width / 2f, bounds.Top, bounds.Left + bounds.Width / 2f, bounds.Bottom);
                graphics.DrawLine(pen, bounds.Left, bounds.Top + bounds.Height / 2f, bounds.Right, bounds.Top + bounds.Height / 2f);
                break;
            case OrbatUnitType.Logistics:
            case OrbatUnitType.Maintenance:
            case OrbatUnitType.Transportation:
                graphics.DrawRectangle(pen, Rectangle.Round(RectangleF.Inflate(icon, -8, -8)));
                graphics.DrawLine(pen, icon.Left + 8, icon.Top + 8, icon.Left + icon.Width / 2f, icon.Top);
                graphics.DrawLine(pen, icon.Right - 8, icon.Top + 8, icon.Left + icon.Width / 2f, icon.Top);
                break;
            case OrbatUnitType.SpecialOperations:
                graphics.FillEllipse(brush, icon.Left + icon.Width / 2f - 6, icon.Top + icon.Height / 2f - 6, 12, 12);
                graphics.DrawArc(pen, RectangleF.Inflate(icon, -8, -2), 0, 360);
                break;
            case OrbatUnitType.Naval:
                graphics.DrawArc(pen, icon, 20, 140);
                break;
            case OrbatUnitType.Cyber:
                graphics.DrawString("CY", font, brush, icon, centerFormat);
                break;
            case OrbatUnitType.Intelligence:
                graphics.DrawString("INT", font, brush, icon, centerFormat);
                break;
            case OrbatUnitType.PsychologicalOperations:
                graphics.DrawString("PS", font, brush, icon, centerFormat);
                break;
            default:
                graphics.DrawString("?", font, brush, icon, centerFormat);
                break;
        }
    }

    private static void DrawCapsule(Graphics graphics, Pen pen, RectangleF bounds)
    {
        var height = Math.Min(bounds.Height, bounds.Width);
        var capsule = new RectangleF(
            bounds.Left,
            bounds.Top + (bounds.Height - height) / 2f,
            bounds.Width,
            height);
        var radius = capsule.Height / 2f;

        using var path = new GraphicsPath();
        path.AddArc(capsule.Left, capsule.Top, radius * 2f, radius * 2f, 90, 180);
        path.AddLine(capsule.Left + radius, capsule.Top, capsule.Right - radius, capsule.Top);
        path.AddArc(capsule.Right - radius * 2f, capsule.Top, radius * 2f, radius * 2f, 270, 180);
        path.AddLine(capsule.Right - radius, capsule.Bottom, capsule.Left + radius, capsule.Bottom);
        path.CloseFigure();

        graphics.DrawPath(pen, path);
    }

    private static void DrawAmplifiers(Graphics graphics, OrbatUnitRecord unit, RectangleF bounds)
    {
        var modifier = GetReinforcedReduced(unit);
        if (modifier != OrbatReinforcedReduced.NotApplicable)
            DrawReinforcedReducedMarker(graphics, bounds, modifier);
    }

    private static OrbatReinforcedReduced GetReinforcedReduced(OrbatUnitRecord unit)
    {
        if (unit.ReinforcedReduced != OrbatReinforcedReduced.NotApplicable)
            return unit.ReinforcedReduced;
        if (unit.Reinforced && unit.Reduced)
            return OrbatReinforcedReduced.ReinforcedAndReduced;
        if (unit.Reinforced)
            return OrbatReinforcedReduced.Reinforced;
        return unit.Reduced ? OrbatReinforcedReduced.Reduced : OrbatReinforcedReduced.NotApplicable;
    }

    private static void DrawReinforcedReducedMarker(Graphics graphics, RectangleF bounds, OrbatReinforcedReduced modifier)
    {
        var centerX = bounds.Right + 10f;
        var centerY = bounds.Top - 9f;
        var symbolSize = 7f;

        using var pen = new Pen(Color.Black, 2.7f)
        {
            StartCap = LineCap.Square,
            EndCap = LineCap.Square
        };
        using var parenPen = new Pen(Color.Black, 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        graphics.DrawArc(parenPen, centerX - 11f, centerY - 7f, 7f, 14f, 100, 160);
        graphics.DrawArc(parenPen, centerX + 4f, centerY - 7f, 7f, 14f, -80, 160);

        if (modifier == OrbatReinforcedReduced.Reinforced || modifier == OrbatReinforcedReduced.ReinforcedAndReduced)
            graphics.DrawLine(pen, centerX, centerY - symbolSize / 2f, centerX, centerY + symbolSize / 2f);

        if (modifier == OrbatReinforcedReduced.Reinforced || modifier == OrbatReinforcedReduced.ReinforcedAndReduced || modifier == OrbatReinforcedReduced.Reduced)
            graphics.DrawLine(pen, centerX - symbolSize / 2f, centerY, centerX + symbolSize / 2f, centerY);

        if (modifier == OrbatReinforcedReduced.ReinforcedAndReduced)
            graphics.DrawLine(pen, centerX - symbolSize / 2f, centerY + 4f, centerX + symbolSize / 2f, centerY + 4f);
    }

    private void DrawUnitText(Graphics graphics, OrbatUnitRecord unit, RectangleF bounds, RectangleF symbolBounds)
    {
        var topText = ShowUniqueDesignation ? unit.UniqueDesignation ?? string.Empty : string.Empty;
        var bottomText = string.IsNullOrWhiteSpace(unit.ShortName) ? unit.Name : unit.ShortName!;
        var bottomLineCount = Math.Max(1, bottomText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length);
        var stackBottom = symbolBounds.Bottom + GetStackOffset(unit);
        var taskForceTop = stackBottom + 2f;
        var bottomTop = unit.TaskForce ? stackBottom + 16f : stackBottom + (bottomLineCount == 1 ? 3f : 8f);
        var bottomHeight = Math.Min(42f, bottomLineCount * 18f);

        using var textBrush = new SolidBrush(Color.Black);
        using var backgroundBrush = new SolidBrush(_canvas.BackColor);
        using var topFont = new Font(Font.FontFamily, 8f, FontStyle.Regular);
        using var bottomFont = new Font(Font.FontFamily, 8.5f, FontStyle.Bold);
        using var centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

        if (!string.IsNullOrWhiteSpace(topText))
            graphics.DrawString(topText, topFont, textBrush, new RectangleF(bounds.Left, symbolBounds.Top - 18, bounds.Width, 16), centerFormat);

        if (unit.TaskForce)
        {
            var taskForceBounds = new RectangleF(bounds.Left, taskForceTop, bounds.Width, 14f);
            graphics.FillRectangle(backgroundBrush, RectangleF.Inflate(taskForceBounds, -20f, 0f));
            graphics.DrawString("TF", topFont, textBrush, taskForceBounds, centerFormat);
        }

        var bottomBounds = new RectangleF(bounds.Left, bottomTop, bounds.Width, bottomHeight);
        graphics.FillRectangle(backgroundBrush, RectangleF.Inflate(bottomBounds, -6f, 0f));
        graphics.DrawString(bottomText, bottomFont, textBrush, bottomBounds, centerFormat);
    }

    private void DrawLegend(Graphics graphics)
    {
        var y = _contentSize.Height - 64;
        var x = Math.Max(CanvasPadding, _contentSize.Width - 270);
        DrawLegendItem(graphics, OrbatAffiliation.Friend, "Friend", x, y);
        DrawLegendItem(graphics, OrbatAffiliation.Hostile, "Hostile", x, y + 30);
    }

    private static void DrawLegendItem(Graphics graphics, OrbatAffiliation affiliation, string label, float x, float y)
    {
        var palette = GetPalette(affiliation);
        var box = new RectangleF(x, y, 54, 22);
        using (var fill = new SolidBrush(palette.Fill))
            graphics.FillRectangle(fill, box);
        using (var pen = new Pen(palette.Border, 2f))
            graphics.DrawRectangle(pen, Rectangle.Round(box));
        using (var font = new Font(SystemFonts.DefaultFont.FontFamily, 10f, FontStyle.Bold))
        using (var brush = new SolidBrush(Color.Black))
            graphics.DrawString(label, font, brush, x + 70, y + 1);
    }

    private OrbatUnitRecord? SelectAt(Point clientPoint, bool raiseActivated)
    {
        var logicalPoint = new PointF(
            (clientPoint.X - _canvas.AutoScrollPosition.X) / Zoom,
            (clientPoint.Y - _canvas.AutoScrollPosition.Y) / Zoom);

        var hit = FindNodeAt(logicalPoint);
        if (hit == null)
            return null;

        _selectedUnit = hit.Unit;
        _canvas.Invalidate();
        if (raiseActivated)
            UnitActivated?.Invoke(this, new OrbatUnitEventArgs(hit.Unit));

        return hit.Unit;
    }

    private void SelectAt(Point clientPoint)
    {
        SelectAt(clientPoint, true);
    }

    private void RequestContextAt(Point clientPoint)
    {
        var unit = SelectAt(clientPoint, false);
        if (unit != null)
            UnitContextRequested?.Invoke(this, new OrbatUnitEventArgs(unit));
    }

    private OrbatLayoutNode? FindNodeAt(PointF logicalPoint)
    {
        foreach (var root in _layoutRoots)
        {
            var hit = FindNodeAt(root, logicalPoint);
            if (hit != null)
                return hit;
        }

        return null;
    }

    private static OrbatLayoutNode? FindNodeAt(OrbatLayoutNode node, PointF logicalPoint)
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

    private static string GetEchelonMarker(OrbatEchelon echelon)
    {
        switch (echelon)
        {
            case OrbatEchelon.Team:
                return "O";
            case OrbatEchelon.Squad:
                return "OO";
            case OrbatEchelon.Section:
                return "OOO";
            case OrbatEchelon.Platoon:
                return "I";
            case OrbatEchelon.Company:
                return "II";
            case OrbatEchelon.Battalion:
                return "III";
            case OrbatEchelon.Regiment:
                return "IIII";
            case OrbatEchelon.Brigade:
                return "X";
            case OrbatEchelon.Division:
                return "XX";
            case OrbatEchelon.Corps:
                return "XXX";
            case OrbatEchelon.Army:
                return "XXXX";
            case OrbatEchelon.ArmyGroup:
                return "XXXXX";
            case OrbatEchelon.Region:
                return "XXXXXX";
            case OrbatEchelon.Command:
                return "++";
            default:
                return string.Empty;
        }
    }

    private static OrbatPalette GetPalette(OrbatAffiliation affiliation)
    {
        switch (affiliation)
        {
            case OrbatAffiliation.Unspecified:
                return new OrbatPalette(Color.White, Color.Black);
            case OrbatAffiliation.Hostile:
                return new OrbatPalette(Color.FromArgb(255, 48, 49), Color.FromArgb(180, 0, 0));
            case OrbatAffiliation.Neutral:
                return new OrbatPalette(Color.FromArgb(0, 226, 110), Color.FromArgb(0, 120, 45));
            case OrbatAffiliation.Unknown:
                return new OrbatPalette(Color.FromArgb(255, 235, 0), Color.FromArgb(150, 135, 0));
            case OrbatAffiliation.Suspect:
                return new OrbatPalette(Color.FromArgb(255, 217, 107), Color.FromArgb(175, 115, 0));
            case OrbatAffiliation.Civilian:
                return new OrbatPalette(Color.FromArgb(206, 123, 206), Color.FromArgb(120, 0, 120));
            default:
                return new OrbatPalette(Color.FromArgb(0, 168, 220), Color.FromArgb(0, 90, 125));
        }
    }

    private sealed class OrbatCanvasPanel : Panel
    {
        private readonly OrbatChartView _owner;

        public OrbatCanvasPanel(OrbatChartView owner)
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
            if (e.Button == MouseButtons.Right)
                _owner.RequestContextAt(e.Location);
            else
                _owner.SelectAt(e.Location);
        }
    }

    private enum EchelonMarkKind
    {
        OpenCircle,
        FilledDot,
        VerticalLine,
        Cross,
        Plus
    }

    private sealed class OrbatLayoutNode
    {
        public OrbatLayoutNode(OrbatUnitRecord unit)
        {
            Unit = unit;
        }

        public OrbatUnitRecord Unit { get; }
        public List<OrbatLayoutNode> Children { get; } = new();
        public RectangleF Bounds { get; set; }
        public float SubtreeWidth { get; set; }

    }

    private readonly struct OrbatPalette
    {
        public OrbatPalette(Color fill, Color border)
        {
            Fill = fill;
            Border = border;
        }

        public Color Fill { get; }
        public Color Border { get; }
    }
}
