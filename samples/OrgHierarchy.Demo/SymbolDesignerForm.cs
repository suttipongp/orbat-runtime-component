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
    private readonly ComboBox _frameShapeComboBox = new();
    private readonly ComboBox _frameStatusComboBox = new();
    private readonly TrackBar _referenceOpacityTrackBar = new();
    private readonly CheckBox _showGridCheckBox = new() { Text = "Grid", Checked = true, AutoSize = true };
    private readonly CheckBox _showIconGuideCheckBox = new() { Text = "Icon guide", Checked = true, AutoSize = true };
    private readonly ComboBox _iconGuideShapeComboBox = new();
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
    private readonly NumericUpDown _fontSizeInput = CreateFontSizeInput();
    private readonly TextBox _textInput = new();
    private readonly TextBox _drawTextInput = new() { Text = "TXT", Width = 80 };
    private readonly NumericUpDown _drawTextSizeInput = CreateFontSizeInput();
    private bool _updatingSelectionControls;

    public SymbolDesignerForm()
    {
        Text = "ORBAT Symbol Designer";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1120, 900);
        Size = new Size(1240, 980);
        KeyPreview = true;
        KeyDown += HandleShortcutKeyDown;

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

        _frameShapeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _frameShapeComboBox.Width = 152;
        _frameShapeComboBox.Items.AddRange(Enum.GetNames<SymbolFrameShape>().Cast<object>().ToArray());
        _frameShapeComboBox.SelectedItem = SymbolFrameShape.FriendlyUnit.ToString();
        _frameShapeComboBox.SelectedIndexChanged += (_, _) =>
        {
            _canvas.FrameShape = GetSelectedFrameShape();
            RefreshOutput();
            _canvas.Invalidate();
        };

        _frameStatusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _frameStatusComboBox.Width = 92;
        _frameStatusComboBox.Items.AddRange(Enum.GetNames<SymbolFrameStatus>().Cast<object>().ToArray());
        _frameStatusComboBox.SelectedItem = SymbolFrameStatus.Present.ToString();
        _frameStatusComboBox.SelectedIndexChanged += (_, _) =>
        {
            _canvas.FrameStatus = GetSelectedFrameStatus();
            RefreshOutput();
            _canvas.Invalidate();
        };

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
        _gridDivisionsInput.Value = 12;
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
        _iconGuideShapeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _iconGuideShapeComboBox.Width = 128;
        _iconGuideShapeComboBox.Items.AddRange(Enum.GetNames<IconGuideShape>().Cast<object>().ToArray());
        _iconGuideShapeComboBox.SelectedItem = IconGuideShape.FlatTopBottom.ToString();
        _iconGuideShapeComboBox.SelectedIndexChanged += (_, _) =>
        {
            _canvas.IconGuideShape = GetSelectedIconGuideShape();
            _canvas.Invalidate();
        };
        _snapCheckBox.CheckedChanged += (_, _) => _canvas.SnapEnabled = _snapCheckBox.Checked;
        _fillCheckBox.CheckedChanged += (_, _) =>
        {
            _canvas.FillClosedShapes = _fillCheckBox.Checked;
            ApplyFillOptionToSelection();
        };
        _drawTextInput.TextChanged += (_, _) => _canvas.DrawText = _drawTextInput.Text;
        _drawTextSizeInput.ValueChanged += (_, _) => _canvas.DrawFontSize = (float)_drawTextSizeInput.Value;

        var loadButton = CreateButton("Load reference", LoadReferenceImage);
        var loadClipboardButton = CreateButton("Load clipboard", LoadReferenceFromClipboard);
        var loadBaseButton = CreateButton("Load base", LoadBaseSymbol);
        var saveLibraryButton = CreateButton("Save library", SaveLibrary);
        var loadLibraryButton = CreateButton("Load library", LoadLibrary);
        var viewLibraryButton = CreateButton("View library", ViewLibrary);
        var undoButton = CreateButton("Undo", () => _canvas.Undo());
        var duplicateButton = CreateButton("Duplicate", () => _canvas.DuplicateSelected());
        var copyButton = CreateButton("Copy", () => _canvas.CopySelected());
        var pasteButton = CreateButton("Paste", () => _canvas.PasteCopied());
        var rotateButton = CreateButton("Rotate 90", () => _canvas.RotateSelectedClockwise());
        var mirrorHorizontalButton = CreateButton("Mirror H", () => _canvas.MirrorSelectedHorizontal());
        var mirrorVerticalButton = CreateButton("Mirror V", () => _canvas.MirrorSelectedVertical());
        var deleteButton = CreateButton("Delete", DeleteSelectedCommand);
        var clearButton = CreateButton("Clear", () => _canvas.ClearCanvas());
        var closePathButton = CreateButton("Close path", CloseLinePath);
        var airDefenseButton = CreateButton("Air defense arc", AddAirDefenseArc);
        var copyCodeButton = CreateButton("Copy C# code", CopyCode);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoScroll = true,
            Height = 148,
            Padding = new Padding(8, 8, 8, 4),
            WrapContents = true
        };
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Unit type", Margin = new Padding(0, 6, 4, 0) });
        toolbar.Controls.Add(_unitTypeComboBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Frame", Margin = new Padding(14, 6, 4, 0) });
        toolbar.Controls.Add(_frameShapeComboBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Status", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_frameStatusComboBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Tool", Margin = new Padding(14, 6, 4, 0) });
        toolbar.Controls.Add(_toolComboBox);
        toolbar.Controls.Add(loadButton);
        toolbar.Controls.Add(loadClipboardButton);
        toolbar.Controls.Add(loadBaseButton);
        toolbar.Controls.Add(saveLibraryButton);
        toolbar.Controls.Add(loadLibraryButton);
        toolbar.Controls.Add(viewLibraryButton);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Reference", Margin = new Padding(14, 6, 4, 0) });
        toolbar.Controls.Add(_referenceOpacityTrackBar);
        toolbar.Controls.Add(_showGridCheckBox);
        toolbar.Controls.Add(_showIconGuideCheckBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Guide shape", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_iconGuideShapeComboBox);
        toolbar.Controls.Add(_snapCheckBox);
        toolbar.Controls.Add(_fillCheckBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Grid", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_gridDivisionsInput);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Text", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_drawTextInput);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Size %", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_drawTextSizeInput);
        toolbar.Controls.Add(undoButton);
        toolbar.Controls.Add(duplicateButton);
        toolbar.Controls.Add(copyButton);
        toolbar.Controls.Add(pasteButton);
        toolbar.Controls.Add(rotateButton);
        toolbar.Controls.Add(mirrorHorizontalButton);
        toolbar.Controls.Add(mirrorVerticalButton);
        toolbar.Controls.Add(deleteButton);
        toolbar.Controls.Add(clearButton);
        toolbar.Controls.Add(closePathButton);
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
        _canvas.FrameShape = GetSelectedFrameShape();
        _canvas.FrameStatus = GetSelectedFrameStatus();
        _canvas.ReferenceOpacity = _referenceOpacityTrackBar.Value / 100f;
        _canvas.GridDivisions = (int)_gridDivisionsInput.Value;
        _canvas.ShowGrid = _showGridCheckBox.Checked;
        _canvas.ShowIconGuide = _showIconGuideCheckBox.Checked;
        _canvas.IconGuideShape = GetSelectedIconGuideShape();
        _canvas.SnapEnabled = _snapCheckBox.Checked;
        _canvas.FillClosedShapes = _fillCheckBox.Checked;
        _canvas.DrawText = _drawTextInput.Text;
        _canvas.DrawFontSize = (float)_drawTextSizeInput.Value;
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

    public SymbolDesignerForm(string libraryFileName)
        : this()
    {
        LoadLibraryFile(libraryFileName);
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

    private static NumericUpDown CreateFontSizeInput()
    {
        return new NumericUpDown
        {
            DecimalPlaces = 1,
            Increment = 1m,
            Minimum = 4m,
            Maximum = 72m,
            Value = 12m,
            Width = 60
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
            RowCount = 7,
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
        AddCoordinateRow(panel, 4, "Radius", _radiusInput, "Text %", _fontSizeInput);
        AddTextRow(panel, 5, "Text", _textInput);
        panel.Controls.Add(new Label { AutoSize = true, Text = "Select a command, then drag it on the canvas or edit values here.", ForeColor = SystemColors.GrayText }, 0, 6);
        panel.SetColumnSpan(panel.GetControlFromPosition(0, 6)!, 4);
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

    private static void AddTextRow(TableLayoutPanel panel, int row, string label, Control control)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.Controls.Add(new Label { AutoSize = true, Text = label, Margin = new Padding(0, 6, 4, 0) }, 0, row);
        panel.Controls.Add(control, 1, row);
        panel.SetColumnSpan(control, 3);
    }

    private void WireSelectionInputs()
    {
        foreach (var input in new[]
        {
            _startXInput, _startYInput, _endXInput, _endYInput,
            _control1XInput, _control1YInput, _control2XInput, _control2YInput, _radiusInput, _fontSizeInput
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

    private SymbolFrameShape GetSelectedFrameShape()
    {
        return Enum.TryParse(Convert.ToString(_frameShapeComboBox.SelectedItem), out SymbolFrameShape shape)
            ? shape
            : SymbolFrameShape.FriendlyUnit;
    }

    private SymbolFrameStatus GetSelectedFrameStatus()
    {
        return Enum.TryParse(Convert.ToString(_frameStatusComboBox.SelectedItem), out SymbolFrameStatus status)
            ? status
            : SymbolFrameStatus.Present;
    }

    private IconGuideShape GetSelectedIconGuideShape()
    {
        return Enum.TryParse(Convert.ToString(_iconGuideShapeComboBox.SelectedItem), out IconGuideShape shape)
            ? shape
            : IconGuideShape.FlatTopBottom;
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

        var libraryName = GetLibraryNameFromFileName(dialog.FileName);
        var selectedUnitType = Convert.ToString(_unitTypeComboBox.SelectedItem) ?? Components.OrbatUnitType.Unspecified.ToString();
        var libraryUnitType = InferUnitTypeFromLibraryName(libraryName, selectedUnitType);
        var definition = new SymbolLibraryDefinition
        {
            Name = libraryName,
            UnitType = libraryUnitType,
            FrameShape = GetSelectedFrameShape(),
            FrameStatus = GetSelectedFrameStatus(),
            Version = 1,
            Commands = _canvas.Commands.Select(command => command.Clone()).ToList()
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(definition, options), Encoding.UTF8);
    }

    private static string GetLibraryNameFromFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (name.EndsWith(".orbatsymbol.json", StringComparison.OrdinalIgnoreCase))
            name = name[..^".orbatsymbol.json".Length];
        else
            name = Path.GetFileNameWithoutExtension(name);

        return string.IsNullOrWhiteSpace(name)
            ? Components.OrbatUnitType.Unspecified.ToString()
            : name;
    }

    private static string InferUnitTypeFromLibraryName(string libraryName, string fallbackUnitType)
    {
        if (Enum.TryParse(libraryName, ignoreCase: true, out Components.OrbatUnitType exactUnitType))
            return exactUnitType.ToString();

        var normalized = new string(libraryName.Where(char.IsLetterOrDigit).ToArray());
        if (Enum.TryParse(normalized, ignoreCase: true, out Components.OrbatUnitType normalizedUnitType))
            return normalizedUnitType.ToString();

        return normalized.Equals("transport", StringComparison.OrdinalIgnoreCase)
            ? Components.OrbatUnitType.Transportation.ToString()
            : fallbackUnitType;
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

        LoadLibraryFile(dialog.FileName);
    }

    internal void LoadLibraryFile(string fileName)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(File.ReadAllText(fileName, Encoding.UTF8), options);
        if (definition == null)
            return;

        if (Enum.TryParse(definition.UnitType, out Components.OrbatUnitType unitType))
            _unitTypeComboBox.SelectedItem = unitType.ToString();
        _frameShapeComboBox.SelectedItem = definition.FrameShape.ToString();
        _frameStatusComboBox.SelectedItem = definition.FrameStatus.ToString();
        _canvas.FrameShape = definition.FrameShape;
        _canvas.FrameStatus = definition.FrameStatus;
        _canvas.SetCommands(definition.Commands);
        Text = $"ORBAT Symbol Designer - {GetLibraryNameFromFileName(fileName)}";
    }

    private void ViewLibrary()
    {
        using var form = new SymbolLibraryViewerForm();
        form.ShowDialog(this);
    }

    private void AddAirDefenseArc()
    {
        _canvas.AddCommand(SymbolDrawCommand.AirDefenseArc());
    }

    private void CloseLinePath()
    {
        if (!_canvas.TryCloseLinePath(_fillCheckBox.Checked))
            MessageBox.Show(this, "Draw at least two connected line or curve segments before closing a path.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        _preview.SetFrame(GetSelectedFrameShape(), GetSelectedFrameStatus());
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
                _control1XInput, _control1YInput, _control2XInput, _control2YInput, _radiusInput, _fontSizeInput, _textInput
            })
            {
                control.Enabled = enabled;
            }

            if (command == null)
            {
                _fillCheckBox.Enabled = true;
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
            _fontSizeInput.Value = ToFontSizeDecimal(command.FontSize);
            _textInput.Text = command.Text;
            _fillCheckBox.Enabled = true;
            if (command.CanFill)
                _fillCheckBox.Checked = command.Filled;

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
        command.FontSize = Math.Clamp((float)_fontSizeInput.Value, 4f, 72f);
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

    private static decimal ToFontSizeDecimal(float value) => Math.Min(72m, Math.Max(4m, (decimal)value));

    private void HandleShortcutKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsEditingInput())
            return;

        if (e.Control && e.KeyCode == Keys.C)
        {
            _canvas.CopySelected();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.V)
        {
            _canvas.PasteCopied();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.D)
        {
            _canvas.DuplicateSelected();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.R)
        {
            _canvas.RotateSelectedClockwise();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            _canvas.DeleteSelected();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private bool IsEditingInput()
    {
        if (ActiveControl is TextBoxBase or NumericUpDown)
            return true;

        return _textInput.ContainsFocus
            || _drawTextInput.ContainsFocus
            || _startXInput.ContainsFocus
            || _startYInput.ContainsFocus
            || _endXInput.ContainsFocus
            || _endYInput.ContainsFocus
            || _control1XInput.ContainsFocus
            || _control1YInput.ContainsFocus
            || _control2XInput.ContainsFocus
            || _control2YInput.ContainsFocus
            || _radiusInput.ContainsFocus
            || _fontSizeInput.ContainsFocus
            || _drawTextSizeInput.ContainsFocus;
    }
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

internal enum SymbolFrameShape
{
    FriendlyUnit,
    FriendlyEquipment,
    Hostile,
    Neutral,
    Unknown
}

internal enum SymbolFrameStatus
{
    Present,
    PlannedAnticipated
}

internal enum IconGuideShape
{
    FlatTopBottom,
    PointedTopBottom
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
    private SymbolDrawCommand? _copiedCommand;

    public event EventHandler? CommandsChanged;
    public event EventHandler? SelectionChanged;

    public SymbolFrameShape FrameShape { get; set; } = SymbolFrameShape.FriendlyUnit;
    public SymbolFrameStatus FrameStatus { get; set; } = SymbolFrameStatus.Present;

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
    public IconGuideShape IconGuideShape { get; set; } = IconGuideShape.FlatTopBottom;
    public bool SnapEnabled { get; set; } = true;
    public bool FillClosedShapes { get; set; }
    public string DrawText { get; set; } = "TXT";
    public float DrawFontSize { get; set; } = 12f;
    public int GridDivisions { get; set; } = 12;
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

    public void CopySelected()
    {
        var command = SelectedCommand;
        if (command == null)
            return;

        _copiedCommand = command.Clone();
    }

    public void PasteCopied()
    {
        if (_copiedCommand == null)
            return;

        var command = _copiedCommand.Clone();
        command.Move(new SymbolPoint(0.04f, 0.04f));
        AddCommand(command);
        _copiedCommand = command.Clone();
    }

    public void DuplicateSelected()
    {
        var command = SelectedCommand;
        if (command == null)
            return;

        var duplicate = command.Clone();
        duplicate.Move(new SymbolPoint(0.04f, 0.04f));
        AddCommand(duplicate);
        _copiedCommand = duplicate.Clone();
    }

    public void RotateSelectedClockwise()
    {
        TransformSelected(command => command.RotateClockwise(new SymbolPoint(0.5f, 0.5f)));
    }

    public void MirrorSelectedHorizontal()
    {
        TransformSelected(command => command.MirrorHorizontal(0.5f));
    }

    public void MirrorSelectedVertical()
    {
        TransformSelected(command => command.MirrorVertical(0.5f));
    }

    public bool TryCloseLinePath(bool filled)
    {
        var pathCommands = _commands
            .Where(command => command.Kind is SymbolDrawCommandKind.Line or SymbolDrawCommandKind.Bezier)
            .Select(command => command.Clone())
            .ToList();
        if (pathCommands.Count < 2)
            return false;

        var points = BuildConnectedPath(pathCommands);
        if (points.Count < 3)
            return false;

        _commands.RemoveAll(command => command.Kind is SymbolDrawCommandKind.Line or SymbolDrawCommandKind.Bezier);
        _commands.Add(SymbolDrawCommand.Path(points, filled));
        SelectedIndex = _commands.Count - 1;
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
        return true;
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

    private void TransformSelected(Action<SymbolDrawCommand> transform)
    {
        var command = SelectedCommand;
        if (command == null)
            return;

        transform(command);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
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

    public void ClearCanvas()
    {
        _referenceImage?.Dispose();
        _referenceImage = null;
        ClearCommands();
    }

    private static List<SymbolPoint> BuildConnectedPath(List<SymbolDrawCommand> segments)
    {
        const float joinTolerance = 0.025f;
        var first = segments[0];
        segments.RemoveAt(0);
        var points = new List<SymbolPoint>();
        AddPathSegmentPoints(points, first, reverse: false, includeStart: true);

        while (segments.Count > 0)
        {
            var last = points[^1];
            var matchIndex = segments.FindIndex(segment => Distance(last, segment.Start) <= joinTolerance || Distance(last, segment.End) <= joinTolerance);
            if (matchIndex < 0)
                break;

            var match = segments[matchIndex];
            segments.RemoveAt(matchIndex);
            var reverse = Distance(last, match.End) < Distance(last, match.Start);
            AddPathSegmentPoints(points, match, reverse, includeStart: false);
        }

        if (Distance(points[0], points[^1]) <= joinTolerance)
            points[^1] = points[0];

        return points;
    }

    private static void AddPathSegmentPoints(List<SymbolPoint> points, SymbolDrawCommand command, bool reverse, bool includeStart)
    {
        const int curveSteps = 32;
        if (command.Kind == SymbolDrawCommandKind.Line)
        {
            if (includeStart)
                points.Add(reverse ? command.End : command.Start);
            points.Add(reverse ? command.Start : command.End);
            return;
        }

        for (var index = includeStart ? 0 : 1; index <= curveSteps; index++)
        {
            var t = index / (float)curveSteps;
            if (reverse)
                t = 1f - t;
            points.Add(new SymbolPoint(EvaluateBezier(command, t)));
        }
    }

    private static PointF EvaluateBezier(SymbolDrawCommand command, float t)
    {
        var u = 1f - t;
        var x = u * u * u * command.Start.X
            + 3f * u * u * t * command.Control1.X
            + 3f * u * t * t * command.Control2.X
            + t * t * t * command.End.X;
        var y = u * u * u * command.Start.Y
            + 3f * u * u * t * command.Control1.Y
            + 3f * u * t * t * command.Control2.Y
            + t * t * t * command.End.Y;
        return new PointF(x, y);
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

        var workspace = GetWorkspaceBounds();
        var frame = GetFrameBounds();
        e.Graphics.FillRectangle(Brushes.White, workspace);
        DrawReference(e.Graphics, frame);
        if (ShowGrid)
            DrawGrid(e.Graphics, frame, workspace);
        if (ShowIconGuide)
            DrawIconGuide(e.Graphics, frame);
        SymbolFrameRenderer.DrawFrame(e.Graphics, frame, FrameShape, FrameStatus, fillFrame: false, IconGuideShape);

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

    private void DrawGrid(Graphics graphics, RectangleF frame, RectangleF workspace)
    {
        using var minorPen = new Pen(Color.FromArgb(210, 220, 226), 1f);
        using var majorPen = new Pen(Color.FromArgb(145, 164, 178), 1f);

        var step = frame.Height / Math.Max(1, GridDivisions);
        var centerX = frame.Left + frame.Width / 2f;
        var centerY = frame.Top + frame.Height / 2f;

        for (var x = centerX; x <= workspace.Right + 0.5f; x += step)
            graphics.DrawLine(IsCenterGridLine(x, centerX) ? majorPen : minorPen, x, workspace.Top, x, workspace.Bottom);
        for (var x = centerX - step; x >= workspace.Left - 0.5f; x -= step)
            graphics.DrawLine(minorPen, x, workspace.Top, x, workspace.Bottom);
        for (var y = centerY; y <= workspace.Bottom + 0.5f; y += step)
            graphics.DrawLine(IsCenterGridLine(y, centerY) ? majorPen : minorPen, workspace.Left, y, workspace.Right, y);
        for (var y = centerY - step; y >= workspace.Top - 0.5f; y -= step)
            graphics.DrawLine(minorPen, workspace.Left, y, workspace.Right, y);
    }

    private void DrawIconGuide(Graphics graphics, RectangleF frame)
    {
        var guide = GetIconGuideBounds(frame);
        var points = GetIconGuidePoints(IconGuideShape);
        using var guidePen = new Pen(Color.FromArgb(220, 24, 82, 180), 1.4f);
        var absolutePoints = points.Select(point => ToAbsolute(frame, point)).ToArray();
        DrawIconGuideGrid(graphics, frame, absolutePoints);
        graphics.DrawPolygon(guidePen, absolutePoints);
        graphics.DrawLine(guidePen, guide.Left, guide.Top + guide.Height / 2f, guide.Right, guide.Top + guide.Height / 2f);
        graphics.DrawLine(guidePen, guide.Left + guide.Width / 2f, guide.Top, guide.Left + guide.Width / 2f, guide.Bottom);
    }

    private void DrawIconGuideGrid(Graphics graphics, RectangleF frame, PointF[] absolutePoints)
    {
        using var path = new GraphicsPath();
        path.AddPolygon(absolutePoints);
        var state = graphics.Save();
        graphics.SetClip(path, CombineMode.Intersect);
        using var guideGridPen = new Pen(Color.FromArgb(150, 64, 128, 220), 1f) { DashStyle = DashStyle.Dot };

        foreach (var x in GetVerticalGridCoordinates())
        {
            var absoluteX = frame.Left + frame.Width * x;
            graphics.DrawLine(guideGridPen, absoluteX, frame.Top, absoluteX, frame.Bottom);
        }

        foreach (var y in GetHorizontalGridCoordinates())
        {
            var absoluteY = frame.Top + frame.Height * y;
            graphics.DrawLine(guideGridPen, frame.Left, absoluteY, frame.Right, absoluteY);
        }

        graphics.Restore(state);
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
            SymbolDesignerTool.Text => SymbolDrawCommand.TextCommand(end, string.IsNullOrWhiteSpace(DrawText) ? "TXT" : DrawText, DrawFontSize),
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
        var workspace = GetWorkspaceBounds();
        var x = Math.Clamp((point.X - frame.Left) / frame.Width, 0f, 1f);
        var minY = (workspace.Top - frame.Top) / frame.Height;
        var maxY = (workspace.Bottom - frame.Top) / frame.Height;
        var y = Math.Clamp((point.Y - frame.Top) / frame.Height, minY, maxY);
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

        foreach (var candidate in GetLineSnapCandidates(point))
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

        foreach (var point in GetIconGuidePoints(IconGuideShape))
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

    private IEnumerable<PointF> GetLineSnapCandidates(PointF point)
    {
        foreach (var command in _commands)
        {
            foreach (var segment in command.GetSegments())
                yield return ClosestPointOnSegment(point, segment.Start, segment.End);

            if (command.Kind == SymbolDrawCommandKind.Bezier)
            {
                var previous = (PointF)command.Start;
                for (var step = 1; step <= 32; step++)
                {
                    var current = EvaluateBezier(command, step / 32f);
                    yield return ClosestPointOnSegment(point, previous, current);
                    previous = current;
                }
            }
        }
    }

    private static PointF ClosestPointOnSegment(PointF point, PointF start, PointF end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        if (Math.Abs(dx) < 0.0001f && Math.Abs(dy) < 0.0001f)
            return start;

        var t = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0f, 1f);
        return new PointF(start.X + t * dx, start.Y + t * dy);
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
        var width = Math.Min(maxWidth, maxHeight);
        var height = width / StandardFrameAspectRatio;

        return new RectangleF(
            (ClientSize.Width - width) / 2f,
            (ClientSize.Height - height) / 2f,
            width,
            height);
    }

    private RectangleF GetWorkspaceBounds()
    {
        var frame = GetFrameBounds();
        var side = frame.Width;
        return new RectangleF(
            frame.Left,
            frame.Top + (frame.Height - side) / 2f,
            side,
            side);
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

    private static bool IsCenterGridLine(float value, float center) => Math.Abs(value - center) < 0.0001f;

    private static PointF[] GetIconGuidePoints(IconGuideShape shape)
    {
        const float left = 1f / 6f;
        const float right = 5f / 6f;
        const float center = 0.5f;
        const float flatXOffset = 0.1381f;
        const float flatYShoulder = 0.2929f;
        const float diagonalXOffset = 0.2357f;
        const float diagonalYOffset = 0.3536f;

        return shape == IconGuideShape.FlatTopBottom
            ? new[]
            {
                new PointF(center - flatXOffset, 0f),
                new PointF(center + flatXOffset, 0f),
                new PointF(right, flatYShoulder),
                new PointF(right, 1f - flatYShoulder),
                new PointF(center + flatXOffset, 1f),
                new PointF(center - flatXOffset, 1f),
                new PointF(left, 1f - flatYShoulder),
                new PointF(left, flatYShoulder)
            }
            : new[]
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
    private SymbolFrameShape _frameShape = SymbolFrameShape.FriendlyUnit;
    private SymbolFrameStatus _frameStatus = SymbolFrameStatus.Present;

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

    public void SetFrame(SymbolFrameShape frameShape, SymbolFrameStatus frameStatus)
    {
        _frameShape = frameShape;
        _frameStatus = frameStatus;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Color.White);

        var contentBounds = new RectangleF(30, 68, Math.Max(1, ClientSize.Width - 60), Math.Max(1, ClientSize.Height - 98));
        var frame = SymbolFrameRenderer.GetFittedFrame(contentBounds, _frameShape, _commands, IconGuideShape.FlatTopBottom);

        using var pen = new Pen(Color.Black, 2f);
        SymbolFrameRenderer.DrawFrame(e.Graphics, frame, _frameShape, _frameStatus, fillFrame: true, IconGuideShape.FlatTopBottom);
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
    public SymbolFrameShape FrameShape { get; set; } = SymbolFrameShape.FriendlyUnit;
    public SymbolFrameStatus FrameStatus { get; set; } = SymbolFrameStatus.Present;
    public List<SymbolDrawCommand> Commands { get; set; } = new();
}

internal static class SymbolFrameRenderer
{
    private const float StandardFrameAspectRatio = 1.5f;

    public static void DrawFrame(Graphics graphics, RectangleF frame, SymbolFrameShape shape, SymbolFrameStatus status, bool fillFrame, IconGuideShape guideShape)
    {
        using var pen = new Pen(Color.Black, 2f);
        if (status == SymbolFrameStatus.PlannedAnticipated)
            pen.DashStyle = DashStyle.Dash;

        using var path = CreatePath(frame, shape, guideShape);
        if (fillFrame)
        {
            var palette = GetPalette(shape);
            using var fill = new SolidBrush(palette.Fill);
            graphics.FillPath(fill, path);
        }

        graphics.DrawPath(pen, path);
    }

    public static RectangleF GetFittedFrame(RectangleF contentBounds, SymbolFrameShape shape, IReadOnlyList<SymbolDrawCommand> commands, IconGuideShape guideShape)
    {
        var normalizedBounds = GetNormalizedVisualBounds(shape, commands, guideShape);
        if (normalizedBounds.Width <= 0f || normalizedBounds.Height <= 0f)
            normalizedBounds = new RectangleF(0f, 0f, 1f, 1f);

        var frameWidthFromContentWidth = contentBounds.Width / normalizedBounds.Width;
        var frameHeightFromContentHeight = contentBounds.Height / normalizedBounds.Height;
        var frameHeight = Math.Min(frameWidthFromContentWidth / StandardFrameAspectRatio, frameHeightFromContentHeight);
        var frameWidth = frameHeight * StandardFrameAspectRatio;
        var frameLeft = contentBounds.Left + (contentBounds.Width - frameWidth * normalizedBounds.Width) / 2f - frameWidth * normalizedBounds.Left;
        var frameTop = contentBounds.Top + (contentBounds.Height - frameHeight * normalizedBounds.Height) / 2f - frameHeight * normalizedBounds.Top;
        return new RectangleF(frameLeft, frameTop, frameWidth, frameHeight);
    }

    private static RectangleF GetNormalizedVisualBounds(SymbolFrameShape shape, IReadOnlyList<SymbolDrawCommand> commands, IconGuideShape guideShape)
    {
        using var path = CreatePath(new RectangleF(0f, 0f, 1f, 1f), shape, guideShape);
        var bounds = path.GetBounds();
        foreach (var command in commands)
            bounds = Union(bounds, command.GetNormalizedVisualBounds());

        return bounds;
    }

    private static RectangleF Union(RectangleF first, RectangleF second)
    {
        if (first.IsEmpty)
            return second;
        if (second.IsEmpty)
            return first;

        var left = Math.Min(first.Left, second.Left);
        var top = Math.Min(first.Top, second.Top);
        var right = Math.Max(first.Right, second.Right);
        var bottom = Math.Max(first.Bottom, second.Bottom);
        return RectangleF.FromLTRB(left, top, right, bottom);
    }

    public static SymbolPalette GetPalette(SymbolFrameShape shape)
    {
        return shape switch
        {
            SymbolFrameShape.Hostile => new SymbolPalette(Color.FromArgb(255, 128, 128), Color.FromArgb(255, 0, 0)),
            SymbolFrameShape.Neutral => new SymbolPalette(Color.FromArgb(170, 255, 170), Color.FromArgb(0, 255, 0)),
            SymbolFrameShape.Unknown => new SymbolPalette(Color.FromArgb(255, 255, 128), Color.FromArgb(255, 255, 0)),
            _ => new SymbolPalette(Color.FromArgb(128, 224, 255), Color.FromArgb(0, 255, 255))
        };
    }

    private static GraphicsPath CreatePath(RectangleF frame, SymbolFrameShape shape, IconGuideShape guideShape)
    {
        var path = new GraphicsPath();
        switch (shape)
        {
            case SymbolFrameShape.FriendlyEquipment:
                path.AddEllipse(GetGuideCircumcircleBounds(frame, guideShape));
                break;
            case SymbolFrameShape.Hostile:
                path.AddPolygon(GetHostileDiamondPoints(frame, guideShape));
                break;
            case SymbolFrameShape.Neutral:
                var side = Math.Min(frame.Width, frame.Height);
                path.AddRectangle(new RectangleF(
                    frame.Left + (frame.Width - side) / 2f,
                    frame.Top + (frame.Height - side) / 2f,
                    side,
                    side));
                break;
            case SymbolFrameShape.Unknown:
                AddUnknownFrame(path, frame);
                break;
            default:
                path.AddRectangle(frame);
                break;
        }

        return path;
    }

    private static void AddUnknownFrame(GraphicsPath path, RectangleF frame)
    {
        path.AddBezier(
            ToAbsolute(frame, new PointF(0.26995647f, 0.14187229f)),
            ToAbsolute(frame, new PointF(0.16981132f, -0.24999999f)),
            ToAbsolute(frame, new PointF(0.83309144f, -0.24999999f)),
            ToAbsolute(frame, new PointF(0.7445573f, 0.15275763f)));
        path.AddBezier(
            ToAbsolute(frame, new PointF(0.7445573f, 0.15275763f)),
            ToAbsolute(frame, new PointF(1f, 0f)),
            ToAbsolute(frame, new PointF(1f, 1f)),
            ToAbsolute(frame, new PointF(0.74746007f, 0.8385341f)));
        path.AddBezier(
            ToAbsolute(frame, new PointF(0.74746007f, 0.8385341f)),
            ToAbsolute(frame, new PointF(0.796807f, 1.25f)),
            ToAbsolute(frame, new PointF(0.20029028f, 1.2042816f)),
            ToAbsolute(frame, new PointF(0.26850507f, 0.86030483f)));
        path.AddBezier(
            ToAbsolute(frame, new PointF(0.26850507f, 0.86030483f)),
            ToAbsolute(frame, new PointF(0f, 1f)),
            ToAbsolute(frame, new PointF(0f, 0f)),
            ToAbsolute(frame, new PointF(0.26995647f, 0.14187229f)));
        path.CloseFigure();
    }

    private static RectangleF GetGuideCircumcircleBounds(RectangleF frame, IconGuideShape guideShape)
    {
        var center = new PointF(frame.Left + frame.Width / 2f, frame.Top + frame.Height / 2f);
        var radius = GetIconGuidePoints(guideShape)
            .Select(point => ToAbsolute(frame, point))
            .Select(point => Distance(center, point))
            .Max();
        return new RectangleF(center.X - radius, center.Y - radius, radius * 2f, radius * 2f);
    }

    private static PointF[] GetHostileDiamondPoints(RectangleF frame, IconGuideShape guideShape)
    {
        var guidePoints = GetIconGuidePoints(guideShape).Select(point => ToAbsolute(frame, point)).ToArray();
        if (guideShape == IconGuideShape.FlatTopBottom)
        {
            return new[]
            {
                GetLineIntersection(guidePoints[7], guidePoints[0], guidePoints[1], guidePoints[2]),
                GetLineIntersection(guidePoints[1], guidePoints[2], guidePoints[3], guidePoints[4]),
                GetLineIntersection(guidePoints[3], guidePoints[4], guidePoints[5], guidePoints[6]),
                GetLineIntersection(guidePoints[5], guidePoints[6], guidePoints[7], guidePoints[0])
            };
        }

        var center = new PointF(frame.Left + frame.Width / 2f, frame.Top + frame.Height / 2f);
        return new[]
        {
            guidePoints.OrderBy(point => point.Y).ThenBy(point => Math.Abs(point.X - center.X)).First(),
            guidePoints.OrderByDescending(point => point.X).ThenBy(point => Math.Abs(point.Y - center.Y)).First(),
            guidePoints.OrderByDescending(point => point.Y).ThenBy(point => Math.Abs(point.X - center.X)).First(),
            guidePoints.OrderBy(point => point.X).ThenBy(point => Math.Abs(point.Y - center.Y)).First()
        };
    }

    private static PointF ToAbsolute(RectangleF frame, PointF point) =>
        new(frame.Left + frame.Width * point.X, frame.Top + frame.Height * point.Y);

    private static PointF GetLineIntersection(PointF firstStart, PointF firstEnd, PointF secondStart, PointF secondEnd)
    {
        var a1 = firstEnd.Y - firstStart.Y;
        var b1 = firstStart.X - firstEnd.X;
        var c1 = a1 * firstStart.X + b1 * firstStart.Y;
        var a2 = secondEnd.Y - secondStart.Y;
        var b2 = secondStart.X - secondEnd.X;
        var c2 = a2 * secondStart.X + b2 * secondStart.Y;
        var determinant = a1 * b2 - a2 * b1;
        if (Math.Abs(determinant) < 0.0001f)
            return firstEnd;

        return new PointF(
            (b2 * c1 - b1 * c2) / determinant,
            (a1 * c2 - a2 * c1) / determinant);
    }

    private static float Distance(PointF first, PointF second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static PointF[] GetIconGuidePoints(IconGuideShape shape)
    {
        const float left = 1f / 6f;
        const float right = 5f / 6f;
        const float center = 0.5f;
        const float flatXOffset = 0.1381f;
        const float flatYShoulder = 0.2929f;
        const float diagonalXOffset = 0.2357f;
        const float diagonalYOffset = 0.3536f;

        return shape == IconGuideShape.FlatTopBottom
            ? new[]
            {
                new PointF(center - flatXOffset, 0f),
                new PointF(center + flatXOffset, 0f),
                new PointF(right, flatYShoulder),
                new PointF(right, 1f - flatYShoulder),
                new PointF(center + flatXOffset, 1f),
                new PointF(center - flatXOffset, 1f),
                new PointF(left, 1f - flatYShoulder),
                new PointF(left, flatYShoulder)
            }
            : new[]
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
}

internal readonly record struct SymbolPalette(Color Fill, Color Symbol);

internal sealed class SymbolDrawCommand
{
    public SymbolDrawCommandKind Kind { get; set; }
    public SymbolPoint Start { get; set; }
    public SymbolPoint End { get; set; }
    public SymbolPoint Control1 { get; set; }
    public SymbolPoint Control2 { get; set; }
    public float Radius { get; set; }
    public float FontSize { get; set; } = 12f;
    public string Text { get; set; } = string.Empty;
    public bool Filled { get; set; }
    public List<SymbolPoint> Points { get; set; } = new();
    [JsonIgnore]
    public bool CanFill => Kind is SymbolDrawCommandKind.Rectangle or SymbolDrawCommandKind.Ellipse or SymbolDrawCommandKind.Circle or SymbolDrawCommandKind.Capsule or SymbolDrawCommandKind.Path;

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

    public static SymbolDrawCommand Path(IEnumerable<SymbolPoint> points, bool filled = false)
    {
        var pointList = points.ToList();
        return new()
        {
            Kind = SymbolDrawCommandKind.Path,
            Start = pointList.Count > 0 ? pointList[0] : default,
            End = pointList.Count > 0 ? pointList[^1] : default,
            Points = pointList,
            Filled = filled
        };
    }

    public static SymbolDrawCommand Dot(PointF center, float radius) =>
        new() { Kind = SymbolDrawCommandKind.Dot, Start = new SymbolPoint(center), End = new SymbolPoint(center), Radius = radius };

    public static SymbolDrawCommand TextCommand(PointF location, string text, float fontSize = 12f) =>
        new() { Kind = SymbolDrawCommandKind.Text, Start = new SymbolPoint(location), End = new SymbolPoint(location), Text = text, FontSize = fontSize };

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
            FontSize = FontSize,
            Text = Text,
            Filled = Filled,
            Points = Points.Select(point => point).ToList()
        };

    public void Move(SymbolPoint delta)
    {
        Start = Start.Offset(delta);
        End = End.Offset(delta);
        Control1 = Control1.Offset(delta);
        Control2 = Control2.Offset(delta);
        if (Points.Count > 0)
            Points = Points.Select(point => point.Offset(delta)).ToList();
    }

    public void RotateClockwise(SymbolPoint center)
    {
        TransformPoints(point =>
        {
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            return new SymbolPoint(center.X - dy, center.Y + dx);
        });
    }

    public void MirrorHorizontal(float centerX)
    {
        TransformPoints(point => new SymbolPoint(centerX * 2f - point.X, point.Y));
    }

    public void MirrorVertical(float centerY)
    {
        TransformPoints(point => new SymbolPoint(point.X, centerY * 2f - point.Y));
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

    private void TransformPoints(Func<SymbolPoint, SymbolPoint> transform)
    {
        Start = transform(Start);
        End = transform(End);
        Control1 = transform(Control1);
        Control2 = transform(Control2);
        if (Points.Count > 0)
            Points = Points.Select(transform).ToList();

        if (Kind is SymbolDrawCommandKind.Dot or SymbolDrawCommandKind.Text)
            End = Start;
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
        if (Kind == SymbolDrawCommandKind.Path)
        {
            for (var index = 0; index < Points.Count - 1; index++)
                yield return new SymbolSegment(Points[index], Points[index + 1]);
            if (Points.Count > 2 && Distance(Points[0], Points[^1]) > 0.0001f)
                yield return new SymbolSegment(Points[^1], Points[0]);
            yield break;
        }

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
            SymbolDrawCommandKind.Text => Distance(mousePoint, ToAbsolute(frame, Start)) <= Math.Max(24f, GetScaledFontSize(frame) * 1.5f),
            SymbolDrawCommandKind.Path => HitTestPath(mousePoint, frame, threshold),
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
            case SymbolDrawCommandKind.Path:
                using (var path = CreateGraphicsPath(frame))
                {
                    if (Filled)
                        graphics.FillPath(brush, path);
                    graphics.DrawPath(pen, path);
                }
                break;
            case SymbolDrawCommandKind.Dot:
                var center = ToAbsolute(frame, Start);
                var dotRadius = Radius * Math.Min(frame.Width, frame.Height);
                graphics.FillEllipse(brush, center.X - dotRadius, center.Y - dotRadius, dotRadius * 2f, dotRadius * 2f);
                break;
            case SymbolDrawCommandKind.Text:
                var location = ToAbsolute(frame, Start);
                var fontSize = GetScaledFontSize(frame);
                using (var font = new Font(SystemFonts.DefaultFont.FontFamily, fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    var textBounds = GetTextBounds(frame, location, fontSize);
                    graphics.DrawString(Text, font, brush, textBounds, format);
                }
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
            SymbolDrawCommandKind.Text => $"{Kind} \"{Text}\" {Format(FontSize)}% at {FormatPoint(Start)}",
            SymbolDrawCommandKind.Path => $"{Kind} {Points.Count} points" + (Filled ? " filled" : string.Empty),
            _ => $"{Kind} {FormatPoint(Start)} to {FormatPoint(End)}"
        };
    }

    public RectangleF GetNormalizedVisualBounds()
    {
        return Kind switch
        {
            SymbolDrawCommandKind.Circle => RectangleF.FromLTRB(Start.X - Radius, Start.Y - Radius, Start.X + Radius, Start.Y + Radius),
            SymbolDrawCommandKind.Dot => RectangleF.FromLTRB(Start.X - Radius, Start.Y - Radius, Start.X + Radius, Start.Y + Radius),
            SymbolDrawCommandKind.Text => GetTextNormalizedBounds(),
            SymbolDrawCommandKind.Path => GetPointsBounds(Points),
            SymbolDrawCommandKind.Bezier => GetPointsBounds(new[] { Start, End, Control1, Control2 }),
            _ => GetPointsBounds(new[] { Start, End })
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
            SymbolDrawCommandKind.Path =>
                PathCode(graphics, pen, brush, bounds),
            SymbolDrawCommandKind.Dot =>
                $"{graphics}.FillEllipse({brush}, {PointCode(bounds, Start)}.X - {RadiusCode(bounds)}, {PointCode(bounds, Start)}.Y - {RadiusCode(bounds)}, {RadiusCode(bounds)} * 2f, {RadiusCode(bounds)} * 2f);",
            SymbolDrawCommandKind.Text =>
                $"{{\r\n    var textValue = \"{EscapeCSharpString(Text)}\";\r\n    var textLocation = {PointCode(bounds, Start)};\r\n    var textSize = {bounds}.Height * {Format(FontSize / 100f)}f;\r\n    var textWidth = Math.Min({bounds}.Width, Math.Max({bounds}.Width * 0.45f, Math.Max(1, textValue.Length) * textSize * 1.15f));\r\n    var textHeight = textSize * 1.6f;\r\n    using var textFont = new Font(font.FontFamily, textSize, FontStyle.Bold, GraphicsUnit.Pixel);\r\n    using var textFormat = new StringFormat {{ Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap }};\r\n    {graphics}.DrawString(textValue, textFont, {brush}, new RectangleF(textLocation.X - textWidth / 2f, textLocation.Y - textHeight / 2f, textWidth, textHeight), textFormat);\r\n}}",
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

    private GraphicsPath CreateGraphicsPath(RectangleF frame)
    {
        var path = new GraphicsPath();
        if (Points.Count == 0)
            return path;

        var absolutePoints = Points.Select(point => ToAbsolute(frame, point)).ToArray();
        if (absolutePoints.Length == 1)
            path.AddLine(absolutePoints[0], absolutePoints[0]);
        else
            path.AddLines(absolutePoints);
        path.CloseFigure();
        return path;
    }

    private bool HitTestPath(Point mousePoint, RectangleF frame, float threshold)
    {
        using var path = CreateGraphicsPath(frame);
        using var hitPen = new Pen(Color.Black, threshold * 2f);
        if (path.IsOutlineVisible(mousePoint, hitPen) || path.IsVisible(mousePoint))
            return true;

        var segments = GetSegments().ToArray();
        return segments.Any(segment =>
            DistanceToSegment(mousePoint, ToAbsolute(frame, segment.Start), ToAbsolute(frame, segment.End)) <= threshold);
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

    private RectangleF GetTextNormalizedBounds()
    {
        var height = Math.Clamp(FontSize, 4f, 72f) / 100f;
        var width = Math.Max(0.18f, Math.Max(1, Text.Length) * height * 0.72f);
        return RectangleF.FromLTRB(Start.X - width / 2f, Start.Y - height / 2f, Start.X + width / 2f, Start.Y + height / 2f);
    }

    private static RectangleF GetPointsBounds(IReadOnlyList<SymbolPoint> points)
    {
        if (points.Count == 0)
            return RectangleF.Empty;

        var left = points.Min(point => point.X);
        var top = points.Min(point => point.Y);
        var right = points.Max(point => point.X);
        var bottom = points.Max(point => point.Y);
        if (Math.Abs(right - left) < 0.0001f)
        {
            left -= 0.001f;
            right += 0.001f;
        }

        if (Math.Abs(bottom - top) < 0.0001f)
        {
            top -= 0.001f;
            bottom += 0.001f;
        }

        return RectangleF.FromLTRB(left, top, right, bottom);
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

    private string PathCode(string graphics, string pen, string brush, string bounds)
    {
        if (Points.Count == 0)
            return string.Empty;

        var pointLines = string.Join(
            ",\r\n        ",
            Points.Select(point => PointCode(bounds, point)));
        var fillLine = Filled ? $"    {graphics}.FillPath({brush}, closedPath);\r\n" : string.Empty;
        return "{\r\n"
            + "    using var closedPath = new GraphicsPath();\r\n"
            + "    closedPath.AddLines(new[]\r\n"
            + "    {\r\n"
            + $"        {pointLines}\r\n"
            + "    });\r\n"
            + "    closedPath.CloseFigure();\r\n"
            + fillLine
            + $"    {graphics}.DrawPath({pen}, closedPath);\r\n"
            + "}";
    }

    private float GetScaledFontSize(RectangleF frame) =>
        Math.Clamp(FontSize, 4f, 72f) / 100f * frame.Height;

    private RectangleF GetTextBounds(RectangleF frame, PointF location, float fontSize)
    {
        var textWidth = Math.Min(
            frame.Width,
            Math.Max(frame.Width * 0.45f, Math.Max(1, Text.Length) * fontSize * 1.15f));
        var textHeight = fontSize * 1.6f;
        return new RectangleF(location.X - textWidth / 2f, location.Y - textHeight / 2f, textWidth, textHeight);
    }

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
    Bezier,
    Path
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
