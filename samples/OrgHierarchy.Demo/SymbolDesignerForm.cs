using System.Drawing.Drawing2D;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrgHierarchy.Demo;

public sealed class SymbolDesignerForm : Form
{
    private readonly SymbolDesignerCanvas _canvas = new();
    private readonly SymbolPreviewControl _preview = new();
    private readonly ComboBox _toolComboBox = new();
    private readonly ComboBox _unitTypeComboBox = new();
    private readonly TrackBar _referenceOpacityTrackBar = new();
    private readonly CheckBox _showGridCheckBox = new() { Text = "Grid", Checked = true, AutoSize = true };
    private readonly CheckBox _showIconGuideCheckBox = new() { Text = "Icon guide", Checked = true, AutoSize = true };
    private readonly CheckBox _snapCheckBox = new() { Text = "Snap", Checked = true, AutoSize = true };
    private readonly CheckBox _fillCheckBox = new() { Text = "Fill closed", AutoSize = true };
    private readonly NumericUpDown _gridDivisionsInput = new();
    private readonly Label _statusLabel = new() { Dock = DockStyle.Fill, ForeColor = SystemColors.GrayText, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ListBox _commandListBox = new();
    private readonly TextBox _codeTextBox = new();
    private readonly NumericUpDown _startXInput = CreateCoordinateInput();
    private readonly NumericUpDown _startYInput = CreateCoordinateInput();
    private readonly NumericUpDown _endXInput = CreateCoordinateInput();
    private readonly NumericUpDown _endYInput = CreateCoordinateInput();
    private readonly NumericUpDown _control1XInput = CreateCoordinateInput();
    private readonly NumericUpDown _control1YInput = CreateCoordinateInput();
    private readonly NumericUpDown _control2XInput = CreateCoordinateInput();
    private readonly NumericUpDown _control2YInput = CreateCoordinateInput();
    private readonly NumericUpDown _radiusInput = CreateCoordinateInput();
    private readonly TextBox _textInput = new();
    private readonly TextBox _drawTextInput = new() { Text = "TXT", Width = 80 };
    private bool _updatingSelectionControls;

    public SymbolDesignerForm()
    {
        Text = "ORBAT Symbol Designer";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1120, 740);
        Size = new Size(1240, 800);

        _toolComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _toolComboBox.Items.AddRange(Enum.GetNames<SymbolDesignerTool>().Cast<object>().ToArray());
        _toolComboBox.SelectedItem = SymbolDesignerTool.Line.ToString();
        _toolComboBox.SelectedIndexChanged += (_, _) =>
        {
            _canvas.Tool = GetSelectedTool();
            UpdateToolStatus();
        };

        _unitTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _unitTypeComboBox.Items.AddRange(Enum.GetNames<Components.OrbatUnitType>().Cast<object>().ToArray());
        _unitTypeComboBox.SelectedItem = Components.OrbatUnitType.Unspecified.ToString();
        _unitTypeComboBox.SelectedIndexChanged += (_, _) => RefreshOutput();

        _referenceOpacityTrackBar.Minimum = 0;
        _referenceOpacityTrackBar.Maximum = 100;
        _referenceOpacityTrackBar.TickFrequency = 10;
        _referenceOpacityTrackBar.Value = 35;
        _referenceOpacityTrackBar.Width = 120;
        _referenceOpacityTrackBar.ValueChanged += (_, _) =>
        {
            _canvas.ReferenceOpacity = _referenceOpacityTrackBar.Value / 100f;
            _canvas.Invalidate();
        };

        _gridDivisionsInput.Minimum = 4;
        _gridDivisionsInput.Maximum = 40;
        _gridDivisionsInput.Value = 10;
        _gridDivisionsInput.Width = 52;
        _gridDivisionsInput.ValueChanged += (_, _) =>
        {
            _canvas.GridDivisions = (int)_gridDivisionsInput.Value;
            _canvas.Invalidate();
        };

        _showGridCheckBox.CheckedChanged += (_, _) =>
        {
            _canvas.ShowGrid = _showGridCheckBox.Checked;
            _canvas.Invalidate();
        };
        _showIconGuideCheckBox.CheckedChanged += (_, _) =>
        {
            _canvas.ShowIconGuide = _showIconGuideCheckBox.Checked;
            _canvas.Invalidate();
        };
        _snapCheckBox.CheckedChanged += (_, _) => _canvas.SnapEnabled = _snapCheckBox.Checked;
        _fillCheckBox.CheckedChanged += (_, _) =>
        {
            _canvas.FillClosedShapes = _fillCheckBox.Checked;
            ApplyFillOptionToSelection();
        };
        _drawTextInput.TextChanged += (_, _) => _canvas.DrawText = _drawTextInput.Text;

        var loadButton = CreateButton("Load reference", LoadReferenceImage);
        var loadClipboardButton = CreateButton("Load clipboard", LoadReferenceFromClipboard);
        var loadBaseButton = CreateButton("Load base", LoadBaseSymbol);
        var saveLibraryButton = CreateButton("Save library", SaveLibrary);
        var loadLibraryButton = CreateButton("Load library", LoadLibrary);
        var undoButton = CreateButton("Undo", () => _canvas.Undo());
        var deleteButton = CreateButton("Delete", DeleteSelectedCommand);
        var clearButton = CreateButton("Clear", () => _canvas.ClearCommands());
        var airDefenseButton = CreateButton("Air defense arc", AddAirDefenseArc);
        var copyCodeButton = CreateButton("Copy C# code", CopyCode);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoScroll = true,
            Height = 116,
            Padding = new Padding(8, 8, 8, 4),
            WrapContents = true
        };
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Unit type", Margin = new Padding(0, 6, 4, 0) });
        toolbar.Controls.Add(_unitTypeComboBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Tool", Margin = new Padding(14, 6, 4, 0) });
        toolbar.Controls.Add(_toolComboBox);
        toolbar.Controls.Add(loadButton);
        toolbar.Controls.Add(loadClipboardButton);
        toolbar.Controls.Add(loadBaseButton);
        toolbar.Controls.Add(saveLibraryButton);
        toolbar.Controls.Add(loadLibraryButton);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Reference", Margin = new Padding(14, 6, 4, 0) });
        toolbar.Controls.Add(_referenceOpacityTrackBar);
        toolbar.Controls.Add(_showGridCheckBox);
        toolbar.Controls.Add(_showIconGuideCheckBox);
        toolbar.Controls.Add(_snapCheckBox);
        toolbar.Controls.Add(_fillCheckBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Grid", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_gridDivisionsInput);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Text", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_drawTextInput);
        toolbar.Controls.Add(undoButton);
        toolbar.Controls.Add(deleteButton);
        toolbar.Controls.Add(clearButton);
        toolbar.Controls.Add(airDefenseButton);
        toolbar.Controls.Add(copyCodeButton);

        var statusPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            Padding = new Padding(10, 0, 10, 4)
        };
        statusPanel.Controls.Add(_statusLabel);

        _canvas.Dock = DockStyle.Fill;
        _canvas.ReferenceOpacity = _referenceOpacityTrackBar.Value / 100f;
        _canvas.GridDivisions = (int)_gridDivisionsInput.Value;
        _canvas.ShowGrid = _showGridCheckBox.Checked;
        _canvas.ShowIconGuide = _showIconGuideCheckBox.Checked;
        _canvas.SnapEnabled = _snapCheckBox.Checked;
        _canvas.FillClosedShapes = _fillCheckBox.Checked;
        _canvas.DrawText = _drawTextInput.Text;
        _canvas.CommandsChanged += (_, _) =>
        {
            RefreshOutput();
            if (_canvas.SelectedIndex >= 0 && GetSelectedTool() != SymbolDesignerTool.SelectMove)
                SelectTool(SymbolDesignerTool.SelectMove);
        };
        _canvas.SelectionChanged += (_, _) => RefreshSelectionControls();

        _preview.Dock = DockStyle.Fill;
        _preview.BackColor = Color.White;

        _commandListBox.Dock = DockStyle.Fill;
        _commandListBox.IntegralHeight = false;
        _commandListBox.SelectedIndexChanged += (_, _) =>
        {
            if (_commandListBox.SelectedIndex != _canvas.SelectedIndex)
                _canvas.SelectCommand(_commandListBox.SelectedIndex);
            if (_commandListBox.SelectedIndex >= 0)
                SelectTool(SymbolDesignerTool.SelectMove);
        };

        WireSelectionInputs();

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
        commandsTab.Controls.Add(CreateCommandsPanel());
        codeTab.Controls.Add(_codeTextBox);
        rightTabs.Controls.Add(previewTab);
        rightTabs.Controls.Add(commandsTab);
        rightTabs.Controls.Add(codeTab);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 780
        };
        split.Panel1.Controls.Add(_canvas);
        split.Panel2.Controls.Add(rightTabs);

        Controls.Add(split);
        Controls.Add(statusPanel);
        Controls.Add(toolbar);

        RefreshOutput();
        RefreshSelectionControls();
        UpdateToolStatus();
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

    private static NumericUpDown CreateCoordinateInput()
    {
        return new NumericUpDown
        {
            DecimalPlaces = 3,
            Increment = 0.01m,
            Minimum = -1m,
            Maximum = 2m,
            Width = 72
        };
    }

    private Control CreateCommandsPanel()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 260
        };
        split.Panel1.Controls.Add(_commandListBox);
        split.Panel2.Controls.Add(CreateSelectionEditor());
        return split;
    }

    private Control CreateSelectionEditor()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 6,
            Padding = new Padding(0, 8, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        AddCoordinateRow(panel, 0, "Start X", _startXInput, "Start Y", _startYInput);
        AddCoordinateRow(panel, 1, "End X", _endXInput, "End Y", _endYInput);
        AddCoordinateRow(panel, 2, "C1 X", _control1XInput, "C1 Y", _control1YInput);
        AddCoordinateRow(panel, 3, "C2 X", _control2XInput, "C2 Y", _control2YInput);
        AddCoordinateRow(panel, 4, "Radius", _radiusInput, "Text", _textInput);
        panel.Controls.Add(new Label { AutoSize = true, Text = "Select a command, then drag it on the canvas or edit values here.", ForeColor = SystemColors.GrayText }, 0, 5);
        panel.SetColumnSpan(panel.GetControlFromPosition(0, 5)!, 4);
        return panel;
    }

    private static void AddCoordinateRow(TableLayoutPanel panel, int row, string firstLabel, Control firstControl, string secondLabel, Control secondControl)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.Controls.Add(new Label { AutoSize = true, Text = firstLabel, Margin = new Padding(0, 6, 4, 0) }, 0, row);
        panel.Controls.Add(firstControl, 1, row);
        panel.Controls.Add(new Label { AutoSize = true, Text = secondLabel, Margin = new Padding(0, 6, 4, 0) }, 2, row);
        panel.Controls.Add(secondControl, 3, row);
    }

    private void WireSelectionInputs()
    {
        foreach (var input in new[]
        {
            _startXInput, _startYInput, _endXInput, _endYInput,
            _control1XInput, _control1YInput, _control2XInput, _control2YInput, _radiusInput
        })
        {
            input.ValueChanged += (_, _) => ApplySelectionControls();
        }

        _textInput.TextChanged += (_, _) => ApplySelectionControls();
    }

    private SymbolDesignerTool GetSelectedTool()
    {
        return Enum.TryParse(Convert.ToString(_toolComboBox.SelectedItem), out SymbolDesignerTool tool)
            ? tool
            : SymbolDesignerTool.Line;
    }

    private void SelectTool(SymbolDesignerTool tool)
    {
        var value = tool.ToString();
        if (!Equals(_toolComboBox.SelectedItem, value))
            _toolComboBox.SelectedItem = value;
        else
            _canvas.Tool = tool;
        UpdateToolStatus();
    }

    private void UpdateToolStatus()
    {
        _statusLabel.Text = GetSelectedTool() switch
        {
            SymbolDesignerTool.SelectMove => "SelectMove: drag a line to move it, or drag a handle to edit an endpoint.",
            SymbolDesignerTool.Arc => "Arc: click start, click highest point, click end.",
            SymbolDesignerTool.Circle => "Circle: drag from the center outward. Use Fill closed for a solid circle.",
            SymbolDesignerTool.Text => "Text: enter text in the toolbar, then click the canvas to place it.",
            _ => "Draw: drag on the canvas. Use Fill closed for closed shapes."
        };
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

    private void LoadReferenceFromClipboard()
    {
        if (!Clipboard.ContainsImage())
        {
            MessageBox.Show(this, "Clipboard does not contain an image.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var image = Clipboard.GetImage();
        if (image == null)
        {
            MessageBox.Show(this, "Clipboard image could not be loaded.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _canvas.LoadReferenceImage(image);
    }

    private void LoadBaseSymbol()
    {
        var selected = Convert.ToString(_unitTypeComboBox.SelectedItem);
        if (!Enum.TryParse(selected, out Components.OrbatUnitType unitType))
            return;

        var commands = BuiltInSymbolLibrary.Create(unitType);
        if (commands.Count == 0)
        {
            MessageBox.Show(this, "No editable base symbol is available for this unit type yet.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _canvas.SetCommands(commands);
    }

    private void SaveLibrary()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save symbol library",
            Filter = "ORBAT symbol library|*.orbatsymbol.json|JSON files|*.json|All files|*.*",
            FileName = $"{_unitTypeComboBox.SelectedItem}.orbatsymbol.json"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var definition = new SymbolLibraryDefinition
        {
            Name = Convert.ToString(_unitTypeComboBox.SelectedItem) ?? "Unspecified",
            UnitType = Convert.ToString(_unitTypeComboBox.SelectedItem) ?? "Unspecified",
            Version = 1,
            Commands = _canvas.Commands.Select(command => command.Clone()).ToList()
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(definition, options), Encoding.UTF8);
    }

    private void LoadLibrary()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load symbol library",
            Filter = "ORBAT symbol library|*.orbatsymbol.json;*.json|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(File.ReadAllText(dialog.FileName, Encoding.UTF8), options);
        if (definition == null)
            return;

        if (Enum.TryParse(definition.UnitType, out Components.OrbatUnitType unitType))
            _unitTypeComboBox.SelectedItem = unitType.ToString();
        _canvas.SetCommands(definition.Commands);
    }

    private void AddAirDefenseArc()
    {
        _canvas.AddCommand(SymbolDrawCommand.AirDefenseArc());
    }

    private void DeleteSelectedCommand()
    {
        _canvas.DeleteSelected();
    }

    private void CopyCode()
    {
        Clipboard.SetText(_codeTextBox.Text);
    }

    private void RefreshOutput()
    {
        var commands = _canvas.Commands.ToArray();
        _preview.SetCommands(commands);
        _commandListBox.BeginUpdate();
        try
        {
            _commandListBox.Items.Clear();
            foreach (var command in commands)
                _commandListBox.Items.Add(command.GetSummary());
            _commandListBox.SelectedIndex = _canvas.SelectedIndex >= 0 && _canvas.SelectedIndex < commands.Length
                ? _canvas.SelectedIndex
                : -1;
        }
        finally
        {
            _commandListBox.EndUpdate();
        }

        _codeTextBox.Text = GenerateCode(commands);
        RefreshSelectionControls();
    }

    private void RefreshSelectionControls()
    {
        _updatingSelectionControls = true;
        try
        {
            var command = _canvas.SelectedCommand;
            var enabled = command != null;
            foreach (Control control in new Control[]
            {
                _startXInput, _startYInput, _endXInput, _endYInput,
                _control1XInput, _control1YInput, _control2XInput, _control2YInput, _radiusInput, _textInput
            })
            {
                control.Enabled = enabled;
            }

            if (command == null)
            {
                _fillCheckBox.Enabled = false;
                _fillCheckBox.Checked = false;
                return;
            }

            _startXInput.Value = ToDecimal(command.Start.X);
            _startYInput.Value = ToDecimal(command.Start.Y);
            _endXInput.Value = ToDecimal(command.End.X);
            _endYInput.Value = ToDecimal(command.End.Y);
            _control1XInput.Value = ToDecimal(command.Control1.X);
            _control1YInput.Value = ToDecimal(command.Control1.Y);
            _control2XInput.Value = ToDecimal(command.Control2.X);
            _control2YInput.Value = ToDecimal(command.Control2.Y);
            _radiusInput.Value = ToDecimal(command.Radius);
            _textInput.Text = command.Text;
            _fillCheckBox.Enabled = command.CanFill;
            _fillCheckBox.Checked = command.CanFill && command.Filled;

            if (_commandListBox.SelectedIndex != _canvas.SelectedIndex)
                _commandListBox.SelectedIndex = _canvas.SelectedIndex;
        }
        finally
        {
            _updatingSelectionControls = false;
        }
    }

    private void ApplyFillOptionToSelection()
    {
        if (_updatingSelectionControls)
            return;

        var command = _canvas.SelectedCommand;
        if (command == null || !command.CanFill)
            return;

        command.Filled = _fillCheckBox.Checked;
        _canvas.NotifyCommandEdited();
    }

    private void ApplySelectionControls()
    {
        if (_updatingSelectionControls)
            return;

        var command = _canvas.SelectedCommand;
        if (command == null)
            return;

        command.Start = new SymbolPoint((float)_startXInput.Value, (float)_startYInput.Value);
        command.End = new SymbolPoint((float)_endXInput.Value, (float)_endYInput.Value);
        command.Control1 = new SymbolPoint((float)_control1XInput.Value, (float)_control1YInput.Value);
        command.Control2 = new SymbolPoint((float)_control2XInput.Value, (float)_control2YInput.Value);
        command.Radius = Math.Clamp((float)_radiusInput.Value, 0f, 1f);
        command.Text = _textInput.Text;
        _canvas.NotifyCommandEdited();
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

    private static decimal ToDecimal(float value) => Math.Min(2m, Math.Max(-1m, (decimal)value));
}

internal enum SymbolDesignerTool
{
    SelectMove,
    Line,
    Rectangle,
    Ellipse,
    Circle,
    Capsule,
    Dot,
    Text,
    Arc,
    BezierArc
}

internal sealed class SymbolDesignerCanvas : Control
{
    private const float SnapThreshold = 0.025f;
    private const float StandardFrameAspectRatio = 1.5f;
    private readonly List<SymbolDrawCommand> _commands = new();
    private Bitmap? _referenceImage;
    private PointF? _dragStart;
    private PointF? _dragCurrent;
    private PointF? _lastDragPoint;
    private PointF? _arcStart;
    private PointF? _arcPeak;
    private DragTarget _dragTarget = DragTarget.None;
    private SymbolDesignerTool _tool = SymbolDesignerTool.Line;

    public event EventHandler? CommandsChanged;
    public event EventHandler? SelectionChanged;

    public SymbolDesignerTool Tool
    {
        get => _tool;
        set
        {
            _tool = value;
            _arcStart = null;
            _arcPeak = null;
            _dragStart = null;
            _dragCurrent = null;
            Cursor = value == SymbolDesignerTool.SelectMove ? Cursors.Default : Cursors.Cross;
            Invalidate();
        }
    }
    public float ReferenceOpacity { get; set; } = 0.35f;
    public bool ShowGrid { get; set; } = true;
    public bool ShowIconGuide { get; set; } = true;
    public bool SnapEnabled { get; set; } = true;
    public bool FillClosedShapes { get; set; }
    public string DrawText { get; set; } = "TXT";
    public int GridDivisions { get; set; } = 10;
    public int SelectedIndex { get; private set; } = -1;
    public SymbolDrawCommand? SelectedCommand => SelectedIndex >= 0 && SelectedIndex < _commands.Count ? _commands[SelectedIndex] : null;
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

    public void LoadReferenceImage(Image image)
    {
        _referenceImage?.Dispose();
        _referenceImage = new Bitmap(image);
        Invalidate();
    }

    public void SetCommands(IEnumerable<SymbolDrawCommand> commands)
    {
        _commands.Clear();
        _commands.AddRange(commands.Select(command => command.Clone()));
        SelectedIndex = _commands.Count > 0 ? 0 : -1;
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void AddCommand(SymbolDrawCommand command)
    {
        _commands.Add(command);
        SelectedIndex = _commands.Count - 1;
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void SelectCommand(int index)
    {
        var normalized = index >= 0 && index < _commands.Count ? index : -1;
        if (SelectedIndex == normalized)
            return;

        SelectedIndex = normalized;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void NotifyCommandEdited()
    {
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void DeleteSelected()
    {
        if (SelectedIndex < 0 || SelectedIndex >= _commands.Count)
            return;

        _commands.RemoveAt(SelectedIndex);
        SelectedIndex = Math.Min(SelectedIndex, _commands.Count - 1);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void Undo()
    {
        if (_commands.Count == 0)
            return;

        _commands.RemoveAt(_commands.Count - 1);
        SelectedIndex = Math.Min(SelectedIndex, _commands.Count - 1);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void ClearCommands()
    {
        _commands.Clear();
        SelectedIndex = -1;
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;

        var symbolPoint = ToSymbolPoint(e.Location, true);
        if (Tool == SymbolDesignerTool.Arc)
        {
            HandleArcClick(symbolPoint);
            return;
        }

        if (Tool == SymbolDesignerTool.SelectMove)
        {
            BeginEditDrag(e.Location, symbolPoint);
            return;
        }

        _dragStart = symbolPoint;
        _dragCurrent = symbolPoint;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var symbolPoint = ToSymbolPoint(e.Location, _dragTarget != DragTarget.Move);

        if (_dragTarget != DragTarget.None && SelectedCommand != null && _lastDragPoint.HasValue)
        {
            EditSelectedCommand(symbolPoint);
            return;
        }

        if (Tool == SymbolDesignerTool.SelectMove)
        {
            UpdateHoverCursor(e.Location);
            return;
        }

        if (Tool == SymbolDesignerTool.Arc && (_arcStart.HasValue || _arcPeak.HasValue))
        {
            _dragCurrent = symbolPoint;
            Invalidate();
            return;
        }

        if (!_dragStart.HasValue)
            return;

        _dragCurrent = symbolPoint;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left)
            return;

        if (Tool == SymbolDesignerTool.Arc)
            return;

        if (_dragTarget != DragTarget.None)
        {
            _dragTarget = DragTarget.None;
            _lastDragPoint = null;
            CommandsChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return;
        }

        if (!_dragStart.HasValue)
            return;

        var end = ToSymbolPoint(e.Location, true);
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
        if (ShowGrid)
            DrawGrid(e.Graphics, frame);
        if (ShowIconGuide)
            DrawIconGuide(e.Graphics, frame);
        DrawFrame(e.Graphics, frame);

        for (var index = 0; index < _commands.Count; index++)
        {
            using var pen = new Pen(index == SelectedIndex ? Color.FromArgb(40, 120, 220) : Color.Black, index == SelectedIndex ? 2.4f : 2f);
            _commands[index].Draw(e.Graphics, frame, pen, Brushes.Black);
        }

        if (_dragStart.HasValue && _dragCurrent.HasValue)
        {
            using var previewPen = new Pen(Color.FromArgb(190, Color.Goldenrod), 1.5f) { DashStyle = DashStyle.Dash };
            CreateCommand(_dragStart.Value, _dragCurrent.Value)?.Draw(e.Graphics, frame, previewPen, Brushes.Goldenrod);
        }

        DrawArcPreview(e.Graphics, frame);

        DrawSelectionHandles(e.Graphics, frame);
    }

    private void HandleArcClick(PointF symbolPoint)
    {
        if (!_arcStart.HasValue)
        {
            _arcStart = symbolPoint;
            _dragCurrent = symbolPoint;
        }
        else if (!_arcPeak.HasValue)
        {
            _arcPeak = symbolPoint;
            _dragCurrent = symbolPoint;
        }
        else
        {
            AddCommand(SymbolDrawCommand.ThreePointArc(_arcStart.Value, _arcPeak.Value, symbolPoint));
            _arcStart = null;
            _arcPeak = null;
            _dragCurrent = null;
        }

        Invalidate();
    }

    private void BeginEditDrag(Point mousePoint, PointF symbolPoint)
    {
        var frame = GetFrameBounds();
        var target = HitTestHandle(mousePoint, frame);
        if (target.Target != DragTarget.None)
        {
            SelectCommand(target.Index);
            _dragTarget = target.Target;
            _lastDragPoint = symbolPoint;
            return;
        }

        var index = HitTestCommand(mousePoint, frame);
        SelectCommand(index);
        if (index >= 0)
        {
            _dragTarget = DragTarget.Move;
            _lastDragPoint = ToSymbolPoint(mousePoint, false);
            Cursor = Cursors.SizeAll;
        }
    }

    private void EditSelectedCommand(PointF symbolPoint)
    {
        var command = SelectedCommand;
        if (command == null || !_lastDragPoint.HasValue)
            return;

        if (_dragTarget == DragTarget.Move)
        {
            var delta = new SymbolPoint(symbolPoint.X - _lastDragPoint.Value.X, symbolPoint.Y - _lastDragPoint.Value.Y);
            command.Move(delta);
            _lastDragPoint = symbolPoint;
        }
        else
        {
            var point = new SymbolPoint(symbolPoint);
            command.SetPoint(_dragTarget, point);
            _lastDragPoint = symbolPoint;
        }

        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private (int Index, DragTarget Target) HitTestHandle(Point mousePoint, RectangleF frame)
    {
        const float handleRadius = 10f;
        for (var index = _commands.Count - 1; index >= 0; index--)
        {
            foreach (var handle in _commands[index].GetHandles())
            {
                var absolute = ToAbsolute(frame, handle.Point);
                if (Distance(mousePoint, absolute) <= handleRadius)
                    return (index, handle.Target);
            }
        }

        return (-1, DragTarget.None);
    }

    private int HitTestCommand(Point mousePoint, RectangleF frame)
    {
        for (var index = _commands.Count - 1; index >= 0; index--)
        {
            if (_commands[index].HitTest(mousePoint, frame, 12f))
                return index;
        }

        return -1;
    }

    private void UpdateHoverCursor(Point mousePoint)
    {
        var frame = GetFrameBounds();
        var handle = HitTestHandle(mousePoint, frame);
        if (handle.Target != DragTarget.None)
        {
            Cursor = Cursors.Hand;
            return;
        }

        Cursor = HitTestCommand(mousePoint, frame) >= 0 ? Cursors.SizeAll : Cursors.Default;
    }

    private void DrawSelectionHandles(Graphics graphics, RectangleF frame)
    {
        var command = SelectedCommand;
        if (command == null)
            return;

        foreach (var handle in command.GetHandles())
        {
            var absolute = ToAbsolute(frame, handle.Point);
            var rect = new RectangleF(absolute.X - 4f, absolute.Y - 4f, 8f, 8f);
            graphics.FillRectangle(Brushes.White, rect);
            graphics.DrawRectangle(Pens.DodgerBlue, Rectangle.Round(rect));
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

    private void DrawGrid(Graphics graphics, RectangleF frame)
    {
        using var minorPen = new Pen(Color.FromArgb(205, 218, 226), 1f);
        using var majorPen = new Pen(Color.FromArgb(145, 164, 178), 1f);
        foreach (var x in GetVerticalGridCoordinates())
        {
            var absoluteX = frame.Left + frame.Width * x;
            var pen = IsCenterGridLine(x) ? majorPen : minorPen;
            graphics.DrawLine(pen, absoluteX, frame.Top, absoluteX, frame.Bottom);
        }

        foreach (var y in GetHorizontalGridCoordinates())
        {
            var absoluteY = frame.Top + frame.Height * y;
            var pen = IsCenterGridLine(y) ? majorPen : minorPen;
            graphics.DrawLine(pen, frame.Left, absoluteY, frame.Right, absoluteY);
        }
    }

    private static void DrawIconGuide(Graphics graphics, RectangleF frame)
    {
        var guide = GetIconGuideBounds(frame);
        var points = GetIconGuidePoints();
        using var guidePen = new Pen(Color.FromArgb(150, 70, 135, 210), 1f);
        graphics.DrawPolygon(guidePen, points.Select(point => ToAbsolute(frame, point)).ToArray());
        graphics.DrawLine(guidePen, guide.Left, guide.Top + guide.Height / 2f, guide.Right, guide.Top + guide.Height / 2f);
        graphics.DrawLine(guidePen, guide.Left + guide.Width / 2f, guide.Top, guide.Left + guide.Width / 2f, guide.Bottom);
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

        if (Tool == SymbolDesignerTool.Arc && !HasRenderableArea(start, end))
            return null;

        return Tool switch
        {
            SymbolDesignerTool.Line => SymbolDrawCommand.Line(start, end),
            SymbolDesignerTool.Rectangle => SymbolDrawCommand.Rectangle(start, end, FillClosedShapes),
            SymbolDesignerTool.Ellipse => SymbolDrawCommand.Ellipse(start, end, FillClosedShapes),
            SymbolDesignerTool.Circle => SymbolDrawCommand.Circle(start, end, GetFrameBounds(), FillClosedShapes),
            SymbolDesignerTool.Capsule => SymbolDrawCommand.Capsule(start, end, FillClosedShapes),
            SymbolDesignerTool.Dot => SymbolDrawCommand.Dot(end, 0.08f),
            SymbolDesignerTool.Text => SymbolDrawCommand.TextCommand(end, string.IsNullOrWhiteSpace(DrawText) ? "TXT" : DrawText),
            SymbolDesignerTool.Arc => null,
            SymbolDesignerTool.BezierArc => SymbolDrawCommand.BezierArc(start, end),
            _ => null
        };
    }

    private void DrawArcPreview(Graphics graphics, RectangleF frame)
    {
        if (!_arcStart.HasValue || !_dragCurrent.HasValue)
            return;

        SymbolDrawCommand command;
        if (_arcPeak.HasValue)
            command = SymbolDrawCommand.ThreePointArc(_arcStart.Value, _arcPeak.Value, _dragCurrent.Value);
        else
            command = SymbolDrawCommand.Line(_arcStart.Value, _dragCurrent.Value);

        using var previewPen = new Pen(Color.FromArgb(190, Color.Goldenrod), 1.5f) { DashStyle = DashStyle.Dash };
        command.Draw(graphics, frame, previewPen, Brushes.Goldenrod);
    }

    private PointF ToSymbolPoint(Point point, bool applySnap)
    {
        var frame = GetFrameBounds();
        var x = Math.Clamp((point.X - frame.Left) / frame.Width, 0f, 1f);
        var y = Math.Clamp((point.Y - frame.Top) / frame.Height, 0f, 1f);
        var symbolPoint = new PointF(x, y);
        return applySnap && SnapEnabled ? SnapPoint(symbolPoint) : symbolPoint;
    }

    private PointF SnapPoint(PointF point)
    {
        var best = point;
        var bestDistance = SnapThreshold;
        foreach (var candidate in GetSnapCandidates())
        {
            var distance = Distance(point, candidate);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private IEnumerable<PointF> GetSnapCandidates()
    {
        yield return new PointF(0f, 0f);
        yield return new PointF(1f, 0f);
        yield return new PointF(0f, 1f);
        yield return new PointF(1f, 1f);

        foreach (var point in GetIconGuidePoints())
            yield return point;
        yield return new PointF(0.5f, 0.5f);

        foreach (var x in GetVerticalGridCoordinates())
        {
            foreach (var y in GetHorizontalGridCoordinates())
                yield return new PointF(x, y);
        }

        foreach (var command in _commands)
        {
            foreach (var point in command.GetSnapPoints())
                yield return point;
        }

        foreach (var intersection in GetLineIntersections())
            yield return intersection;
    }

    private IEnumerable<PointF> GetLineIntersections()
    {
        var segments = _commands.SelectMany(command => command.GetSegments()).ToArray();
        for (var first = 0; first < segments.Length; first++)
        {
            for (var second = first + 1; second < segments.Length; second++)
            {
                if (TryGetIntersection(segments[first].Start, segments[first].End, segments[second].Start, segments[second].End, out var intersection))
                    yield return intersection;
            }
        }
    }

    private RectangleF GetFrameBounds()
    {
        var maxWidth = ClientSize.Width - 80;
        var maxHeight = ClientSize.Height - 100;
        var width = Math.Min(maxWidth, maxHeight * StandardFrameAspectRatio);
        var height = width / StandardFrameAspectRatio;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * StandardFrameAspectRatio;
        }

        return new RectangleF(
            (ClientSize.Width - width) / 2f,
            (ClientSize.Height - height) / 2f,
            width,
            height);
    }

    private static PointF ToAbsolute(RectangleF frame, PointF point) =>
        new(frame.Left + frame.Width * point.X, frame.Top + frame.Height * point.Y);

    private static RectangleF GetIconGuideBounds(RectangleF frame)
    {
        var size = frame.Height;
        return new RectangleF(
            frame.Left + (frame.Width - size) / 2f,
            frame.Top + (frame.Height - size) / 2f,
            size,
            size);
    }

    private IEnumerable<float> GetHorizontalGridCoordinates()
    {
        var divisions = Math.Max(1, GridDivisions);
        var step = 1f / divisions;
        return GetCenteredGridCoordinates(step);
    }

    private IEnumerable<float> GetVerticalGridCoordinates()
    {
        var divisions = Math.Max(1, GridDivisions);
        var horizontalStepInFrame = 1f / divisions / StandardFrameAspectRatio;
        return GetCenteredGridCoordinates(horizontalStepInFrame);
    }

    private static IEnumerable<float> GetCenteredGridCoordinates(float step)
    {
        const float center = 0.5f;
        const float tolerance = 0.0001f;
        var values = new List<float> { center };

        for (var offset = step; offset < center - tolerance; offset += step)
        {
            var left = center - offset;
            var right = center + offset;
            if (left >= step - tolerance)
                values.Add(left);
            if (1f - right >= step - tolerance)
                values.Add(right);
        }

        values.Sort();
        return values;
    }

    private static bool IsCenterGridLine(float value) => Math.Abs(value - 0.5f) < 0.0001f;

    private static PointF[] GetIconGuidePoints()
    {
        const float left = 1f / 6f;
        const float right = 5f / 6f;
        const float center = 0.5f;
        const float diagonalXOffset = 0.2357f;
        const float diagonalYOffset = 0.3536f;

        return new[]
        {
            new PointF(center, 0f),
            new PointF(center + diagonalXOffset, center - diagonalYOffset),
            new PointF(right, center),
            new PointF(center + diagonalXOffset, center + diagonalYOffset),
            new PointF(center, 1f),
            new PointF(center - diagonalXOffset, center + diagonalYOffset),
            new PointF(left, center),
            new PointF(center - diagonalXOffset, center - diagonalYOffset)
        };
    }


    private static float Distance(Point first, PointF second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static float Distance(PointF first, PointF second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static bool HasRenderableArea(PointF start, PointF end)
    {
        const float minimumExtent = 0.001f;
        return Math.Abs(end.X - start.X) > minimumExtent && Math.Abs(end.Y - start.Y) > minimumExtent;
    }

    private static bool TryGetIntersection(PointF p, PointF p2, PointF q, PointF q2, out PointF intersection)
    {
        intersection = PointF.Empty;
        var r = new PointF(p2.X - p.X, p2.Y - p.Y);
        var s = new PointF(q2.X - q.X, q2.Y - q.Y);
        var denominator = r.X * s.Y - r.Y * s.X;
        if (Math.Abs(denominator) < 0.0001f)
            return false;

        var qmp = new PointF(q.X - p.X, q.Y - p.Y);
        var t = (qmp.X * s.Y - qmp.Y * s.X) / denominator;
        var u = (qmp.X * r.Y - qmp.Y * r.X) / denominator;
        if (t < 0f || t > 1f || u < 0f || u > 1f)
            return false;

        intersection = new PointF(p.X + t * r.X, p.Y + t * r.Y);
        return true;
    }
}

internal sealed class SymbolPreviewControl : Control
{
    private const float StandardFrameAspectRatio = 1.5f;
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
        var height = width / StandardFrameAspectRatio;
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

internal sealed class SymbolLibraryDefinition
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public List<SymbolDrawCommand> Commands { get; set; } = new();
}

internal sealed class SymbolDrawCommand
{
    public SymbolDrawCommandKind Kind { get; set; }
    public SymbolPoint Start { get; set; }
    public SymbolPoint End { get; set; }
    public SymbolPoint Control1 { get; set; }
    public SymbolPoint Control2 { get; set; }
    public float Radius { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Filled { get; set; }
    [JsonIgnore]
    public bool CanFill => Kind is SymbolDrawCommandKind.Rectangle or SymbolDrawCommandKind.Ellipse or SymbolDrawCommandKind.Circle or SymbolDrawCommandKind.Capsule;

    public static SymbolDrawCommand Line(PointF start, PointF end) =>
        new() { Kind = SymbolDrawCommandKind.Line, Start = new SymbolPoint(start), End = new SymbolPoint(end) };

    public static SymbolDrawCommand Rectangle(PointF start, PointF end, bool filled = false) =>
        new() { Kind = SymbolDrawCommandKind.Rectangle, Start = new SymbolPoint(start), End = new SymbolPoint(end), Filled = filled };

    public static SymbolDrawCommand Ellipse(PointF start, PointF end, bool filled = false) =>
        new() { Kind = SymbolDrawCommandKind.Ellipse, Start = new SymbolPoint(start), End = new SymbolPoint(end), Filled = filled };

    public static SymbolDrawCommand Circle(PointF start, PointF end, RectangleF frame, bool filled = false)
    {
        var center = ToAbsolute(frame, start);
        var edge = ToAbsolute(frame, end);
        var radius = Distance(center, edge) / Math.Min(frame.Width, frame.Height);
        return new() { Kind = SymbolDrawCommandKind.Circle, Start = new SymbolPoint(start), End = new SymbolPoint(end), Radius = radius, Filled = filled };
    }

    public static SymbolDrawCommand Capsule(PointF start, PointF end, bool filled = false) =>
        new() { Kind = SymbolDrawCommandKind.Capsule, Start = new SymbolPoint(start), End = new SymbolPoint(end), Filled = filled };

    public static SymbolDrawCommand Dot(PointF center, float radius) =>
        new() { Kind = SymbolDrawCommandKind.Dot, Start = new SymbolPoint(center), End = new SymbolPoint(center), Radius = radius };

    public static SymbolDrawCommand TextCommand(PointF location, string text) =>
        new() { Kind = SymbolDrawCommandKind.Text, Start = new SymbolPoint(location), End = new SymbolPoint(location), Text = text };

    public static SymbolDrawCommand Arc(PointF start, PointF end) =>
        new() { Kind = SymbolDrawCommandKind.Arc, Start = new SymbolPoint(start), End = new SymbolPoint(end) };

    public static SymbolDrawCommand ThreePointArc(PointF start, PointF peak, PointF end)
    {
        return new SymbolDrawCommand
        {
            Kind = SymbolDrawCommandKind.Bezier,
            Start = new SymbolPoint(start),
            End = new SymbolPoint(end),
            Control1 = new SymbolPoint(
                start.X + (peak.X - start.X) * 0.67f,
                start.Y + (peak.Y - start.Y) * 0.67f),
            Control2 = new SymbolPoint(
                end.X + (peak.X - end.X) * 0.67f,
                end.Y + (peak.Y - end.Y) * 0.67f)
        };
    }

    public static SymbolDrawCommand BezierArc(PointF start, PointF end)
    {
        var rise = Math.Abs(end.Y - start.Y) * 0.45f;
        var top = Math.Min(start.Y, end.Y) - rise;
        return new SymbolDrawCommand
        {
            Kind = SymbolDrawCommandKind.Bezier,
            Start = new SymbolPoint(start),
            End = new SymbolPoint(end),
            Control1 = new SymbolPoint(start.X + (end.X - start.X) * 0.25f, top),
            Control2 = new SymbolPoint(start.X + (end.X - start.X) * 0.75f, top)
        };
    }

    public static SymbolDrawCommand AirDefenseArc() =>
        new()
        {
            Kind = SymbolDrawCommandKind.Bezier,
            Start = new SymbolPoint(0f, 1f),
            End = new SymbolPoint(1f, 1f),
            Control1 = new SymbolPoint(0.25f, 0.77f),
            Control2 = new SymbolPoint(0.75f, 0.77f)
        };

    public SymbolDrawCommand Clone() =>
        new()
        {
            Kind = Kind,
            Start = Start,
            End = End,
            Control1 = Control1,
            Control2 = Control2,
            Radius = Radius,
            Text = Text,
            Filled = Filled
        };

    public void Move(SymbolPoint delta)
    {
        Start = Start.Offset(delta);
        End = End.Offset(delta);
        Control1 = Control1.Offset(delta);
        Control2 = Control2.Offset(delta);
    }

    public void SetPoint(DragTarget target, SymbolPoint point)
    {
        switch (target)
        {
            case DragTarget.Start:
                Start = point;
                if (Kind is SymbolDrawCommandKind.Dot or SymbolDrawCommandKind.Text)
                    End = point;
                break;
            case DragTarget.End:
                End = point;
                if (Kind == SymbolDrawCommandKind.Circle)
                    Radius = Distance(Start, End);
                break;
            case DragTarget.Control1:
                Control1 = point;
                break;
            case DragTarget.Control2:
                Control2 = point;
                break;
            case DragTarget.Peak:
                var updated = ThreePointArc(Start, point, End);
                Control1 = updated.Control1;
                Control2 = updated.Control2;
                break;
        }
    }

    public IEnumerable<SymbolHandle> GetHandles()
    {
        yield return new SymbolHandle(DragTarget.Start, Start);
        if (Kind is not SymbolDrawCommandKind.Dot and not SymbolDrawCommandKind.Text)
            yield return new SymbolHandle(DragTarget.End, End);
        if (Kind == SymbolDrawCommandKind.Bezier)
        {
            yield return new SymbolHandle(DragTarget.Peak, new SymbolPoint(EvaluateBezier(0.5f)));
            yield return new SymbolHandle(DragTarget.Control1, Control1);
            yield return new SymbolHandle(DragTarget.Control2, Control2);
        }
    }

    public IEnumerable<PointF> GetSnapPoints()
    {
        yield return Start;
        yield return End;
        if (Kind == SymbolDrawCommandKind.Bezier)
        {
            yield return Control1;
            yield return Control2;
        }
    }

    public IEnumerable<SymbolSegment> GetSegments()
    {
        if (Kind is SymbolDrawCommandKind.Line or SymbolDrawCommandKind.Rectangle or SymbolDrawCommandKind.Capsule)
        {
            if (Kind == SymbolDrawCommandKind.Line)
            {
                yield return new SymbolSegment(Start, End);
                yield break;
            }

            var rect = GetNormalizedRect();
            var topLeft = new PointF(rect.Left, rect.Top);
            var topRight = new PointF(rect.Right, rect.Top);
            var bottomRight = new PointF(rect.Right, rect.Bottom);
            var bottomLeft = new PointF(rect.Left, rect.Bottom);
            yield return new SymbolSegment(topLeft, topRight);
            yield return new SymbolSegment(topRight, bottomRight);
            yield return new SymbolSegment(bottomRight, bottomLeft);
            yield return new SymbolSegment(bottomLeft, topLeft);
        }
    }

    public bool HitTest(Point mousePoint, RectangleF frame, float threshold)
    {
        return Kind switch
        {
            SymbolDrawCommandKind.Line => DistanceToSegment(mousePoint, ToAbsolute(frame, Start), ToAbsolute(frame, End)) <= threshold,
            SymbolDrawCommandKind.Bezier => HitTestBezier(mousePoint, frame, threshold),
            SymbolDrawCommandKind.Circle => Distance(mousePoint, ToAbsolute(frame, Start)) <= Radius * Math.Min(frame.Width, frame.Height) + threshold,
            SymbolDrawCommandKind.Dot => Distance(mousePoint, ToAbsolute(frame, Start)) <= Radius * Math.Min(frame.Width, frame.Height) + threshold,
            SymbolDrawCommandKind.Text => Distance(mousePoint, ToAbsolute(frame, Start)) <= 24f,
            _ => ToRectangle(frame).Contains(mousePoint) || DistanceToRect(mousePoint, ToRectangle(frame)) <= threshold
        };
    }

    public void Draw(Graphics graphics, RectangleF frame, Pen pen, Brush brush)
    {
        switch (Kind)
        {
            case SymbolDrawCommandKind.Line:
                graphics.DrawLine(pen, ToAbsolute(frame, Start), ToAbsolute(frame, End));
                break;
            case SymbolDrawCommandKind.Rectangle:
                if (Filled)
                    graphics.FillRectangle(brush, ToRectangle(frame));
                graphics.DrawRectangle(pen, System.Drawing.Rectangle.Round(ToRectangle(frame)));
                break;
            case SymbolDrawCommandKind.Ellipse:
                if (Filled)
                    graphics.FillEllipse(brush, ToRectangle(frame));
                graphics.DrawEllipse(pen, ToRectangle(frame));
                break;
            case SymbolDrawCommandKind.Circle:
                var circle = ToCircleRectangle(frame);
                if (Filled)
                    graphics.FillEllipse(brush, circle);
                graphics.DrawEllipse(pen, circle);
                break;
            case SymbolDrawCommandKind.Capsule:
                DrawCapsule(graphics, pen, brush, ToRectangle(frame), Filled);
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
                var arcBounds = ToRectangle(frame);
                if (arcBounds.Width > 0f && arcBounds.Height > 0f)
                    graphics.DrawArc(pen, arcBounds, 200f, 140f);
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
                Filled
                    ? $"{graphics}.FillRectangle({brush}, {RectCode(bounds)});\r\n{graphics}.DrawRectangle({pen}, Rectangle.Round({RectCode(bounds)}));"
                    : $"{graphics}.DrawRectangle({pen}, Rectangle.Round({RectCode(bounds)}));",
            SymbolDrawCommandKind.Ellipse =>
                Filled
                    ? $"{graphics}.FillEllipse({brush}, {RectCode(bounds)});\r\n{graphics}.DrawEllipse({pen}, {RectCode(bounds)});"
                    : $"{graphics}.DrawEllipse({pen}, {RectCode(bounds)});",
            SymbolDrawCommandKind.Circle =>
                Filled
                    ? $"{{\r\n    var circleBounds = {CircleRectCode(bounds)};\r\n    {graphics}.FillEllipse({brush}, circleBounds);\r\n    {graphics}.DrawEllipse({pen}, circleBounds);\r\n}}"
                    : $"{graphics}.DrawEllipse({pen}, {CircleRectCode(bounds)});",
            SymbolDrawCommandKind.Capsule =>
                CapsuleCode(graphics, pen, brush, bounds),
            SymbolDrawCommandKind.Dot =>
                $"{graphics}.FillEllipse({brush}, {PointCode(bounds, Start)}.X - {RadiusCode(bounds)}, {PointCode(bounds, Start)}.Y - {RadiusCode(bounds)}, {RadiusCode(bounds)} * 2f, {RadiusCode(bounds)} * 2f);",
            SymbolDrawCommandKind.Text =>
                $"{{\r\n    var textLocation = {PointCode(bounds, Start)};\r\n    {graphics}.DrawString(\"{EscapeCSharpString(Text)}\", font, {brush}, new RectangleF(textLocation.X - 34f, textLocation.Y - 14f, 68f, 28f), centerFormat);\r\n}}",
            SymbolDrawCommandKind.Arc =>
                $"{graphics}.DrawArc({pen}, {RectCode(bounds)}, 200f, 140f);",
            SymbolDrawCommandKind.Bezier =>
                $"{graphics}.DrawBezier({pen}, {PointCode(bounds, Start)}, {PointCode(bounds, Control1)}, {PointCode(bounds, Control2)}, {PointCode(bounds, End)});",
            _ => string.Empty
        };
    }

    private static void DrawCapsule(Graphics graphics, Pen pen, Brush brush, RectangleF rect, bool filled)
    {
        var height = Math.Min(rect.Height, rect.Width);
        var capsule = new RectangleF(
            rect.Left,
            rect.Top + (rect.Height - height) / 2f,
            rect.Width,
            height);
        var radius = capsule.Height / 2f;
        using var path = new GraphicsPath();
        path.AddArc(capsule.Left, capsule.Top, radius * 2f, radius * 2f, 90, 180);
        path.AddLine(capsule.Left + radius, capsule.Top, capsule.Right - radius, capsule.Top);
        path.AddArc(capsule.Right - radius * 2f, capsule.Top, radius * 2f, radius * 2f, 270, 180);
        path.AddLine(capsule.Right - radius, capsule.Bottom, capsule.Left + radius, capsule.Bottom);
        path.CloseFigure();
        if (filled)
            graphics.FillPath(brush, path);
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

    private RectangleF ToCircleRectangle(RectangleF frame)
    {
        var center = ToAbsolute(frame, Start);
        var radius = Radius * Math.Min(frame.Width, frame.Height);
        return new RectangleF(center.X - radius, center.Y - radius, radius * 2f, radius * 2f);
    }

    private RectangleF GetNormalizedRect()
    {
        var left = Math.Min(Start.X, End.X);
        var top = Math.Min(Start.Y, End.Y);
        return new RectangleF(left, top, Math.Abs(End.X - Start.X), Math.Abs(End.Y - Start.Y));
    }

    private static PointF ToAbsolute(RectangleF frame, PointF point) =>
        new(frame.Left + frame.Width * point.X, frame.Top + frame.Height * point.Y);

    private static string PointCode(string bounds, SymbolPoint point) =>
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

    private string CircleRectCode(string bounds)
    {
        var radius = RadiusCode(bounds);
        return $"new RectangleF({PointCode(bounds, Start)}.X - {radius}, {PointCode(bounds, Start)}.Y - {radius}, {radius} * 2f, {radius} * 2f)";
    }

    private string CapsuleCode(string graphics, string pen, string brush, string bounds)
    {
        var fillLine = Filled ? $"    {graphics}.FillPath({brush}, capsulePath);\r\n" : string.Empty;
        return "{\r\n"
            + $"    var capsuleBounds = {RectCode(bounds)};\r\n"
            + "    var capsuleHeight = Math.Min(capsuleBounds.Height, capsuleBounds.Width);\r\n"
            + "    capsuleBounds = new RectangleF(capsuleBounds.Left, capsuleBounds.Top + (capsuleBounds.Height - capsuleHeight) / 2f, capsuleBounds.Width, capsuleHeight);\r\n"
            + "    var capsuleRadius = capsuleBounds.Height / 2f;\r\n"
            + "    using var capsulePath = new GraphicsPath();\r\n"
            + "    capsulePath.AddArc(capsuleBounds.Left, capsuleBounds.Top, capsuleRadius * 2f, capsuleRadius * 2f, 90, 180);\r\n"
            + "    capsulePath.AddLine(capsuleBounds.Left + capsuleRadius, capsuleBounds.Top, capsuleBounds.Right - capsuleRadius, capsuleBounds.Top);\r\n"
            + "    capsulePath.AddArc(capsuleBounds.Right - capsuleRadius * 2f, capsuleBounds.Top, capsuleRadius * 2f, capsuleRadius * 2f, 270, 180);\r\n"
            + "    capsulePath.AddLine(capsuleBounds.Right - capsuleRadius, capsuleBounds.Bottom, capsuleBounds.Left + capsuleRadius, capsuleBounds.Bottom);\r\n"
            + "    capsulePath.CloseFigure();\r\n"
            + fillLine
            + $"    {graphics}.DrawPath({pen}, capsulePath);\r\n"
            + "}";
    }

    private static string FormatPoint(SymbolPoint point) => $"({Format(point.X)}, {Format(point.Y)})";

    private static string Format(float value) => value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    private static string EscapeCSharpString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static float Distance(Point first, PointF second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static float Distance(PointF first, PointF second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static float DistanceToSegment(Point point, PointF start, PointF end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        if (Math.Abs(dx) < 0.001f && Math.Abs(dy) < 0.001f)
            return Distance(point, start);

        var t = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0f, 1f);
        return Distance(point, new PointF(start.X + t * dx, start.Y + t * dy));
    }

    private static float DistanceToRect(Point point, RectangleF rect)
    {
        var dx = Math.Max(Math.Max(rect.Left - point.X, 0), point.X - rect.Right);
        var dy = Math.Max(Math.Max(rect.Top - point.Y, 0), point.Y - rect.Bottom);
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private bool HitTestBezier(Point mousePoint, RectangleF frame, float threshold)
    {
        var previous = ToAbsolute(frame, Start);
        for (var step = 1; step <= 24; step++)
        {
            var t = step / 24f;
            var current = ToAbsolute(frame, EvaluateBezier(t));
            if (DistanceToSegment(mousePoint, previous, current) <= threshold)
                return true;
            previous = current;
        }

        return false;
    }

    private PointF EvaluateBezier(float t)
    {
        var u = 1f - t;
        var x = u * u * u * Start.X
            + 3f * u * u * t * Control1.X
            + 3f * u * t * t * Control2.X
            + t * t * t * End.X;
        var y = u * u * u * Start.Y
            + 3f * u * u * t * Control1.Y
            + 3f * u * t * t * Control2.Y
            + t * t * t * End.Y;
        return new PointF(x, y);
    }
}

internal readonly record struct SymbolHandle(DragTarget Target, SymbolPoint Point);

internal readonly record struct SymbolSegment(PointF Start, PointF End);

internal readonly record struct SymbolPoint(float X, float Y)
{
    public SymbolPoint(PointF point)
        : this(point.X, point.Y)
    {
    }

    public SymbolPoint Offset(SymbolPoint delta) => new(X + delta.X, Y + delta.Y);

    public static implicit operator PointF(SymbolPoint point) => new(point.X, point.Y);
}

internal enum DragTarget
{
    None,
    Move,
    Start,
    End,
    Control1,
    Control2,
    Peak
}

internal enum SymbolDrawCommandKind
{
    Line,
    Rectangle,
    Ellipse,
    Circle,
    Capsule,
    Dot,
    Text,
    Arc,
    Bezier
}

internal static class BuiltInSymbolLibrary
{
    public static IReadOnlyList<SymbolDrawCommand> Create(Components.OrbatUnitType unitType)
    {
        return unitType switch
        {
            Components.OrbatUnitType.Infantry => new[]
            {
                SymbolDrawCommand.Line(new PointF(0f, 0f), new PointF(1f, 1f)),
                SymbolDrawCommand.Line(new PointF(1f, 0f), new PointF(0f, 1f))
            },
            Components.OrbatUnitType.MechanizedInfantry => new[]
            {
                SymbolDrawCommand.Capsule(new PointF(0.22f, 0.32f), new PointF(0.78f, 0.68f)),
                SymbolDrawCommand.Line(new PointF(0f, 0f), new PointF(1f, 1f)),
                SymbolDrawCommand.Line(new PointF(1f, 0f), new PointF(0f, 1f))
            },
            Components.OrbatUnitType.Armor => new[]
            {
                SymbolDrawCommand.Capsule(new PointF(0.16f, 0.3f), new PointF(0.84f, 0.7f))
            },
            Components.OrbatUnitType.Artillery => new[]
            {
                SymbolDrawCommand.Dot(new PointF(0.5f, 0.5f), 0.12f)
            },
            Components.OrbatUnitType.AirDefense => new[]
            {
                SymbolDrawCommand.AirDefenseArc()
            },
            Components.OrbatUnitType.Engineer => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.28f, 0.36f), new PointF(0.28f, 0.64f)),
                SymbolDrawCommand.Line(new PointF(0.28f, 0.36f), new PointF(0.72f, 0.36f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.36f), new PointF(0.5f, 0.64f)),
                SymbolDrawCommand.Line(new PointF(0.72f, 0.36f), new PointF(0.72f, 0.64f))
            },
            Components.OrbatUnitType.Signal => new[]
            {
                SymbolDrawCommand.Line(new PointF(0f, 0f), new PointF(0.5f, 0.66f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.66f), new PointF(0.5f, 0.32f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.32f), new PointF(1f, 1f))
            },
            Components.OrbatUnitType.Medical => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.5f, 0f), new PointF(0.5f, 1f)),
                SymbolDrawCommand.Line(new PointF(0f, 0.5f), new PointF(1f, 0.5f))
            },
            _ => Array.Empty<SymbolDrawCommand>()
        };
    }
}
