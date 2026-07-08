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
    private readonly ComboBox _affiliationComboBox = new();
    private readonly ComboBox _physicalDomainComboBox = new();
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
    private readonly NumericUpDown _strokeWidthInput = CreateStrokeWidthInput();
    private readonly TextBox _textInput = new();
    private readonly TextBox _drawTextInput = new() { Text = "TXT", Width = 80 };
    private readonly NumericUpDown _drawTextSizeInput = CreateFontSizeInput();
    private readonly NumericUpDown _drawStrokeWidthInput = CreateStrokeWidthInput();
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

        _affiliationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _affiliationComboBox.Width = 92;
        _affiliationComboBox.Items.AddRange(Enum.GetNames<SymbolAffiliation>().Cast<object>().ToArray());
        _affiliationComboBox.SelectedItem = SymbolAffiliation.Friendly.ToString();
        _affiliationComboBox.SelectedIndexChanged += (_, _) =>
        {
            _canvas.FrameShape = GetSelectedFrameShape();
            RefreshOutput();
            _canvas.Invalidate();
        };

        _physicalDomainComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _physicalDomainComboBox.Width = 92;
        _physicalDomainComboBox.Items.AddRange(Enum.GetNames<SymbolPhysicalDomain>().Cast<object>().ToArray());
        _physicalDomainComboBox.SelectedItem = SymbolPhysicalDomain.LandUnit.ToString();
        _physicalDomainComboBox.SelectedIndexChanged += (_, _) =>
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
        _drawStrokeWidthInput.ValueChanged += (_, _) =>
        {
            _canvas.DrawStrokeWidth = (float)_drawStrokeWidthInput.Value;
            ApplyToolbarStrokeToSelection();
        };

        var loadButton = CreateButton("Load reference", LoadReferenceImage);
        var loadClipboardButton = CreateButton("Load clipboard", LoadReferenceFromClipboard);
        var resetReferenceButton = CreateButton("Reset ref", () => _canvas.ResetReferenceTransform());
        var loadBaseButton = CreateButton("Load base", LoadBaseSymbol);
        var saveLibraryButton = CreateButton("Save library", SaveLibrary);
        var loadLibraryButton = CreateButton("Load library", LoadLibrary);
        var viewLibraryButton = CreateButton("View library", ViewLibrary);
        var undoButton = CreateButton("Undo", () => _canvas.Undo());
        var redoButton = CreateButton("Redo", () => _canvas.Redo());
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
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Affiliation", Margin = new Padding(14, 6, 4, 0) });
        toolbar.Controls.Add(_affiliationComboBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Domain", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_physicalDomainComboBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Status", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_frameStatusComboBox);
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Tool", Margin = new Padding(14, 6, 4, 0) });
        toolbar.Controls.Add(_toolComboBox);
        toolbar.Controls.Add(loadButton);
        toolbar.Controls.Add(loadClipboardButton);
        toolbar.Controls.Add(resetReferenceButton);
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
        toolbar.Controls.Add(new Label { AutoSize = true, Text = "Stroke", Margin = new Padding(8, 6, 4, 0) });
        toolbar.Controls.Add(_drawStrokeWidthInput);
        toolbar.Controls.Add(undoButton);
        toolbar.Controls.Add(redoButton);
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
        _canvas.DrawStrokeWidth = (float)_drawStrokeWidthInput.Value;
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

    private static NumericUpDown CreateStrokeWidthInput()
    {
        return new NumericUpDown
        {
            DecimalPlaces = 1,
            Increment = 0.5m,
            Minimum = 0.5m,
            Maximum = 12m,
            Value = 2m,
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
            RowCount = 8,
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
        AddCoordinateRow(panel, 5, "Stroke", _strokeWidthInput, string.Empty, new Panel());
        AddTextRow(panel, 6, "Text", _textInput);
        panel.Controls.Add(new Label { AutoSize = true, Text = "Select a command, then drag it on the canvas or edit values here.", ForeColor = SystemColors.GrayText }, 0, 7);
        panel.SetColumnSpan(panel.GetControlFromPosition(0, 7)!, 4);
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
            _control1XInput, _control1YInput, _control2XInput, _control2YInput, _radiusInput, _fontSizeInput, _strokeWidthInput
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
        return SymbolFrameMapping.GetFrameShape(GetSelectedAffiliation(), GetSelectedPhysicalDomain());
    }

    private SymbolAffiliation GetSelectedAffiliation()
    {
        return Enum.TryParse(Convert.ToString(_affiliationComboBox.SelectedItem), out SymbolAffiliation affiliation)
            ? affiliation
            : SymbolAffiliation.Friendly;
    }

    private SymbolPhysicalDomain GetSelectedPhysicalDomain()
    {
        return Enum.TryParse(Convert.ToString(_physicalDomainComboBox.SelectedItem), out SymbolPhysicalDomain domain)
            ? domain
            : SymbolPhysicalDomain.LandUnit;
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
            SymbolDesignerTool.ParallelLine => "ParallelLine: select an existing line or segment, then drag a new line parallel to it.",
            SymbolDesignerTool.PerpendicularLine => "PerpendicularLine: select an existing line or segment, then drag a new line perpendicular to it.",
            SymbolDesignerTool.Arc => "Arc: click start, click highest point, click end.",
            SymbolDesignerTool.Circle => "Circle: drag from the center outward. Use Fill closed for a solid circle.",
            SymbolDesignerTool.Text => "Text: enter text in the toolbar, then click the canvas to place it.",
            _ => "Draw: drag on the canvas. Reference: right-drag to move, mouse wheel to zoom, Reset ref to fit."
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
            Affiliation = GetSelectedAffiliation(),
            PhysicalDomain = GetSelectedPhysicalDomain(),
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
            : normalized.Equals("chemical", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("nbc", StringComparison.OrdinalIgnoreCase)
                ? Components.OrbatUnitType.CBRN.ToString()
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
        _affiliationComboBox.SelectedItem = definition.GetEffectiveAffiliation().ToString();
        _physicalDomainComboBox.SelectedItem = definition.GetEffectivePhysicalDomain().ToString();
        _frameStatusComboBox.SelectedItem = definition.FrameStatus.ToString();
        _canvas.FrameShape = definition.GetEffectiveFrameShape();
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
        _canvas.AddCommand(SymbolDrawCommand.AirDefenseArc().WithStrokeWidth((float)_drawStrokeWidthInput.Value));
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
        _preview.PhysicalDomain = GetSelectedPhysicalDomain();
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
                _control1XInput, _control1YInput, _control2XInput, _control2YInput, _radiusInput, _fontSizeInput, _strokeWidthInput, _textInput
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
            _strokeWidthInput.Value = ToStrokeWidthDecimal(command.StrokeWidth);
            _drawStrokeWidthInput.Value = ToStrokeWidthDecimal(command.StrokeWidth);
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

        _canvas.UpdateSelectedCommand(command => command.Filled = _fillCheckBox.Checked);
    }

    private void ApplyToolbarStrokeToSelection()
    {
        if (_updatingSelectionControls)
            return;

        var command = _canvas.SelectedCommand;
        if (command == null || !command.UsesStroke)
            return;

        var strokeWidth = Math.Clamp((float)_drawStrokeWidthInput.Value, 0.5f, 12f);
        _strokeWidthInput.Value = ToStrokeWidthDecimal(strokeWidth);
        _canvas.UpdateSelectedCommand(command => command.StrokeWidth = strokeWidth);
    }

    private void ApplySelectionControls()
    {
        if (_updatingSelectionControls)
            return;

        var command = _canvas.SelectedCommand;
        if (command == null)
            return;

        _canvas.UpdateSelectedCommand(command =>
        {
            command.Start = new SymbolPoint((float)_startXInput.Value, (float)_startYInput.Value);
            command.End = new SymbolPoint((float)_endXInput.Value, (float)_endYInput.Value);
            command.Control1 = new SymbolPoint((float)_control1XInput.Value, (float)_control1YInput.Value);
            command.Control2 = new SymbolPoint((float)_control2XInput.Value, (float)_control2YInput.Value);
            command.Radius = Math.Clamp((float)_radiusInput.Value, 0f, 1f);
            command.FontSize = Math.Clamp((float)_fontSizeInput.Value, 4f, 72f);
            command.StrokeWidth = Math.Clamp((float)_strokeWidthInput.Value, 0.5f, 12f);
            command.Text = _textInput.Text;
        });
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

    private static decimal ToStrokeWidthDecimal(float value) => Math.Min(12m, Math.Max(0.5m, (decimal)value));

    private void HandleShortcutKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsEditingInput())
            return;

        if (e.Control && (e.KeyCode == Keys.Y || (e.Shift && e.KeyCode == Keys.Z)))
        {
            _canvas.Redo();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.Z)
        {
            _canvas.Undo();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.C)
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
            || _strokeWidthInput.ContainsFocus
            || _drawTextSizeInput.ContainsFocus
            || _drawStrokeWidthInput.ContainsFocus;
    }
}

internal enum SymbolDesignerTool
{
    SelectMove,
    Line,
    ParallelLine,
    PerpendicularLine,
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

internal enum SymbolAffiliation
{
    Friendly,
    Hostile,
    Neutral,
    Unknown
}

internal enum SymbolPhysicalDomain
{
    LandUnit,
    Equipment
}

internal enum SymbolFrameStatus
{
    Present,
    PlannedAnticipated
}

internal static class SymbolFrameMapping
{
    public static SymbolFrameShape GetFrameShape(SymbolAffiliation affiliation, SymbolPhysicalDomain domain)
    {
        return affiliation switch
        {
            SymbolAffiliation.Friendly => domain == SymbolPhysicalDomain.Equipment
                ? SymbolFrameShape.FriendlyEquipment
                : SymbolFrameShape.FriendlyUnit,
            SymbolAffiliation.Hostile => SymbolFrameShape.Hostile,
            SymbolAffiliation.Neutral => SymbolFrameShape.Neutral,
            SymbolAffiliation.Unknown => SymbolFrameShape.Unknown,
            _ => SymbolFrameShape.FriendlyUnit
        };
    }

    public static SymbolAffiliation GetAffiliation(SymbolFrameShape frameShape)
    {
        return frameShape switch
        {
            SymbolFrameShape.Hostile => SymbolAffiliation.Hostile,
            SymbolFrameShape.Neutral => SymbolAffiliation.Neutral,
            SymbolFrameShape.Unknown => SymbolAffiliation.Unknown,
            _ => SymbolAffiliation.Friendly
        };
    }

    public static SymbolPhysicalDomain GetPhysicalDomain(SymbolFrameShape frameShape)
    {
        return frameShape == SymbolFrameShape.FriendlyEquipment
            ? SymbolPhysicalDomain.Equipment
            : SymbolPhysicalDomain.LandUnit;
    }
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
    private const int HistoryLimit = 100;
    private readonly List<SymbolDrawCommand> _commands = new();
    private readonly Stack<SymbolCanvasSnapshot> _undoStack = new();
    private readonly Stack<SymbolCanvasSnapshot> _redoStack = new();
    private Bitmap? _referenceImage;
    private float _referenceScale = 1f;
    private PointF _referenceOffset;
    private bool _draggingReference;
    private Point _referenceDragStart;
    private PointF _referenceDragStartOffset;
    private PointF? _dragStart;
    private PointF? _dragCurrent;
    private PointF? _lastDragPoint;
    private PointF? _arcStart;
    private PointF? _arcPeak;
    private DragTarget _dragTarget = DragTarget.None;
    private SymbolDesignerTool _tool = SymbolDesignerTool.Line;
    private SymbolDrawCommand? _copiedCommand;
    private SymbolCanvasSnapshot? _pendingEditSnapshot;
    private bool _editHistoryCommitted;

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
    public float DrawStrokeWidth { get; set; } = 2f;
    public int GridDivisions { get; set; } = 12;
    public int SelectedIndex { get; private set; } = -1;
    public SymbolDrawCommand? SelectedCommand => SelectedIndex >= 0 && SelectedIndex < _commands.Count ? _commands[SelectedIndex] : null;
    public IReadOnlyList<SymbolDrawCommand> Commands => _commands;

    public SymbolDesignerCanvas()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        Cursor = Cursors.Cross;
        TabStop = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public void LoadReferenceImage(string fileName)
    {
        _referenceImage?.Dispose();
        _referenceImage = new Bitmap(fileName);
        ResetReferenceTransform();
        Invalidate();
    }

    public void LoadReferenceImage(Image image)
    {
        _referenceImage?.Dispose();
        _referenceImage = new Bitmap(image);
        ResetReferenceTransform();
        Invalidate();
    }

    public void ResetReferenceTransform()
    {
        _referenceScale = 1f;
        _referenceOffset = PointF.Empty;
        Invalidate();
    }

    public void SetCommands(IEnumerable<SymbolDrawCommand> commands)
    {
        SaveHistory();
        _commands.Clear();
        _commands.AddRange(commands.Select(command => command.Clone()));
        SelectedIndex = _commands.Count > 0 ? 0 : -1;
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void AddCommand(SymbolDrawCommand command)
    {
        SaveHistory();
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

        SaveHistory();
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

    public void UpdateSelectedCommand(Action<SymbolDrawCommand> update)
    {
        var command = SelectedCommand;
        if (command == null)
            return;

        var before = CaptureSnapshot();
        update(command);
        if (SnapshotEquals(before))
            return;

        PushUndoSnapshot(before);
        _redoStack.Clear();
        NotifyCommandsChanged(includeSelection: false);
    }

    private void TransformSelected(Action<SymbolDrawCommand> transform)
    {
        var command = SelectedCommand;
        if (command == null)
            return;

        SaveHistory();
        transform(command);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void DeleteSelected()
    {
        if (SelectedIndex < 0 || SelectedIndex >= _commands.Count)
            return;

        SaveHistory();
        _commands.RemoveAt(SelectedIndex);
        SelectedIndex = Math.Min(SelectedIndex, _commands.Count - 1);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        _redoStack.Push(CaptureSnapshot());
        RestoreSnapshot(_undoStack.Pop());
        NotifyCommandsChanged(includeSelection: true);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        _undoStack.Push(CaptureSnapshot());
        RestoreSnapshot(_redoStack.Pop());
        NotifyCommandsChanged(includeSelection: true);
    }

    public void ClearCommands()
    {
        if (_commands.Count == 0 && SelectedIndex == -1)
            return;

        SaveHistory();
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

    private void SaveHistory()
    {
        PushUndoSnapshot(CaptureSnapshot());
        _redoStack.Clear();
    }

    private void PushUndoSnapshot(SymbolCanvasSnapshot snapshot)
    {
        _undoStack.Push(snapshot);
        if (_undoStack.Count <= HistoryLimit)
            return;

        var snapshots = _undoStack.ToArray().Take(HistoryLimit).Reverse().ToArray();
        _undoStack.Clear();
        foreach (var item in snapshots)
            _undoStack.Push(item);
    }

    private SymbolCanvasSnapshot CaptureSnapshot() =>
        new(_commands.Select(command => command.Clone()).ToList(), SelectedIndex);

    private void RestoreSnapshot(SymbolCanvasSnapshot snapshot)
    {
        _commands.Clear();
        _commands.AddRange(snapshot.Commands.Select(command => command.Clone()));
        SelectedIndex = snapshot.SelectedIndex >= 0 && snapshot.SelectedIndex < _commands.Count
            ? snapshot.SelectedIndex
            : _commands.Count > 0 ? Math.Min(snapshot.SelectedIndex, _commands.Count - 1) : -1;
    }

    private bool SnapshotEquals(SymbolCanvasSnapshot snapshot)
    {
        if (SelectedIndex != snapshot.SelectedIndex || _commands.Count != snapshot.Commands.Count)
            return false;

        for (var index = 0; index < _commands.Count; index++)
        {
            if (!_commands[index].HasSameState(snapshot.Commands[index]))
                return false;
        }

        return true;
    }

    private void NotifyCommandsChanged(bool includeSelection)
    {
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        if (includeSelection)
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
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
        Focus();
        if (e.Button == MouseButtons.Right && _referenceImage != null)
        {
            _draggingReference = true;
            _referenceDragStart = e.Location;
            _referenceDragStartOffset = _referenceOffset;
            Cursor = Cursors.SizeAll;
            return;
        }

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
        if (_draggingReference)
        {
            _referenceOffset = new PointF(
                _referenceDragStartOffset.X + e.Location.X - _referenceDragStart.X,
                _referenceDragStartOffset.Y + e.Location.Y - _referenceDragStart.Y);
            Invalidate();
            return;
        }

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
        if (e.Button == MouseButtons.Right && _draggingReference)
        {
            _draggingReference = false;
            Cursor = Tool == SymbolDesignerTool.SelectMove ? Cursors.Default : Cursors.Cross;
            Invalidate();
            return;
        }

        if (e.Button != MouseButtons.Left)
            return;

        if (Tool == SymbolDesignerTool.Arc)
            return;

        if (_dragTarget != DragTarget.None)
        {
            _dragTarget = DragTarget.None;
            _lastDragPoint = null;
            _pendingEditSnapshot = null;
            _editHistoryCommitted = false;
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

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_referenceImage == null || e.Delta == 0)
            return;

        var frame = GetDrawingFrameBounds();
        var oldScale = _referenceScale;
        var zoomFactor = e.Delta > 0 ? 1.1f : 1f / 1.1f;
        var newScale = Math.Clamp(oldScale * zoomFactor, 0.15f, 8f);
        if (Math.Abs(newScale - oldScale) < 0.0001f)
            return;

        var frameCenter = new PointF(frame.Left + frame.Width / 2f, frame.Top + frame.Height / 2f);
        var oldCenter = new PointF(frameCenter.X + _referenceOffset.X, frameCenter.Y + _referenceOffset.Y);
        var scaleRatio = newScale / oldScale;
        var newCenter = new PointF(
            e.X - (e.X - oldCenter.X) * scaleRatio,
            e.Y - (e.Y - oldCenter.Y) * scaleRatio);

        _referenceScale = newScale;
        _referenceOffset = new PointF(newCenter.X - frameCenter.X, newCenter.Y - frameCenter.Y);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var workspace = GetWorkspaceBounds();
        var frame = GetFrameBounds();
        var drawingFrame = GetDrawingFrameBounds(frame);
        var guideFrame = GetGuideFrameBounds(frame);
        e.Graphics.FillRectangle(Brushes.White, workspace);
        DrawReference(e.Graphics, drawingFrame);
        if (ShowGrid)
            DrawGrid(e.Graphics, guideFrame, guideFrame);
        if (ShowIconGuide)
            DrawIconGuide(e.Graphics, guideFrame);
        SymbolFrameRenderer.DrawFrame(e.Graphics, frame, FrameShape, FrameStatus, fillFrame: false, IconGuideShape);
        DrawDrawingBounds(e.Graphics, drawingFrame);

        for (var index = 0; index < _commands.Count; index++)
        {
            using var pen = new Pen(index == SelectedIndex ? Color.FromArgb(40, 120, 220) : Color.Black, index == SelectedIndex ? 2.4f : 2f);
            SymbolFrameRenderer.DrawCommand(e.Graphics, frame, GetCommandDrawingFrame(drawingFrame, _commands[index]), FrameShape, _commands[index], pen, Brushes.Black, IconGuideShape);
        }

        if (_dragStart.HasValue && _dragCurrent.HasValue)
        {
            using var previewPen = new Pen(Color.FromArgb(190, Color.Goldenrod), 1.5f) { DashStyle = DashStyle.Dash };
            CreateCommand(_dragStart.Value, _dragCurrent.Value)?.Draw(e.Graphics, drawingFrame, previewPen, Brushes.Goldenrod);
        }

        DrawArcPreview(e.Graphics, drawingFrame);

        DrawSelectionHandles(e.Graphics, drawingFrame);
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
        var frame = GetDrawingFrameBounds();
        var target = HitTestHandle(mousePoint, frame);
        if (target.Target != DragTarget.None)
        {
            SelectCommand(target.Index);
            _pendingEditSnapshot = CaptureSnapshot();
            _editHistoryCommitted = false;
            _dragTarget = target.Target;
            _lastDragPoint = symbolPoint;
            return;
        }

        var index = HitTestCommand(mousePoint, frame);
        SelectCommand(index);
        if (index >= 0)
        {
            _pendingEditSnapshot = CaptureSnapshot();
            _editHistoryCommitted = false;
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

        var before = !_editHistoryCommitted ? _pendingEditSnapshot ?? CaptureSnapshot() : null;
        if (_dragTarget == DragTarget.Move)
        {
            var delta = new SymbolPoint(symbolPoint.X - _lastDragPoint.Value.X, symbolPoint.Y - _lastDragPoint.Value.Y);
            if (IsSamePoint(delta, default))
                return;

            command.Move(delta);
            _lastDragPoint = symbolPoint;
        }
        else
        {
            var point = new SymbolPoint(symbolPoint);
            command.SetPoint(_dragTarget, point);
            _lastDragPoint = symbolPoint;
        }

        if (before != null)
        {
            if (SnapshotEquals(before))
                return;

            PushUndoSnapshot(before);
            _redoStack.Clear();
            _pendingEditSnapshot = null;
            _editHistoryCommitted = true;
        }
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private static bool IsSamePoint(SymbolPoint first, SymbolPoint second) =>
        Math.Abs(first.X - second.X) < 0.0001f && Math.Abs(first.Y - second.Y) < 0.0001f;

    private (int Index, DragTarget Target) HitTestHandle(Point mousePoint, RectangleF frame)
    {
        const float handleRadius = 10f;
        for (var index = _commands.Count - 1; index >= 0; index--)
        {
            var commandFrame = GetCommandDrawingFrame(frame, _commands[index]);
            foreach (var handle in _commands[index].GetHandles())
            {
                var absolute = ToAbsolute(commandFrame, handle.Point);
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
            if (_commands[index].HitTest(mousePoint, GetCommandDrawingFrame(frame, _commands[index]), 12f))
                return index;
        }

        return -1;
    }

    private void UpdateHoverCursor(Point mousePoint)
    {
        var frame = GetDrawingFrameBounds();
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

        var commandFrame = GetCommandDrawingFrame(frame, command);
        foreach (var handle in command.GetHandles())
        {
            var absolute = ToAbsolute(commandFrame, handle.Point);
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
        var destination = Rectangle.Round(GetReferenceDestination(frame));
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

    private RectangleF GetReferenceDestination(RectangleF frame)
    {
        var width = frame.Width * _referenceScale;
        var height = frame.Height * _referenceScale;
        return new RectangleF(
            frame.Left + (frame.Width - width) / 2f + _referenceOffset.X,
            frame.Top + (frame.Height - height) / 2f + _referenceOffset.Y,
            width,
            height);
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
        var absolutePoints = points.Select(point => ToAbsolute(guide, point)).ToArray();
        DrawIconGuideGrid(graphics, guide, absolutePoints);
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

        foreach (var x in GetSquareGridCoordinates())
        {
            var absoluteX = frame.Left + frame.Width * x;
            graphics.DrawLine(guideGridPen, absoluteX, frame.Top, absoluteX, frame.Bottom);
        }

        foreach (var y in GetSquareGridCoordinates())
        {
            var absoluteY = frame.Top + frame.Height * y;
            graphics.DrawLine(guideGridPen, frame.Left, absoluteY, frame.Right, absoluteY);
        }

        graphics.Restore(state);
    }

    private void DrawDrawingBounds(Graphics graphics, RectangleF drawingFrame)
    {
        if (FrameShape == SymbolFrameShape.FriendlyUnit)
            return;

        using var pen = new Pen(Color.FromArgb(170, 40, 120, 220), 1f) { DashStyle = DashStyle.Dash };
        graphics.DrawRectangle(pen, Rectangle.Round(drawingFrame));
    }

    private SymbolDrawCommand? CreateCommand(PointF start, PointF end)
    {
        if (Distance(start, end) < 0.012f && Tool != SymbolDesignerTool.Dot && Tool != SymbolDesignerTool.Text)
            return null;

        if (Tool == SymbolDesignerTool.Arc && !HasRenderableArea(start, end))
            return null;

        var command = Tool switch
        {
            SymbolDesignerTool.Line => SymbolDrawCommand.Line(start, end),
            SymbolDesignerTool.ParallelLine => SymbolDrawCommand.Line(start, ConstrainLineEnd(start, end, perpendicular: false)),
            SymbolDesignerTool.PerpendicularLine => SymbolDrawCommand.Line(start, ConstrainLineEnd(start, end, perpendicular: true)),
            SymbolDesignerTool.Rectangle => SymbolDrawCommand.Rectangle(start, end, FillClosedShapes),
            SymbolDesignerTool.Ellipse => SymbolDrawCommand.Ellipse(start, end, FillClosedShapes),
            SymbolDesignerTool.Circle => SymbolDrawCommand.Circle(start, end, GetDrawingFrameBounds(), FillClosedShapes),
            SymbolDesignerTool.Capsule => SymbolDrawCommand.Capsule(start, end, FillClosedShapes),
            SymbolDesignerTool.Dot => SymbolDrawCommand.Dot(end, 0.08f),
            SymbolDesignerTool.Text => SymbolDrawCommand.TextCommand(end, string.IsNullOrWhiteSpace(DrawText) ? "TXT" : DrawText, DrawFontSize),
            SymbolDesignerTool.Arc => null,
            SymbolDesignerTool.BezierArc => SymbolDrawCommand.BezierArc(start, end),
            _ => null
        };

        return command?.WithStrokeWidth(DrawStrokeWidth);
    }

    private PointF ConstrainLineEnd(PointF start, PointF end, bool perpendicular)
    {
        if (!TryGetReferenceSegment(out var reference))
            return end;

        var frame = GetDrawingFrameBounds();
        var direction = ToAbsoluteVector(reference.Start, reference.End, frame);
        if (perpendicular)
            direction = new PointF(-direction.Y, direction.X);

        var length = MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length < 0.0001f)
            return end;

        direction = new PointF(direction.X / length, direction.Y / length);
        var raw = ToAbsoluteVector(start, end, frame);
        var projectedLength = raw.X * direction.X + raw.Y * direction.Y;
        if (Math.Abs(projectedLength) < 0.5f)
        {
            var rawLength = MathF.Sqrt(raw.X * raw.X + raw.Y * raw.Y);
            projectedLength = rawLength;
        }

        return new PointF(
            start.X + direction.X * projectedLength / frame.Width,
            start.Y + direction.Y * projectedLength / frame.Height);
    }

    private bool TryGetReferenceSegment(out SymbolSegment segment)
    {
        var selectedCommand = SelectedCommand;
        if (selectedCommand != null)
        {
            foreach (var selectedSegment in selectedCommand.GetSegments())
            {
                if (HasSegmentLength(selectedSegment))
                {
                    segment = selectedSegment;
                    return true;
                }
            }
        }

        for (var index = _commands.Count - 1; index >= 0; index--)
        {
            foreach (var commandSegment in _commands[index].GetSegments())
            {
                if (HasSegmentLength(commandSegment))
                {
                    segment = commandSegment;
                    return true;
                }
            }
        }

        segment = default;
        return false;
    }

    private static bool HasSegmentLength(SymbolSegment segment) =>
        Distance(segment.Start, segment.End) >= 0.0001f;

    private static PointF ToAbsoluteVector(PointF start, PointF end, RectangleF frame) =>
        new((end.X - start.X) * frame.Width, (end.Y - start.Y) * frame.Height);

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
        var frame = GetDrawingFrameBounds();
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

        foreach (var point in GetIconGuideSnapCandidates())
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

    private RectangleF GetDrawingFrameBounds() => GetDrawingFrameBounds(GetFrameBounds());

    private RectangleF GetDrawingFrameBounds(RectangleF frame) =>
        SymbolFrameRenderer.GetInteriorFrame(frame, FrameShape, IconGuideShape);

    private RectangleF GetGuideFrameBounds(RectangleF frame) =>
        SymbolFrameRenderer.GetGuideFrame(frame, FrameShape, IconGuideShape);

    private RectangleF GetCommandDrawingFrame(RectangleF drawingFrame, SymbolDrawCommand command) =>
        SymbolFrameRenderer.GetCommandFrame(drawingFrame, FrameShape, command);

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

    private static PointF ToRelative(RectangleF frame, PointF point) =>
        new((point.X - frame.Left) / frame.Width, (point.Y - frame.Top) / frame.Height);

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

    private IEnumerable<float> GetSquareGridCoordinates()
    {
        var divisions = Math.Max(1, GridDivisions);
        var step = 1f / divisions;
        return GetCenteredGridCoordinates(step);
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

    private IEnumerable<PointF> GetIconGuideSnapCandidates()
    {
        var frame = GetDrawingFrameBounds();
        var guide = GetIconGuideBounds(frame);
        foreach (var point in GetIconGuidePoints(IconGuideShape))
            yield return ToRelative(frame, ToAbsolute(guide, point));
    }

    private static PointF[] GetIconGuidePoints(IconGuideShape shape)
    {
        const float center = 0.5f;
        if (shape == IconGuideShape.FlatTopBottom)
        {
            const float inset = 0.29289323f;
            return new[]
            {
                new PointF(inset, 0f),
                new PointF(1f - inset, 0f),
                new PointF(1f, inset),
                new PointF(1f, 1f - inset),
                new PointF(1f - inset, 1f),
                new PointF(inset, 1f),
                new PointF(0f, 1f - inset),
                new PointF(0f, inset)
            };
        }

        const float diagonalInset = 0.14644662f;
        return new[]
        {
            new PointF(center, 0f),
            new PointF(1f - diagonalInset, diagonalInset),
            new PointF(1f, center),
            new PointF(1f - diagonalInset, 1f - diagonalInset),
            new PointF(center, 1f),
            new PointF(diagonalInset, 1f - diagonalInset),
            new PointF(0f, center),
            new PointF(diagonalInset, diagonalInset)
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
    private static readonly (SymbolFrameShape Shape, string Label)[] AffiliationPreviews =
    {
        (SymbolFrameShape.FriendlyUnit, "Friendly"),
        (SymbolFrameShape.Hostile, "Hostile"),
        (SymbolFrameShape.Neutral, "Neutral"),
        (SymbolFrameShape.Unknown, "Unknown")
    };

    private IReadOnlyList<SymbolDrawCommand> _commands = Array.Empty<SymbolDrawCommand>();
    private SymbolFrameShape _frameShape = SymbolFrameShape.FriendlyUnit;
    private SymbolFrameStatus _frameStatus = SymbolFrameStatus.Present;

    public SymbolPreviewControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public float PreviewScale { get; set; } = 1f;
    public SymbolPhysicalDomain PhysicalDomain { get; set; } = SymbolPhysicalDomain.LandUnit;

    public void SetCommands(IReadOnlyList<SymbolDrawCommand> commands)
    {
        _commands = commands;
        Invalidate();
    }

    public void SetFrame(SymbolFrameShape frameShape, SymbolFrameStatus frameStatus)
    {
        _frameShape = frameShape;
        PhysicalDomain = SymbolFrameMapping.GetPhysicalDomain(frameShape);
        _frameStatus = frameStatus;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Color.White);

        TextRenderer.DrawText(
            e.Graphics,
            "Affiliation preview",
            Font,
            new Rectangle(0, 12, ClientSize.Width, 24),
            SystemColors.ControlText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var previewBounds = new RectangleF(16, 44, Math.Max(1, ClientSize.Width - 32), Math.Max(1, ClientSize.Height - 56));
        var columns = ClientSize.Width >= 420 ? 2 : 1;
        var rows = (int)Math.Ceiling(AffiliationPreviews.Length / (float)columns);
        var gap = 14f;
        var tileWidth = (previewBounds.Width - gap * (columns - 1)) / columns;
        var tileHeight = (previewBounds.Height - gap * (rows - 1)) / rows;

        for (var index = 0; index < AffiliationPreviews.Length; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var tile = new RectangleF(
                previewBounds.Left + column * (tileWidth + gap),
                previewBounds.Top + row * (tileHeight + gap),
                tileWidth,
                tileHeight);
            var shape = AffiliationPreviews[index].Shape == SymbolFrameShape.FriendlyUnit
                ? SymbolFrameMapping.GetFrameShape(SymbolAffiliation.Friendly, PhysicalDomain)
                : AffiliationPreviews[index].Shape;
            DrawAffiliationPreview(e.Graphics, tile, shape, AffiliationPreviews[index].Label);
        }
    }

    private void DrawAffiliationPreview(Graphics graphics, RectangleF tile, SymbolFrameShape shape, string label)
    {
        TextRenderer.DrawText(
            graphics,
            label,
            Font,
            Rectangle.Round(new RectangleF(tile.Left, tile.Top, tile.Width, 20)),
            SystemColors.ControlText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var contentBounds = new RectangleF(tile.Left + 6, tile.Top + 26, Math.Max(1, tile.Width - 12), Math.Max(1, tile.Height - 34));
        var frame = GetPreviewFrame(contentBounds, shape);
        var symbolFrame = SymbolFrameRenderer.GetInteriorFrame(frame, shape, IconGuideShape.FlatTopBottom);

        using var pen = new Pen(Color.Black, 2f);
        SymbolFrameRenderer.DrawFrame(graphics, frame, shape, _frameStatus, fillFrame: true, IconGuideShape.FlatTopBottom);
        foreach (var command in _commands)
            SymbolFrameRenderer.DrawCommand(graphics, frame, SymbolFrameRenderer.GetCommandFrame(symbolFrame, shape, command), shape, command, pen, Brushes.Black, IconGuideShape.FlatTopBottom);

        if (shape == _frameShape)
        {
            using var selectedPen = new Pen(Color.FromArgb(40, 120, 220), 1.4f);
            var selection = RectangleF.Inflate(frame, 5f, 5f);
            graphics.DrawRectangle(selectedPen, Rectangle.Round(selection));
        }
    }

    private RectangleF GetPreviewFrame(RectangleF contentBounds, SymbolFrameShape shape)
    {
        var frame = SymbolFrameRenderer.GetFittedFrame(contentBounds, shape, Array.Empty<SymbolDrawCommand>(), IconGuideShape.FlatTopBottom);
        if (PreviewScale <= 1f)
            return frame;

        return SymbolFrameRenderer.ScaleFrameToFitVisualBounds(frame, contentBounds, shape, IconGuideShape.FlatTopBottom, PreviewScale);
    }

}

internal sealed class SymbolLibraryDefinition
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public SymbolAffiliation Affiliation { get; set; } = SymbolAffiliation.Friendly;
    public SymbolPhysicalDomain PhysicalDomain { get; set; } = SymbolPhysicalDomain.LandUnit;
    public SymbolFrameShape FrameShape { get; set; } = SymbolFrameShape.FriendlyUnit;
    public SymbolFrameStatus FrameStatus { get; set; } = SymbolFrameStatus.Present;
    public List<SymbolDrawCommand> Commands { get; set; } = new();

    public SymbolAffiliation GetEffectiveAffiliation() =>
        FrameShape == SymbolFrameMapping.GetFrameShape(Affiliation, PhysicalDomain)
            ? Affiliation
            : SymbolFrameMapping.GetAffiliation(FrameShape);

    public SymbolPhysicalDomain GetEffectivePhysicalDomain() =>
        FrameShape == SymbolFrameMapping.GetFrameShape(Affiliation, PhysicalDomain)
            ? PhysicalDomain
            : SymbolFrameMapping.GetPhysicalDomain(FrameShape);

    public SymbolFrameShape GetEffectiveFrameShape() =>
        SymbolFrameMapping.GetFrameShape(GetEffectiveAffiliation(), GetEffectivePhysicalDomain());
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

    public static void DrawCommand(Graphics graphics, RectangleF frame, RectangleF commandFrame, SymbolFrameShape shape, SymbolDrawCommand command, Pen pen, Brush brush, IconGuideShape guideShape)
    {
        var state = graphics.Save();
        using var path = CreatePath(frame, shape, guideShape);
        graphics.SetClip(path, CombineMode.Intersect);
        command.Draw(graphics, commandFrame, pen, brush);
        graphics.Restore(state);
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

    public static RectangleF ScaleFrameToFitVisualBounds(RectangleF frame, RectangleF contentBounds, SymbolFrameShape shape, IconGuideShape guideShape, float requestedScale)
    {
        requestedScale = Math.Max(1f, requestedScale);
        var visualBounds = GetFrameVisualBounds(frame, shape, guideShape);
        if (visualBounds.Width <= 0f || visualBounds.Height <= 0f)
            return frame;

        var maxScaleX = contentBounds.Width / visualBounds.Width;
        var maxScaleY = contentBounds.Height / visualBounds.Height;
        var scale = Math.Min(requestedScale, Math.Min(maxScaleX, maxScaleY));
        if (scale <= 1f)
            return frame;

        var center = new PointF(frame.Left + frame.Width / 2f, frame.Top + frame.Height / 2f);
        var scaled = new RectangleF(
            center.X - frame.Width * scale / 2f,
            center.Y - frame.Height * scale / 2f,
            frame.Width * scale,
            frame.Height * scale);
        var scaledVisual = GetFrameVisualBounds(scaled, shape, guideShape);
        var dx = GetContainmentOffset(scaledVisual.Left, scaledVisual.Right, contentBounds.Left, contentBounds.Right);
        var dy = GetContainmentOffset(scaledVisual.Top, scaledVisual.Bottom, contentBounds.Top, contentBounds.Bottom);
        scaled.Offset(dx, dy);
        return scaled;
    }

    public static RectangleF GetInteriorFrame(RectangleF frame, SymbolFrameShape shape, IconGuideShape guideShape)
    {
        return shape switch
        {
            SymbolFrameShape.Hostile => GetHostileInteriorFrame(frame, guideShape),
            SymbolFrameShape.Neutral => GetNeutralInteriorFrame(frame),
            SymbolFrameShape.Unknown => GetUnknownInteriorFrame(frame),
            SymbolFrameShape.FriendlyEquipment => GetEquipmentInteriorFrame(frame, guideShape),
            _ => GetFriendlyUnitInteriorFrame(frame)
        };
    }

    public static RectangleF GetGuideFrame(RectangleF frame, SymbolFrameShape shape, IconGuideShape guideShape)
    {
        return shape switch
        {
            SymbolFrameShape.Hostile => GetHostileGuideFrame(frame, guideShape),
            SymbolFrameShape.Neutral => GetNeutralGuideFrame(frame),
            SymbolFrameShape.Unknown => GetUnknownGuideFrame(frame),
            SymbolFrameShape.FriendlyEquipment => GetEquipmentGuideFrame(frame, guideShape),
            _ => frame
        };
    }

    public static RectangleF GetCommandFrame(RectangleF interiorFrame, SymbolFrameShape shape, SymbolDrawCommand command)
    {
        if (shape != SymbolFrameShape.Hostile || command.Kind != SymbolDrawCommandKind.Capsule)
            return interiorFrame;

        return ScaleRectangle(interiorFrame, 1.24f, 1f);
    }

    private static RectangleF GetNormalizedVisualBounds(SymbolFrameShape shape, IReadOnlyList<SymbolDrawCommand> commands, IconGuideShape guideShape)
    {
        using var path = CreatePath(new RectangleF(0f, 0f, 1f, 1f), shape, guideShape);
        var bounds = path.GetBounds();
        foreach (var command in commands)
            bounds = Union(bounds, command.GetNormalizedVisualBounds());

        return bounds;
    }

    private static RectangleF GetFrameVisualBounds(RectangleF frame, SymbolFrameShape shape, IconGuideShape guideShape)
    {
        using var path = CreatePath(frame, shape, guideShape);
        return path.GetBounds();
    }

    private static float GetContainmentOffset(float innerStart, float innerEnd, float outerStart, float outerEnd)
    {
        if (innerStart < outerStart)
            return outerStart - innerStart;
        if (innerEnd > outerEnd)
            return outerEnd - innerEnd;
        return 0f;
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

    private static RectangleF GetFriendlyUnitInteriorFrame(RectangleF frame) => frame;

    private static RectangleF ScaleRectangle(RectangleF rectangle, float scaleX, float scaleY)
    {
        var width = rectangle.Width * scaleX;
        var height = rectangle.Height * scaleY;
        return new RectangleF(
            rectangle.Left + (rectangle.Width - width) / 2f,
            rectangle.Top + (rectangle.Height - height) / 2f,
            width,
            height);
    }

    private static RectangleF GetHostileInteriorFrame(RectangleF frame, IconGuideShape guideShape)
    {
        var diamond = GetHostileDiamondPoints(frame, guideShape);
        var center = new PointF(frame.Left + frame.Width / 2f, frame.Top + frame.Height / 2f);
        var top = diamond.OrderBy(point => point.Y).First().Y;
        var right = diamond.OrderByDescending(point => point.X).First().X;
        var bottom = diamond.OrderByDescending(point => point.Y).First().Y;
        var left = diamond.OrderBy(point => point.X).First().X;

        var halfWidth = Math.Min(center.X - left, right - center.X) * 0.74f;
        var halfHeight = Math.Min(center.Y - top, bottom - center.Y) * 0.74f;
        return RectangleF.FromLTRB(center.X - halfWidth, center.Y - halfHeight, center.X + halfWidth, center.Y + halfHeight);
    }

    private static RectangleF GetHostileGuideFrame(RectangleF frame, IconGuideShape guideShape)
    {
        var diamond = GetHostileDiamondPoints(frame, guideShape);
        var center = new PointF(frame.Left + frame.Width / 2f, frame.Top + frame.Height / 2f);
        var top = diamond.OrderBy(point => point.Y).First().Y;
        var right = diamond.OrderByDescending(point => point.X).First().X;
        var bottom = diamond.OrderByDescending(point => point.Y).First().Y;
        var left = diamond.OrderBy(point => point.X).First().X;

        var halfWidth = Math.Min(center.X - left, right - center.X) * 0.72f;
        var halfHeight = Math.Min(center.Y - top, bottom - center.Y) * 0.72f;
        return RectangleF.FromLTRB(center.X - halfWidth, center.Y - halfHeight, center.X + halfWidth, center.Y + halfHeight);
    }

    private static RectangleF GetNeutralInteriorFrame(RectangleF frame)
    {
        var side = Math.Min(frame.Width, frame.Height);
        var square = new RectangleF(
            frame.Left + (frame.Width - side) / 2f,
            frame.Top + (frame.Height - side) / 2f,
            side,
            side);
        return RectangleF.Inflate(square, -square.Width * 0.06f, -square.Height * 0.06f);
    }

    private static RectangleF GetNeutralGuideFrame(RectangleF frame)
    {
        var side = Math.Min(frame.Width, frame.Height);
        return new RectangleF(
            frame.Left + (frame.Width - side) / 2f,
            frame.Top + (frame.Height - side) / 2f,
            side,
            side);
    }

    private static RectangleF GetUnknownInteriorFrame(RectangleF frame) =>
        RectangleF.FromLTRB(
            frame.Left + frame.Width * 0.22f,
            frame.Top + frame.Height * 0.22f,
            frame.Right - frame.Width * 0.22f,
            frame.Bottom - frame.Height * 0.22f);

    private static RectangleF GetUnknownGuideFrame(RectangleF frame) =>
        RectangleF.FromLTRB(
            frame.Left + frame.Width * 0.12f,
            frame.Top + frame.Height * 0.12f,
            frame.Right - frame.Width * 0.12f,
            frame.Bottom - frame.Height * 0.12f);

    private static RectangleF GetEquipmentInteriorFrame(RectangleF frame, IconGuideShape guideShape)
    {
        var circle = GetGuideCircumcircleBounds(frame, guideShape);
        var side = Math.Min(circle.Width, circle.Height) * 0.68f;
        return new RectangleF(
            circle.Left + (circle.Width - side) / 2f,
            circle.Top + (circle.Height - side) / 2f,
            side,
            side);
    }

    private static RectangleF GetEquipmentGuideFrame(RectangleF frame, IconGuideShape guideShape)
    {
        return frame;
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
        var guide = GetIconGuideBounds(frame);
        var center = new PointF(guide.Left + guide.Width / 2f, guide.Top + guide.Height / 2f);
        var radius = GetIconGuidePoints(guideShape)
            .Select(point => ToAbsolute(guide, point))
            .Select(point => Distance(center, point))
            .Max();
        return new RectangleF(center.X - radius, center.Y - radius, radius * 2f, radius * 2f);
    }

    private static RectangleF GetIconGuideBounds(RectangleF frame)
    {
        var size = Math.Min(frame.Width, frame.Height);
        return new RectangleF(
            frame.Left + (frame.Width - size) / 2f,
            frame.Top + (frame.Height - size) / 2f,
            size,
            size);
    }

    private static PointF[] GetHostileDiamondPoints(RectangleF frame, IconGuideShape guideShape)
    {
        var center = new PointF(frame.Left + frame.Width / 2f, frame.Top + frame.Height / 2f);
        var radius = Math.Min(frame.Width, frame.Height) / 2f;
        return new[]
        {
            new PointF(center.X, center.Y - radius),
            new PointF(center.X + radius, center.Y),
            new PointF(center.X, center.Y + radius),
            new PointF(center.X - radius, center.Y)
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
        const float center = 0.5f;
        if (shape == IconGuideShape.FlatTopBottom)
        {
            const float inset = 0.29289323f;
            return new[]
            {
                new PointF(inset, 0f),
                new PointF(1f - inset, 0f),
                new PointF(1f, inset),
                new PointF(1f, 1f - inset),
                new PointF(1f - inset, 1f),
                new PointF(inset, 1f),
                new PointF(0f, 1f - inset),
                new PointF(0f, inset)
            };
        }

        const float diagonalInset = 0.14644662f;
        return new[]
        {
            new PointF(center, 0f),
            new PointF(1f - diagonalInset, diagonalInset),
            new PointF(1f, center),
            new PointF(1f - diagonalInset, 1f - diagonalInset),
            new PointF(center, 1f),
            new PointF(diagonalInset, 1f - diagonalInset),
            new PointF(0f, center),
            new PointF(diagonalInset, diagonalInset)
        };
    }
}

internal readonly record struct SymbolPalette(Color Fill, Color Symbol);

internal sealed record SymbolCanvasSnapshot(List<SymbolDrawCommand> Commands, int SelectedIndex);

internal sealed class SymbolDrawCommand
{
    public SymbolDrawCommandKind Kind { get; set; }
    public SymbolPoint Start { get; set; }
    public SymbolPoint End { get; set; }
    public SymbolPoint Control1 { get; set; }
    public SymbolPoint Control2 { get; set; }
    public float Radius { get; set; }
    public float FontSize { get; set; } = 12f;
    public float StrokeWidth { get; set; } = 2f;
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
            StrokeWidth = StrokeWidth,
            Text = Text,
            Filled = Filled,
            Points = Points.Select(point => point).ToList()
        };

    public bool HasSameState(SymbolDrawCommand other) =>
        Kind == other.Kind
        && Start == other.Start
        && End == other.End
        && Control1 == other.Control1
        && Control2 == other.Control2
        && Radius.Equals(other.Radius)
        && FontSize.Equals(other.FontSize)
        && StrokeWidth.Equals(other.StrokeWidth)
        && Text == other.Text
        && Filled == other.Filled
        && Points.SequenceEqual(other.Points);

    public SymbolDrawCommand WithStrokeWidth(float strokeWidth)
    {
        if (UsesStroke)
            StrokeWidth = Math.Clamp(strokeWidth, 0.5f, 12f);
        return this;
    }

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
        using var commandPen = CreateStrokePen(pen);
        switch (Kind)
        {
            case SymbolDrawCommandKind.Line:
                graphics.DrawLine(commandPen, ToAbsolute(frame, Start), ToAbsolute(frame, End));
                break;
            case SymbolDrawCommandKind.Rectangle:
                if (Filled)
                    graphics.FillRectangle(brush, ToRectangle(frame));
                graphics.DrawRectangle(commandPen, System.Drawing.Rectangle.Round(ToRectangle(frame)));
                break;
            case SymbolDrawCommandKind.Ellipse:
                if (Filled)
                    graphics.FillEllipse(brush, ToRectangle(frame));
                graphics.DrawEllipse(commandPen, ToRectangle(frame));
                break;
            case SymbolDrawCommandKind.Circle:
                var circle = ToCircleRectangle(frame);
                if (Filled)
                    graphics.FillEllipse(brush, circle);
                graphics.DrawEllipse(commandPen, circle);
                break;
            case SymbolDrawCommandKind.Capsule:
                DrawCapsule(graphics, commandPen, brush, ToRectangle(frame), Filled);
                break;
            case SymbolDrawCommandKind.Path:
                using (var path = CreateGraphicsPath(frame))
                {
                    if (Filled)
                        graphics.FillPath(brush, path);
                    graphics.DrawPath(commandPen, path);
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
                    graphics.DrawArc(commandPen, arcBounds, 200f, 140f);
                break;
            case SymbolDrawCommandKind.Bezier:
                graphics.DrawBezier(commandPen, ToAbsolute(frame, Start), ToAbsolute(frame, Control1), ToAbsolute(frame, Control2), ToAbsolute(frame, End));
                break;
        }
    }

    private Pen CreateStrokePen(Pen basePen)
    {
        var selectedOffset = Math.Max(0f, basePen.Width - 2f);
        return new Pen(basePen.Color, Math.Max(0.5f, StrokeWidth + selectedOffset))
        {
            DashStyle = basePen.DashStyle,
            StartCap = basePen.StartCap,
            EndCap = basePen.EndCap,
            LineJoin = basePen.LineJoin
        };
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
        var strokePen = UsesStroke ? "strokePen" : pen;
        var commandCode = Kind switch
        {
            SymbolDrawCommandKind.Line =>
                $"{graphics}.DrawLine({strokePen}, {PointCode(bounds, Start)}, {PointCode(bounds, End)});",
            SymbolDrawCommandKind.Rectangle =>
                Filled
                    ? $"{graphics}.FillRectangle({brush}, {RectCode(bounds)});\r\n{graphics}.DrawRectangle({strokePen}, Rectangle.Round({RectCode(bounds)}));"
                    : $"{graphics}.DrawRectangle({strokePen}, Rectangle.Round({RectCode(bounds)}));",
            SymbolDrawCommandKind.Ellipse =>
                Filled
                    ? $"{graphics}.FillEllipse({brush}, {RectCode(bounds)});\r\n{graphics}.DrawEllipse({strokePen}, {RectCode(bounds)});"
                    : $"{graphics}.DrawEllipse({strokePen}, {RectCode(bounds)});",
            SymbolDrawCommandKind.Circle =>
                Filled
                    ? $"{{\r\n    var circleBounds = {CircleRectCode(bounds)};\r\n    {graphics}.FillEllipse({brush}, circleBounds);\r\n    {graphics}.DrawEllipse({strokePen}, circleBounds);\r\n}}"
                    : $"{graphics}.DrawEllipse({strokePen}, {CircleRectCode(bounds)});",
            SymbolDrawCommandKind.Capsule =>
                CapsuleCode(graphics, strokePen, brush, bounds),
            SymbolDrawCommandKind.Path =>
                PathCode(graphics, strokePen, brush, bounds),
            SymbolDrawCommandKind.Dot =>
                $"{graphics}.FillEllipse({brush}, {PointCode(bounds, Start)}.X - {RadiusCode(bounds)}, {PointCode(bounds, Start)}.Y - {RadiusCode(bounds)}, {RadiusCode(bounds)} * 2f, {RadiusCode(bounds)} * 2f);",
            SymbolDrawCommandKind.Text =>
                $"{{\r\n    var textValue = \"{EscapeCSharpString(Text)}\";\r\n    var textLocation = {PointCode(bounds, Start)};\r\n    var textSize = {bounds}.Height * {Format(FontSize / 100f)}f;\r\n    var textWidth = Math.Min({bounds}.Width, Math.Max({bounds}.Width * 0.45f, Math.Max(1, textValue.Length) * textSize * 1.15f));\r\n    var textHeight = textSize * 1.6f;\r\n    using var textFont = new Font(font.FontFamily, textSize, FontStyle.Bold, GraphicsUnit.Pixel);\r\n    using var textFormat = new StringFormat {{ Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap }};\r\n    {graphics}.DrawString(textValue, textFont, {brush}, new RectangleF(textLocation.X - textWidth / 2f, textLocation.Y - textHeight / 2f, textWidth, textHeight), textFormat);\r\n}}",
            SymbolDrawCommandKind.Arc =>
                $"{graphics}.DrawArc({strokePen}, {RectCode(bounds)}, 200f, 140f);",
            SymbolDrawCommandKind.Bezier =>
                $"{graphics}.DrawBezier({strokePen}, {PointCode(bounds, Start)}, {PointCode(bounds, Control1)}, {PointCode(bounds, Control2)}, {PointCode(bounds, End)});",
            _ => string.Empty
        };

        if (!UsesStroke || Math.Abs(StrokeWidth - 2f) < 0.001f)
            return commandCode;

        return $"{{\r\n    using var strokePen = (Pen){pen}.Clone();\r\n    strokePen.Width = {Format(StrokeWidth)}f;\r\n    {IndentCode(commandCode, 4)}\r\n}}";
    }

    [JsonIgnore]
    public bool UsesStroke => Kind is not SymbolDrawCommandKind.Dot and not SymbolDrawCommandKind.Text;

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

    private static string IndentCode(string code, int spaces)
    {
        var indent = new string(' ', spaces);
        return string.Join("\r\n", code.Split(["\r\n", "\n"], StringSplitOptions.None).Select(line => indent + line));
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
            Components.OrbatUnitType.Aviation => new[]
            {
                SymbolDrawCommand.Path(new[]
                {
                    new SymbolPoint(0.12f, 0.5f),
                    new SymbolPoint(0.88f, 0.18f),
                    new SymbolPoint(0.88f, 0.82f)
                }, filled: true)
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
            Components.OrbatUnitType.CBRN => new[]
            {
                SymbolDrawCommand.TextCommand(new PointF(0.5f, 0.5f), "CBRN", 18f)
            },
            Components.OrbatUnitType.Ordnance => new[]
            {
                SymbolDrawCommand.TextCommand(new PointF(0.5f, 0.5f), "ORD", 18f)
            },
            Components.OrbatUnitType.Quartermaster => new[]
            {
                SymbolDrawCommand.TextCommand(new PointF(0.5f, 0.5f), "QM", 18f)
            },
            _ => Array.Empty<SymbolDrawCommand>()
        };
    }
}
