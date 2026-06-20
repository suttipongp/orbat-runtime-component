using System.Drawing.Drawing2D;
using System.Text;

namespace OrgHierarchy.Demo;

public sealed class SymbolDesignerForm : Form
{
    private readonly SymbolDesignerCanvas _canvas = new();
    private readonly SymbolPreviewControl _preview = new();
    private readonly ComboBox _toolComboBox = new();
    private readonly ComboBox _unitTypeComboBox = new();
    private readonly TrackBar _referenceOpacityTrackBar = new();
    private readonly ListBox _commandListBox = new();
    private readonly TextBox _codeTextBox = new();

    public SymbolDesignerForm()
    {
        Text = "ORBAT Symbol Designer";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1060, 720);
        Size = new Size(1180, 780);

        _toolComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _toolComboBox.Items.AddRange(Enum.GetNames<SymbolDesignerTool>().Cast<object>().ToArray());
        _toolComboBox.SelectedItem = SymbolDesignerTool.Line.ToString();
        _toolComboBox.SelectedIndexChanged += (_, _) => _canvas.Tool = GetSelectedTool();

        _unitTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _unitTypeComboBox.Items.AddRange(Enum.GetNames<Components.OrbatUnitType>().Cast<object>().ToArray());
        _unitTypeComboBox.SelectedItem = Components.OrbatUnitType.Unspecified.ToString();
        _unitTypeComboBox.SelectedIndexChanged += (_, _) => RefreshOutput();

        _referenceOpacityTrackBar.Minimum = 0;
        _referenceOpacityTrackBar.Maximum = 100;
        _referenceOpacityTrackBar.TickFrequency = 10;
        _referenceOpacityTrackBar.Value = 35;
        _referenceOpacityTrackBar.Width = 140;
        _referenceOpacityTrackBar.ValueChanged += (_, _) =>
        {
            _canvas.ReferenceOpacity = _referenceOpacityTrackBar.Value / 100f;
            _canvas.Invalidate();
        };

        var loadButton = CreateButton("Load reference", LoadReferenceImage);
        var undoButton = CreateButton("Undo", () => _canvas.Undo());
        var clearButton = CreateButton("Clear", () => _canvas.ClearCommands());
        var airDefenseButton = CreateButton("Air defense arc", AddAirDefenseArc);
        var copyCodeButton = CreateButton("Copy C# code", CopyCode);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 8, 8, 4),
            WrapContents = false
        };
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Unit type", Margin = new Padding(0, 6, 4, 0) });
        toolbar.Controls.Add(_unitTypeComboBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Tool", Margin = new Padding(14, 6, 4, 0) });
        toolbar.Controls.Add(_toolComboBox);
        toolbar.Controls.Add(loadButton);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Reference", Margin = new Padding(14, 6, 4, 0) });
        toolbar.Controls.Add(_referenceOpacityTrackBar);
        toolbar.Controls.Add(undoButton);
        toolbar.Controls.Add(clearButton);
        toolbar.Controls.Add(airDefenseButton);
        toolbar.Controls.Add(copyCodeButton);

        _canvas.Dock = DockStyle.Fill;
        _canvas.ReferenceOpacity = _referenceOpacityTrackBar.Value / 100f;
        _canvas.CommandsChanged += (_, _) => RefreshOutput();

        _preview.Dock = DockStyle.Fill;
        _preview.BackColor = Color.White;

        _commandListBox.Dock = DockStyle.Fill;
        _commandListBox.IntegralHeight = false;

        _codeTextBox.Dock = DockStyle.Fill;
        _codeTextBox.Multiline = true;
        _codeTextBox.ScrollBars = ScrollBars.Both;
        _codeTextBox.WordWrap = false;
        _codeTextBox.Font = new Font(FontFamily.GenericMonospace, 9f);

        var rightTabs = new TabControl { Dock = DockStyle.Fill };
        var previewTab = new TabPage("Preview") { Padding = new Padding(8) };
        var commandsTab = new TabPage("Commands") { Padding = new Padding(8) };
        var codeTab = new TabPage("C# code") { Padding = new Padding(8) };
        previewTab.Controls.Add(_preview);
        commandsTab.Controls.Add(_commandListBox);
        codeTab.Controls.Add(_codeTextBox);
        rightTabs.Controls.Add(previewTab);
        rightTabs.Controls.Add(commandsTab);
        rightTabs.Controls.Add(codeTab);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 760
        };
        split.Panel1.Controls.Add(_canvas);
        split.Panel2.Controls.Add(rightTabs);

        Controls.Add(split);
        Controls.Add(toolbar);

        RefreshOutput();
    }

    private static Button CreateButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 28,
            Margin = new Padding(8, 0, 0, 0),
            UseVisualStyleBackColor = true
        };
        button.Click += (_, _) => action();
        return button;
    }

    private SymbolDesignerTool GetSelectedTool()
    {
        return Enum.TryParse(Convert.ToString(_toolComboBox.SelectedItem), out SymbolDesignerTool tool)
            ? tool
            : SymbolDesignerTool.Line;
    }

    private void LoadReferenceImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load symbol reference image",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _canvas.LoadReferenceImage(dialog.FileName);
    }

    private void AddAirDefenseArc()
    {
        _canvas.AddCommand(SymbolDrawCommand.AirDefenseArc());
    }

    private void CopyCode()
    {
        Clipboard.SetText(_codeTextBox.Text);
    }

    private void RefreshOutput()
    {
        var commands = _canvas.Commands.ToArray();
        _preview.SetCommands(commands);
        _commandListBox.Items.Clear();
        foreach (var command in commands)
            _commandListBox.Items.Add(command.GetSummary());

        _codeTextBox.Text = GenerateCode(commands);
    }

    private string GenerateCode(IReadOnlyList<SymbolDrawCommand> commands)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"// {_unitTypeComboBox.SelectedItem} symbol");
        builder.AppendLine("// Paste these lines inside DrawUnitIcon, using the existing graphics, pen, brush, bounds and icon variables.");
        foreach (var command in commands)
            builder.AppendLine(command.ToCSharp("graphics", "pen", "brush", "bounds", "icon"));

        return builder.ToString();
    }
}

internal enum SymbolDesignerTool
{
    Line,
    Rectangle,
    Ellipse,
    Capsule,
    Dot,
    Text,
    Arc,
    BezierArc
}

internal sealed class SymbolDesignerCanvas : Control
{
    private readonly List<SymbolDrawCommand> _commands = new();
    private Bitmap? _referenceImage;
    private PointF? _dragStart;
    private PointF? _dragCurrent;

    public event EventHandler? CommandsChanged;

    public SymbolDesignerTool Tool { get; set; } = SymbolDesignerTool.Line;
    public float ReferenceOpacity { get; set; } = 0.35f;
    public IReadOnlyList<SymbolDrawCommand> Commands => _commands;

    public SymbolDesignerCanvas()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        Cursor = Cursors.Cross;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public void LoadReferenceImage(string fileName)
    {
        _referenceImage?.Dispose();
        _referenceImage = new Bitmap(fileName);
        Invalidate();
    }

    public void AddCommand(SymbolDrawCommand command)
    {
        _commands.Add(command);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void Undo()
    {
        if (_commands.Count == 0)
            return;

        _commands.RemoveAt(_commands.Count - 1);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void ClearCommands()
    {
        _commands.Clear();
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;

        _dragStart = ToSymbolPoint(e.Location);
        _dragCurrent = _dragStart;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragStart.HasValue)
            return;

        _dragCurrent = ToSymbolPoint(e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || !_dragStart.HasValue)
            return;

        var end = ToSymbolPoint(e.Location);
        var command = CreateCommand(_dragStart.Value, end);
        _dragStart = null;
        _dragCurrent = null;

        if (command != null)
            AddCommand(command);
        else
            Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var frame = GetFrameBounds();
        e.Graphics.FillRectangle(Brushes.White, frame);
        DrawReference(e.Graphics, frame);
        DrawFrame(e.Graphics, frame);

        foreach (var command in _commands)
            command.Draw(e.Graphics, frame, Pens.Black, Brushes.Black);

        if (_dragStart.HasValue && _dragCurrent.HasValue)
        {
            using var previewPen = new Pen(Color.FromArgb(180, Color.Goldenrod), 1f) { DashStyle = DashStyle.Dash };
            CreateCommand(_dragStart.Value, _dragCurrent.Value)?.Draw(e.Graphics, frame, previewPen, Brushes.Goldenrod);
        }
    }

    private void DrawReference(Graphics graphics, RectangleF frame)
    {
        if (_referenceImage == null)
            return;

        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = ReferenceOpacity };
        attributes.SetColorMatrix(matrix);
        var destination = Rectangle.Round(frame);
        graphics.DrawImage(
            _referenceImage,
            destination,
            0,
            0,
            _referenceImage.Width,
            _referenceImage.Height,
            GraphicsUnit.Pixel,
            attributes);
    }

    private static void DrawFrame(Graphics graphics, RectangleF frame)
    {
        using var framePen = new Pen(Color.Black, 2f);
        graphics.DrawRectangle(framePen, Rectangle.Round(frame));
    }

    private SymbolDrawCommand? CreateCommand(PointF start, PointF end)
    {
        if (Distance(start, end) < 0.012f && Tool != SymbolDesignerTool.Dot && Tool != SymbolDesignerTool.Text)
            return null;

        return Tool switch
        {
            SymbolDesignerTool.Line => SymbolDrawCommand.Line(start, end),
            SymbolDesignerTool.Rectangle => SymbolDrawCommand.Rectangle(start, end),
            SymbolDesignerTool.Ellipse => SymbolDrawCommand.Ellipse(start, end),
            SymbolDesignerTool.Capsule => SymbolDrawCommand.Capsule(start, end),
            SymbolDesignerTool.Dot => SymbolDrawCommand.Dot(end, 0.08f),
            SymbolDesignerTool.Text => SymbolDrawCommand.TextCommand(end, "TXT"),
            SymbolDesignerTool.Arc => SymbolDrawCommand.Arc(start, end),
            SymbolDesignerTool.BezierArc => SymbolDrawCommand.BezierArc(start, end),
            _ => null
        };
    }

    private PointF ToSymbolPoint(Point point)
    {
        var frame = GetFrameBounds();
        var x = Math.Clamp((point.X - frame.Left) / frame.Width, 0f, 1f);
        var y = Math.Clamp((point.Y - frame.Top) / frame.Height, 0f, 1f);
        return new PointF(x, y);
    }

    private RectangleF GetFrameBounds()
    {
        var maxWidth = ClientSize.Width - 80;
        var maxHeight = ClientSize.Height - 100;
        var width = Math.Min(maxWidth, maxHeight * 1.6f);
        var height = width / 1.6f;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * 1.6f;
        }

        return new RectangleF(
            (ClientSize.Width - width) / 2f,
            (ClientSize.Height - height) / 2f,
            width,
            height);
    }

    private static float Distance(PointF first, PointF second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}

internal sealed class SymbolPreviewControl : Control
{
    private IReadOnlyList<SymbolDrawCommand> _commands = Array.Empty<SymbolDrawCommand>();

    public SymbolPreviewControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public void SetCommands(IReadOnlyList<SymbolDrawCommand> commands)
    {
        _commands = commands;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Color.White);

        var width = Math.Min(ClientSize.Width - 60, 280);
        var height = width / 1.6f;
        var frame = new RectangleF((ClientSize.Width - width) / 2f, 80, width, height);

        using var pen = new Pen(Color.Black, 2f);
        using var fill = new SolidBrush(Color.FromArgb(126, 211, 236));
        e.Graphics.FillRectangle(fill, frame);
        e.Graphics.DrawRectangle(pen, Rectangle.Round(frame));
        foreach (var command in _commands)
            command.Draw(e.Graphics, frame, pen, Brushes.Black);

        TextRenderer.DrawText(
            e.Graphics,
            "Preview",
            Font,
            new Rectangle(0, 24, ClientSize.Width, 24),
            SystemColors.ControlText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed record SymbolDrawCommand(
    SymbolDrawCommandKind Kind,
    PointF Start,
    PointF End,
    PointF Control1,
    PointF Control2,
    float Radius,
    string Text)
{
    public static SymbolDrawCommand Line(PointF start, PointF end) =>
        new(SymbolDrawCommandKind.Line, start, end, PointF.Empty, PointF.Empty, 0f, string.Empty);

    public static SymbolDrawCommand Rectangle(PointF start, PointF end) =>
        new(SymbolDrawCommandKind.Rectangle, start, end, PointF.Empty, PointF.Empty, 0f, string.Empty);

    public static SymbolDrawCommand Ellipse(PointF start, PointF end) =>
        new(SymbolDrawCommandKind.Ellipse, start, end, PointF.Empty, PointF.Empty, 0f, string.Empty);

    public static SymbolDrawCommand Capsule(PointF start, PointF end) =>
        new(SymbolDrawCommandKind.Capsule, start, end, PointF.Empty, PointF.Empty, 0f, string.Empty);

    public static SymbolDrawCommand Dot(PointF center, float radius) =>
        new(SymbolDrawCommandKind.Dot, center, center, PointF.Empty, PointF.Empty, radius, string.Empty);

    public static SymbolDrawCommand TextCommand(PointF location, string text) =>
        new(SymbolDrawCommandKind.Text, location, location, PointF.Empty, PointF.Empty, 0f, text);

    public static SymbolDrawCommand Arc(PointF start, PointF end) =>
        new(SymbolDrawCommandKind.Arc, start, end, PointF.Empty, PointF.Empty, 0f, string.Empty);

    public static SymbolDrawCommand BezierArc(PointF start, PointF end)
    {
        var rise = Math.Abs(end.Y - start.Y) * 0.45f;
        var top = Math.Min(start.Y, end.Y) - rise;
        return new(
            SymbolDrawCommandKind.Bezier,
            start,
            end,
            new PointF(start.X + (end.X - start.X) * 0.25f, top),
            new PointF(start.X + (end.X - start.X) * 0.75f, top),
            0f,
            string.Empty);
    }

    public static SymbolDrawCommand AirDefenseArc() =>
        new(
            SymbolDrawCommandKind.Bezier,
            new PointF(0f, 1f),
            new PointF(1f, 1f),
            new PointF(0.25f, 0.77f),
            new PointF(0.75f, 0.77f),
            0f,
            string.Empty);

    public void Draw(Graphics graphics, RectangleF frame, Pen pen, Brush brush)
    {
        switch (Kind)
        {
            case SymbolDrawCommandKind.Line:
                graphics.DrawLine(pen, ToAbsolute(frame, Start), ToAbsolute(frame, End));
                break;
            case SymbolDrawCommandKind.Rectangle:
                graphics.DrawRectangle(pen, System.Drawing.Rectangle.Round(ToRectangle(frame)));
                break;
            case SymbolDrawCommandKind.Ellipse:
                graphics.DrawEllipse(pen, ToRectangle(frame));
                break;
            case SymbolDrawCommandKind.Capsule:
                DrawCapsule(graphics, pen, ToRectangle(frame));
                break;
            case SymbolDrawCommandKind.Dot:
                var center = ToAbsolute(frame, Start);
                var dotRadius = Radius * Math.Min(frame.Width, frame.Height);
                graphics.FillEllipse(brush, center.X - dotRadius, center.Y - dotRadius, dotRadius * 2f, dotRadius * 2f);
                break;
            case SymbolDrawCommandKind.Text:
                var location = ToAbsolute(frame, Start);
                using (var font = new Font(SystemFonts.DefaultFont.FontFamily, 12f, FontStyle.Bold))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    graphics.DrawString(Text, font, brush, new RectangleF(location.X - 34f, location.Y - 14f, 68f, 28f), format);
                break;
            case SymbolDrawCommandKind.Arc:
                graphics.DrawArc(pen, ToRectangle(frame), 200f, 140f);
                break;
            case SymbolDrawCommandKind.Bezier:
                graphics.DrawBezier(pen, ToAbsolute(frame, Start), ToAbsolute(frame, Control1), ToAbsolute(frame, Control2), ToAbsolute(frame, End));
                break;
        }
    }

    public string GetSummary()
    {
        return Kind switch
        {
            SymbolDrawCommandKind.Dot => $"{Kind} at {FormatPoint(Start)}",
            SymbolDrawCommandKind.Text => $"{Kind} \"{Text}\" at {FormatPoint(Start)}",
            _ => $"{Kind} {FormatPoint(Start)} to {FormatPoint(End)}"
        };
    }

    public string ToCSharp(string graphics, string pen, string brush, string bounds, string icon)
    {
        return Kind switch
        {
            SymbolDrawCommandKind.Line =>
                $"{graphics}.DrawLine({pen}, {PointCode(bounds, Start)}, {PointCode(bounds, End)});",
            SymbolDrawCommandKind.Rectangle =>
                $"{graphics}.DrawRectangle({pen}, Rectangle.Round({RectCode(bounds)}));",
            SymbolDrawCommandKind.Ellipse =>
                $"{graphics}.DrawEllipse({pen}, {RectCode(bounds)});",
            SymbolDrawCommandKind.Capsule =>
                $"DrawCapsule({graphics}, {pen}, {RectCode(bounds)});",
            SymbolDrawCommandKind.Dot =>
                $"{graphics}.FillEllipse({brush}, {PointCode(bounds, Start)}.X - {RadiusCode(bounds)}, {PointCode(bounds, Start)}.Y - {RadiusCode(bounds)}, {RadiusCode(bounds)} * 2f, {RadiusCode(bounds)} * 2f);",
            SymbolDrawCommandKind.Text =>
                $"{graphics}.DrawString(\"{Text}\", font, {brush}, {icon}, centerFormat);",
            SymbolDrawCommandKind.Arc =>
                $"{graphics}.DrawArc({pen}, {RectCode(bounds)}, 200f, 140f);",
            SymbolDrawCommandKind.Bezier =>
                $"{graphics}.DrawBezier({pen}, {PointCode(bounds, Start)}, {PointCode(bounds, Control1)}, {PointCode(bounds, Control2)}, {PointCode(bounds, End)});",
            _ => string.Empty
        };
    }

    private static void DrawCapsule(Graphics graphics, Pen pen, RectangleF rect)
    {
        var radius = rect.Height / 2f;
        using var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, radius * 2f, rect.Height, 90, 180);
        path.AddArc(rect.Right - radius * 2f, rect.Top, radius * 2f, rect.Height, 270, 180);
        path.CloseFigure();
        graphics.DrawPath(pen, path);
    }

    private RectangleF ToRectangle(RectangleF frame)
    {
        var first = ToAbsolute(frame, Start);
        var second = ToAbsolute(frame, End);
        var left = Math.Min(first.X, second.X);
        var top = Math.Min(first.Y, second.Y);
        return new RectangleF(left, top, Math.Abs(second.X - first.X), Math.Abs(second.Y - first.Y));
    }

    private static PointF ToAbsolute(RectangleF frame, PointF point) =>
        new(frame.Left + frame.Width * point.X, frame.Top + frame.Height * point.Y);

    private static string PointCode(string bounds, PointF point) =>
        $"new PointF({bounds}.Left + {bounds}.Width * {Format(point.X)}f, {bounds}.Top + {bounds}.Height * {Format(point.Y)}f)";

    private string RectCode(string bounds)
    {
        var left = Math.Min(Start.X, End.X);
        var top = Math.Min(Start.Y, End.Y);
        var width = Math.Abs(End.X - Start.X);
        var height = Math.Abs(End.Y - Start.Y);
        return $"new RectangleF({bounds}.Left + {bounds}.Width * {Format(left)}f, {bounds}.Top + {bounds}.Height * {Format(top)}f, {bounds}.Width * {Format(width)}f, {bounds}.Height * {Format(height)}f)";
    }

    private string RadiusCode(string bounds) => $"Math.Min({bounds}.Width, {bounds}.Height) * {Format(Radius)}f";

    private static string FormatPoint(PointF point) => $"({Format(point.X)}, {Format(point.Y)})";

    private static string Format(float value) => value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}

internal enum SymbolDrawCommandKind
{
    Line,
    Rectangle,
    Ellipse,
    Capsule,
    Dot,
    Text,
    Arc,
    Bezier
}
