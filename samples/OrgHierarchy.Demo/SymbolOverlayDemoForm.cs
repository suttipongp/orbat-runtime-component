using OrgHierarchy.Components;

namespace OrgHierarchy.Demo;

public sealed class SymbolOverlayDemoForm : Form
{
    private readonly OverlayCanvas _canvas = new();
    private readonly ComboBox _domainComboBox = new();
    private readonly ComboBox _affiliationComboBox = new();
    private readonly ComboBox _statusComboBox = new();
    private readonly ComboBox _unitTypeComboBox = new();
    private readonly TableLayoutPanel _fieldsPanel = new();
    private readonly Dictionary<string, TextBox> _fieldInputs = new(StringComparer.OrdinalIgnoreCase);

    public SymbolOverlayDemoForm()
    {
        Text = "ORBAT Symbol Overlay Demo";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1100, 760);
        Size = new Size(1280, 820);

        _domainComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _domainComboBox.Items.AddRange(Enum.GetNames<SymbolPhysicalDomain>().Cast<object>().ToArray());
        _domainComboBox.SelectedItem = SymbolPhysicalDomain.LandUnit.ToString();
        _domainComboBox.SelectedIndexChanged += (_, _) => RebuildFieldEditor(loadSample: true);

        _affiliationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _affiliationComboBox.Items.AddRange(Enum.GetNames<SymbolAffiliation>().Cast<object>().ToArray());
        _affiliationComboBox.SelectedItem = SymbolAffiliation.Friendly.ToString();
        _affiliationComboBox.SelectedIndexChanged += (_, _) => ApplyModelToCanvas();

        _statusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusComboBox.Items.AddRange(Enum.GetNames<SymbolFrameStatus>().Cast<object>().ToArray());
        _statusComboBox.SelectedItem = SymbolFrameStatus.Present.ToString();
        _statusComboBox.SelectedIndexChanged += (_, _) => ApplyModelToCanvas();

        _unitTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _unitTypeComboBox.Items.AddRange(Enum.GetNames<OrbatUnitType>().Cast<object>().ToArray());
        _unitTypeComboBox.SelectedItem = OrbatUnitType.Armor.ToString();
        _unitTypeComboBox.SelectedIndexChanged += (_, _) => ApplyModelToCanvas();

        var resetButton = new Button { Text = "Load sample", AutoSize = true };
        resetButton.Click += (_, _) => RebuildFieldEditor(loadSample: true);

        _fieldsPanel.Dock = DockStyle.Top;
        _fieldsPanel.AutoSize = true;
        _fieldsPanel.ColumnCount = 3;
        _fieldsPanel.Padding = new Padding(0, 8, 0, 0);
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));

        var sidePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            AutoScroll = true
        };
        sidePanel.Controls.Add(_fieldsPanel);
        sidePanel.Controls.Add(CreateTopPanel(resetButton));

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 820
        };
        split.Panel1.Controls.Add(_canvas);
        split.Panel2.Controls.Add(sidePanel);

        Controls.Add(split);
        RebuildFieldEditor(loadSample: true);
    }

    private Control CreateTopPanel(Button resetButton)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0, 0, 0, 8)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(panel, "Domain", _domainComboBox);
        AddRow(panel, "Affiliation", _affiliationComboBox);
        AddRow(panel, "Status", _statusComboBox);
        AddRow(panel, "Unit type", _unitTypeComboBox);
        panel.Controls.Add(resetButton, 1, panel.RowCount);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowCount++;
        return panel;
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control control)
    {
        var row = panel.RowCount;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 7, 8, 0) }, 0, row);
        panel.Controls.Add(control, 1, row);
        panel.RowCount++;
    }

    private void RebuildFieldEditor(bool loadSample)
    {
        var existingValues = _fieldInputs.ToDictionary(pair => pair.Key, pair => pair.Value.Text, StringComparer.OrdinalIgnoreCase);
        _fieldInputs.Clear();
        _fieldsPanel.Controls.Clear();
        _fieldsPanel.RowStyles.Clear();
        _fieldsPanel.RowCount = 0;

        var layout = OrbatSymbolAmplifierLayouts.GetLayout(ToComponentDomain(GetSelectedDomain()));
        var sample = loadSample ? CreateSampleValues(layout.Domain) : existingValues;

        foreach (var field in layout.Fields)
        {
            var input = new TextBox { Dock = DockStyle.Fill };
            input.Text = sample.TryGetValue(field.Key, out var value) ? value : string.Empty;
            input.TextChanged += (_, _) => ApplyModelToCanvas();
            _fieldInputs[field.Key] = input;

            var row = _fieldsPanel.RowCount;
            _fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            _fieldsPanel.Controls.Add(new Label { Text = field.Key, AutoSize = true, Margin = new Padding(0, 6, 6, 0) }, 0, row);
            _fieldsPanel.Controls.Add(input, 1, row);
            _fieldsPanel.Controls.Add(new Label { Text = field.Area.ToString(), ForeColor = SystemColors.GrayText, AutoSize = true, Margin = new Padding(4, 6, 0, 0) }, 2, row);
            _fieldsPanel.RowCount++;
        }

        ApplyModelToCanvas();
    }

    private void ApplyModelToCanvas()
    {
        _canvas.Model = new OverlaySymbolModel
        {
            Domain = GetSelectedDomain(),
            Affiliation = GetSelectedAffiliation(),
            Status = GetSelectedStatus(),
            UnitType = GetSelectedUnitType(),
            Amplifiers = _fieldInputs.ToDictionary(pair => pair.Key, pair => pair.Value.Text, StringComparer.OrdinalIgnoreCase)
        };
        _canvas.Invalidate();
    }

    private SymbolPhysicalDomain GetSelectedDomain() =>
        Enum.TryParse(Convert.ToString(_domainComboBox.SelectedItem), out SymbolPhysicalDomain domain)
            ? domain
            : SymbolPhysicalDomain.LandUnit;

    private SymbolAffiliation GetSelectedAffiliation() =>
        Enum.TryParse(Convert.ToString(_affiliationComboBox.SelectedItem), out SymbolAffiliation affiliation)
            ? affiliation
            : SymbolAffiliation.Friendly;

    private SymbolFrameStatus GetSelectedStatus() =>
        Enum.TryParse(Convert.ToString(_statusComboBox.SelectedItem), out SymbolFrameStatus status)
            ? status
            : SymbolFrameStatus.Present;

    private OrbatUnitType GetSelectedUnitType() =>
        Enum.TryParse(Convert.ToString(_unitTypeComboBox.SelectedItem), out OrbatUnitType unitType)
            ? unitType
            : OrbatUnitType.Unspecified;

    private static OrbatSymbolDomain ToComponentDomain(SymbolPhysicalDomain domain) =>
        domain == SymbolPhysicalDomain.Equipment ? OrbatSymbolDomain.Equipment : OrbatSymbolDomain.LandUnit;

    private static Dictionary<string, string> CreateSampleValues(OrbatSymbolDomain domain)
    {
        return domain == OrbatSymbolDomain.Equipment
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "A",
                ["AO"] = "AO",
                ["C"] = "C",
                ["W/R"] = "W/R",
                ["Y/Y"] = "Y/Y",
                ["V/AD/AE"] = "V/AD/AE",
                ["T"] = "T",
                ["Z"] = "Z",
                ["G/AQ"] = "G/AQ",
                ["H/AF"] = "H/AF",
                ["J/L/N/P"] = "J/L/N/P",
                ["R/AG"] = "R/AG",
                ["AL"] = "AL",
                ["S2"] = "S2",
                ["Q"] = "Q"
            }
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A/AA"] = "A/AA",
                ["AO"] = "AO",
                ["AB"] = "AB",
                ["B/C/D"] = "B/C/D",
                ["AC"] = "AC",
                ["AR/W"] = "AR/W",
                ["X/Y"] = "X/Y",
                ["V/AD/AE"] = "V/AD/AE",
                ["C/T"] = "C/T",
                ["Z"] = "Z",
                ["F/AS"] = "F/AS",
                ["G"] = "G",
                ["H/AF"] = "H/AF",
                ["M"] = "M",
                ["J/K/P"] = "J/K/P",
                ["R/AW"] = "R/AW",
                ["AL"] = "AL",
                ["S"] = "S",
                ["S2"] = "S2",
                ["Q"] = "Q"
            };
    }
}

internal sealed class OverlaySymbolModel
{
    public SymbolPhysicalDomain Domain { get; set; } = SymbolPhysicalDomain.LandUnit;
    public SymbolAffiliation Affiliation { get; set; } = SymbolAffiliation.Friendly;
    public SymbolFrameStatus Status { get; set; } = SymbolFrameStatus.Present;
    public OrbatUnitType UnitType { get; set; } = OrbatUnitType.Armor;
    public Dictionary<string, string> Amplifiers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class OverlayCanvas : Control
{
    private readonly Font _fieldFont = new(SystemFonts.DefaultFont.FontFamily, 9f, FontStyle.Bold);
    private readonly Font _smallFont = new(SystemFonts.DefaultFont.FontFamily, 8f, FontStyle.Regular);

    public OverlayCanvas()
    {
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    public OverlaySymbolModel Model { get; set; } = new();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fieldFont.Dispose();
            _smallFont.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        DrawMapBackground(e.Graphics);

        var symbolBounds = GetSymbolBounds();
        var frameShape = SymbolFrameMapping.GetFrameShape(Model.Affiliation, Model.Domain);
        DrawSymbol(e.Graphics, symbolBounds, frameShape);
        DrawAmplifierBoxes(e.Graphics, symbolBounds);
    }

    private void DrawMapBackground(Graphics graphics)
    {
        graphics.Clear(Color.White);

        var page = RectangleF.Inflate(ClientRectangle, -28, -28);
        if (page.Width <= 0 || page.Height <= 0)
            return;

        using var border = new Pen(Color.FromArgb(64, 64, 64), 1.2f);
        using var labelBrush = new SolidBrush(Color.FromArgb(70, 70, 70));
        graphics.DrawRectangle(border, Rectangle.Round(page));
        var title = Model.Domain == SymbolPhysicalDomain.Equipment
            ? "Chapter 4 equipment amplifier overlay demo"
            : "Chapter 2 unit amplifier overlay demo";
        graphics.DrawString(title, _smallFont, labelBrush, page.Left + 12, page.Top + 10);
    }

    private RectangleF GetSymbolBounds()
    {
        var width = Model.Domain == SymbolPhysicalDomain.Equipment ? 190f : 210f;
        var height = Model.Domain == SymbolPhysicalDomain.Equipment ? 190f : 140f;
        var offsetY = Model.Domain == SymbolPhysicalDomain.Equipment ? 4f : 18f;
        return new RectangleF((Width - width) / 2f, (Height - height) / 2f + offsetY, width, height);
    }

    private void DrawSymbol(Graphics graphics, RectangleF symbolBounds, SymbolFrameShape frameShape)
    {
        using var pen = new Pen(Color.Black, 2.4f);
        SymbolFrameRenderer.DrawFrame(graphics, symbolBounds, frameShape, Model.Status, fillFrame: true, IconGuideShape.FlatTopBottom);

        var commands = BuiltInSymbolLibrary.Create(Model.UnitType);
        if (commands.Count == 0)
            commands = BuiltInSymbolLibrary.Create(OrbatUnitType.Armor);

        var drawingFrame = Model.Domain == SymbolPhysicalDomain.Equipment
            ? RectangleF.Inflate(SymbolFrameRenderer.GetInteriorFrame(symbolBounds, frameShape, IconGuideShape.FlatTopBottom), -42f, -42f)
            : SymbolFrameRenderer.GetInteriorFrame(symbolBounds, frameShape, IconGuideShape.FlatTopBottom);
        foreach (var command in commands)
        {
            SymbolFrameRenderer.DrawCommand(
                graphics,
                symbolBounds,
                SymbolFrameRenderer.GetCommandFrame(drawingFrame, frameShape, command),
                frameShape,
                command,
                pen,
                Brushes.Black,
                IconGuideShape.FlatTopBottom);
        }
    }

    private void DrawAmplifierBoxes(Graphics graphics, RectangleF symbolBounds)
    {
        if (Model.Domain == SymbolPhysicalDomain.Equipment)
        {
            DrawEquipmentAmplifierBoxes(graphics, symbolBounds);
            return;
        }

        DrawLandUnitAmplifierBoxes(graphics, symbolBounds);
    }

    private void DrawLandUnitAmplifierBoxes(Graphics graphics, RectangleF symbolBounds)
    {
        var centerX = symbolBounds.Left + symbolBounds.Width / 2f;
        var rowHeight = symbolBounds.Height / 3f;
        var sideWidth = 136f;
        var sideTop = symbolBounds.Top - rowHeight;
        DrawStack(graphics, new[] { "AR/W", "X/Y", "V/AD/AE", "C/T", "Z" }, new RectangleF(symbolBounds.Left - sideWidth, sideTop, sideWidth, rowHeight), StringAlignment.Far);
        DrawStack(graphics, new[] { "F/AS", "G", "H/AF", "M", "J/K/P" }, new RectangleF(symbolBounds.Right, sideTop, sideWidth, rowHeight), StringAlignment.Near);

        var aoBox = new RectangleF(centerX - 126, symbolBounds.Top - 158, 252, 42);
        DrawFieldBox(graphics, "AO", aoBox, StringAlignment.Center);

        using (var connectorPen = new Pen(Color.FromArgb(130, 130, 130), 1.4f))
        {
            var apex = new PointF(centerX, symbolBounds.Top - 116);
            var triangleBaseY = symbolBounds.Top;
            var innerBaseY = symbolBounds.Top - 36;
            var leftBase = new PointF(symbolBounds.Left, triangleBaseY);
            var rightBase = new PointF(symbolBounds.Right, triangleBaseY);
            var leftInner = Interpolate(apex, leftBase, (innerBaseY - apex.Y) / (leftBase.Y - apex.Y));
            var rightInner = Interpolate(apex, rightBase, (innerBaseY - apex.Y) / (rightBase.Y - apex.Y));

            graphics.DrawLine(connectorPen, new PointF(aoBox.Left + aoBox.Width / 2f, aoBox.Bottom), apex);
            graphics.DrawLine(connectorPen, apex, leftBase);
            graphics.DrawLine(connectorPen, apex, rightBase);
            graphics.DrawLine(connectorPen, leftInner, rightInner);
        }

        DrawFieldText(graphics, "AB", new RectangleF(centerX - 58, symbolBounds.Top - 92, 116, 32), StringAlignment.Center);
        DrawFieldBox(graphics, "B/C/D", new RectangleF(centerX - 74, symbolBounds.Top - 58, 148, 28), StringAlignment.Center);
        DrawFieldBox(graphics, "AC", new RectangleF(centerX - 52, symbolBounds.Top - 30, 104, 30), StringAlignment.Center);

        DrawFieldBox(graphics, "A/AA", new RectangleF(centerX - 48, symbolBounds.Top + symbolBounds.Height / 2f - 22, 96, 44), StringAlignment.Center);
        var rawBox = Model.Affiliation == SymbolAffiliation.Friendly
            ? new RectangleF(centerX - 78, symbolBounds.Bottom, 156, 30)
            : new RectangleF(centerX - 52, symbolBounds.Bottom, 104, 30);
        var alBox = Model.Affiliation == SymbolAffiliation.Friendly
            ? new RectangleF(centerX - 105, rawBox.Bottom, 210, 32)
            : new RectangleF(centerX - 80, symbolBounds.Bottom + 42, 160, 32);
        DrawFieldBox(graphics, "R/AW", rawBox, StringAlignment.Center);
        DrawFieldBox(graphics, "AL", alBox, StringAlignment.Center);
        DrawLandUnitConnectors(graphics, symbolBounds, alBox);
    }

    private void DrawEquipmentAmplifierBoxes(Graphics graphics, RectangleF symbolBounds)
    {
        var centerX = symbolBounds.Left + symbolBounds.Width / 2f;
        var rowHeight = 42f;
        var sideWidth = 126f;
        var sideTop = symbolBounds.Top + 2;

        DrawStack(graphics, new[] { "W/R", "Y/Y", "V/AD/AE", "T", "Z" }, new RectangleF(symbolBounds.Left - sideWidth - 42, sideTop, sideWidth, rowHeight), StringAlignment.Far);
        DrawStack(graphics, new[] { "G/AQ", "H/AF", "J/L/N/P" }, new RectangleF(symbolBounds.Right + 42, sideTop + rowHeight, sideWidth, rowHeight), StringAlignment.Near);

        DrawStack(graphics, new[] { "AO", "C" }, new RectangleF(centerX - 64, symbolBounds.Top - 96, 128, 42), StringAlignment.Center);
        DrawFieldBox(graphics, "A", new RectangleF(centerX - 26, symbolBounds.Top + symbolBounds.Height / 2f - 26, 52, 52), StringAlignment.Center);
        var bottomStack = new RectangleF(centerX - 64, symbolBounds.Bottom + 10, 128, 42);
        DrawStack(graphics, new[] { "R/AG", "AL" }, bottomStack, StringAlignment.Center);
        DrawEquipmentConnectors(graphics, symbolBounds, new RectangleF(bottomStack.Left, bottomStack.Top + rowHeight, bottomStack.Width, rowHeight));
    }

    private void DrawStack(Graphics graphics, IReadOnlyList<string> keys, RectangleF firstBox, StringAlignment alignment)
    {
        for (var index = 0; index < keys.Count; index++)
        {
            var box = new RectangleF(firstBox.Left, firstBox.Top + index * firstBox.Height, firstBox.Width, firstBox.Height);
            DrawFieldBox(graphics, keys[index], box, alignment);
        }
    }

    private static PointF Interpolate(PointF start, PointF end, float t) =>
        new(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);

    private void DrawLandUnitConnectors(Graphics graphics, RectangleF symbolBounds, RectangleF alBox)
    {
        var centerX = symbolBounds.Left + symbolBounds.Width / 2f;
        var sX = Model.Affiliation == SymbolAffiliation.Friendly ? symbolBounds.Left : centerX;
        var verticalTop = symbolBounds.Bottom;
        var verticalBottom = symbolBounds.Bottom + 104;

        using var linePen = new Pen(Color.Black, 1.7f);
        graphics.DrawLine(linePen, sX, verticalTop, sX, verticalBottom);
        graphics.DrawLine(linePen, sX, verticalBottom, sX - 76, verticalBottom + 30);

        using var arrowPen = new Pen(Color.Black, 1.7f) { EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor };
        if (Model.Affiliation == SymbolAffiliation.Friendly)
        {
            var qDropX = centerX;
            var qDropTop = alBox.Bottom;
            var qY = alBox.Bottom + 28;
            graphics.DrawLine(linePen, qDropX, qDropTop, qDropX, qY);
            graphics.DrawLine(arrowPen, qDropX, qY, qDropX + 88, qY);
            DrawFieldText(graphics, "Q", new RectangleF(qDropX + 96, qY - 12, 48, 26), StringAlignment.Near);
        }
        else
        {
            var qStartX = centerX;
            graphics.DrawLine(arrowPen, qStartX, symbolBounds.Bottom + 72, qStartX + 76, symbolBounds.Bottom + 72);
            DrawFieldText(graphics, "Q", new RectangleF(qStartX + 84, symbolBounds.Bottom + 60, 48, 26), StringAlignment.Near);
        }

        DrawFieldText(graphics, "S", new RectangleF(sX + 6, verticalBottom - 14, 40, 26), StringAlignment.Near);
        DrawFieldText(graphics, "S2", new RectangleF(sX - 106, verticalBottom + 14, 52, 26), StringAlignment.Near);
    }

    private void DrawEquipmentConnectors(Graphics graphics, RectangleF symbolBounds, RectangleF alBox)
    {
        var centerX = symbolBounds.Left + symbolBounds.Width / 2f;
        var start = new PointF(symbolBounds.Left + 36, symbolBounds.Bottom - 10);
        var elbow = new PointF(symbolBounds.Left - 14, symbolBounds.Bottom + 138);
        var s2End = new PointF(elbow.X - 72, elbow.Y);

        using var linePen = new Pen(Color.Black, 1.7f);
        graphics.DrawLine(linePen, start, elbow);
        graphics.DrawLine(linePen, elbow, s2End);

        using var arrowPen = new Pen(Color.Black, 1.7f) { EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor };
        var qDropX = centerX;
        var qY = alBox.Bottom + 42;
        graphics.DrawLine(linePen, qDropX, alBox.Bottom, qDropX, qY);
        graphics.DrawLine(arrowPen, qDropX, qY, qDropX + 112, qY);

        DrawFieldText(graphics, "S2", new RectangleF(s2End.X - 42, s2End.Y - 16, 52, 28), StringAlignment.Near);
        DrawFieldText(graphics, "Q", new RectangleF(qDropX + 120, qY - 14, 48, 28), StringAlignment.Near);
    }

    private void DrawFieldBox(Graphics graphics, string key, RectangleF box, StringAlignment alignment)
    {
        if (!TryGetValue(key, out var value))
            return;

        DrawBox(graphics, box, value, alignment);
    }

    private void DrawFieldText(Graphics graphics, string key, RectangleF box, StringAlignment alignment)
    {
        if (!TryGetValue(key, out var value))
            return;

        using var brush = new SolidBrush(Color.Black);
        using var format = new StringFormat(StringFormatFlags.NoWrap) { Alignment = alignment, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        graphics.DrawString(value, _fieldFont, brush, box, format);
    }

    private void DrawFieldTrapezoid(Graphics graphics, string key, RectangleF box)
    {
        if (!TryGetValue(key, out var value))
            return;

        var inset = box.Width * 0.18f;
        PointF[] points =
        {
            new(box.Left + inset, box.Top),
            new(box.Right - inset, box.Top),
            new(box.Right, box.Bottom),
            new(box.Left, box.Bottom)
        };

        using var fill = new SolidBrush(Color.White);
        using var border = new Pen(Color.FromArgb(148, 148, 148), 1.4f);
        graphics.FillPolygon(fill, points);
        graphics.DrawPolygon(border, points);
        DrawFieldTextValue(graphics, value, box, StringAlignment.Center);
    }

    private void DrawFieldTextValue(Graphics graphics, string value, RectangleF box, StringAlignment alignment)
    {
        using var brush = new SolidBrush(Color.Black);
        using var format = new StringFormat(StringFormatFlags.NoWrap) { Alignment = alignment, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        graphics.DrawString(value, _fieldFont, brush, box, format);
    }

    private bool TryGetValue(string key, out string value)
    {
        value = Model.Amplifiers.TryGetValue(key, out var raw) ? raw : string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private void DrawBox(Graphics graphics, RectangleF box, string value, StringAlignment alignment)
    {
        using var fill = new SolidBrush(Color.White);
        using var border = new Pen(Color.FromArgb(148, 148, 148), 1.4f);
        using var brush = new SolidBrush(Color.Black);
        using var format = new StringFormat(StringFormatFlags.NoWrap) { Alignment = alignment, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        graphics.FillRectangle(fill, box);
        graphics.DrawRectangle(border, Rectangle.Round(box));
        var textBox = RectangleF.Inflate(box, -7, 0);
        graphics.DrawString(value, _fieldFont, brush, textBox, format);
    }
}
