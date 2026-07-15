using System.Drawing.Drawing2D;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrgHierarchy.Components;

namespace OrgHierarchy.Demo;

public sealed class SymbolDesignerForm : Form
{
    private readonly SymbolDesignerCanvas _canvas = new();
    private readonly SymbolPreviewControl _preview = new();
    private readonly ComboBox _toolComboBox = new() { Width = 156, DropDownWidth = 190 };
    private readonly ComboBox _unitTypeComboBox = new() { Width = 150, DropDownWidth = 190 };
    private readonly ComboBox _equipmentCategoryComboBox = new() { Width = 180, DropDownWidth = 230 };
    private readonly ComboBox _equipmentFunctionComboBox = new() { Width = 340, DropDownWidth = 440 };
    private readonly ComboBox _equipmentVariantComboBox = new() { Width = 300, DropDownWidth = 440, DropDownStyle = ComboBoxStyle.DropDown };
    private readonly ComboBox _equipmentOperatingStateComboBox = new() { Width = 130, DropDownWidth = 170 };
    private readonly ComboBox _symbolRoleComboBox = new() { Width = 145, DropDownWidth = 190 };
    private readonly ComboBox _compositionModeComboBox = new() { Width = 130, DropDownWidth = 170 };
    private readonly ComboBox _modifier1TypeComboBox = new() { Width = 260, DropDownWidth = 340 };
    private readonly ComboBox _modifier2TypeComboBox = new() { Width = 280, DropDownWidth = 360 };
    private readonly ComboBox _landUnitModifier1TypeComboBox = new() { Width = 260, DropDownWidth = 340 };
    private readonly ComboBox _landUnitModifier2TypeComboBox = new() { Width = 280, DropDownWidth = 360 };
    private readonly ComboBox _mobilityTypeComboBox = new() { Width = 300, DropDownWidth = 400 };
    private readonly ComboBox _affiliationComboBox = new() { Width = 110 };
    private readonly ComboBox _physicalDomainComboBox = new() { Width = 115 };
    private readonly ComboBox _frameStatusComboBox = new() { Width = 110 };
    private readonly TrackBar _referenceOpacityTrackBar = new();
    private readonly CheckBox _showGridCheckBox = new() { Text = "Grid", Checked = true, AutoSize = true };
    private readonly CheckBox _showIconGuideCheckBox = new() { Text = "Icon guide", Checked = true, AutoSize = true };
    private readonly ComboBox _iconGuideShapeComboBox = new() { Width = 160, DropDownWidth = 190 };
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
    private readonly Dictionary<SymbolDesignerTool, Button> _toolButtons = new();
    private readonly FlowLayoutPanel _contextOptionsPanel = new();
    private Control? _unitTypeField;
    private Control? _equipmentCategoryField;
    private Control? _equipmentFunctionField;
    private Control? _equipmentVariantField;
    private Control? _equipmentOperatingStateField;
    private Control? _symbolRoleField;
    private Control? _compositionModeField;
    private Control? _modifier1TypeField;
    private Control? _modifier2TypeField;
    private Control? _landUnitModifier1TypeField;
    private Control? _landUnitModifier2TypeField;
    private Control? _mobilityTypeField;
    private Control? _textOptionsField;
    private Control? _fillOptionsField;
    private Control? _rotateOptionsField;
    private bool _updatingSelectionControls;
    private bool _updatingEquipmentVariantOptions;
    private bool _updatingEquipmentFunctionOptions;

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

        _equipmentOperatingStateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _equipmentOperatingStateComboBox.Items.AddRange(Enum.GetNames<OrbatEquipmentOperatingState>().Cast<object>().ToArray());
        _equipmentOperatingStateComboBox.SelectedItem = OrbatEquipmentOperatingState.Ground.ToString();
        _equipmentOperatingStateComboBox.SelectedIndexChanged += (_, _) =>
        {
            _canvas.FrameShape = GetSelectedFrameShape();
            RefreshOutput();
            _canvas.Invalidate();
        };

        _equipmentCategoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _equipmentCategoryComboBox.Items.AddRange(Enum.GetValues<OrbatEquipmentFunctionCategory>()
            .Select(value => new EquipmentFunctionCategorySelection(value)).Cast<object>().ToArray());
        _equipmentCategoryComboBox.SelectedIndex = 0;
        _equipmentCategoryComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (!_updatingEquipmentFunctionOptions)
                RefreshEquipmentFunctionOptions();
        };

        _equipmentFunctionComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        _equipmentFunctionComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _equipmentFunctionComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        RefreshEquipmentFunctionOptions(Components.OrbatEquipmentFunction.Unspecified);
        _equipmentFunctionComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingEquipmentFunctionOptions)
                return;

            RefreshEquipmentVariantOptions(selectFirstWhenNoMatch: true);
            ApplyDefaultEquipmentOperatingState();
            UpdateFunctionSelectorState();
            RefreshOutput();
        };
        _equipmentVariantComboBox.TextChanged += (_, _) =>
        {
            if (!_updatingEquipmentVariantOptions)
                RefreshOutput();
        };

        _symbolRoleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _symbolRoleComboBox.Items.AddRange(Enum.GetNames<OrbatEquipmentSymbolRole>().Cast<object>().ToArray());
        _symbolRoleComboBox.SelectedItem = OrbatEquipmentSymbolRole.MainFunction.ToString();
        _symbolRoleComboBox.SelectedIndexChanged += (_, _) =>
        {
            UpdateFunctionSelectorState();
            RefreshOutput();
        };

        _compositionModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _compositionModeComboBox.Items.AddRange(Enum.GetNames<OrbatEquipmentCompositionMode>().Cast<object>().ToArray());
        _compositionModeComboBox.SelectedItem = OrbatEquipmentCompositionMode.Composable.ToString();
        _compositionModeComboBox.SelectedIndexChanged += (_, _) => RefreshOutput();

        _modifier1TypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _modifier1TypeComboBox.Items.AddRange(Enum.GetValues<OrbatEquipmentModifier1>()
            .Select(value => new EquipmentModifier1Selection(value)).Cast<object>().ToArray());
        _modifier1TypeComboBox.SelectedIndex = 0;
        _modifier1TypeComboBox.SelectedIndexChanged += (_, _) => RefreshOutput();

        _modifier2TypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _modifier2TypeComboBox.Items.AddRange(Enum.GetValues<OrbatEquipmentModifier2>()
            .Select(value => new EquipmentModifier2Selection(value)).Cast<object>().ToArray());
        _modifier2TypeComboBox.SelectedIndex = 0;
        _modifier2TypeComboBox.SelectedIndexChanged += (_, _) => RefreshOutput();

        _landUnitModifier1TypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _landUnitModifier1TypeComboBox.Items.AddRange(Enum.GetValues<OrbatLandUnitModifier1>()
            .Select(value => new LandUnitModifier1Selection(value)).Cast<object>().ToArray());
        _landUnitModifier1TypeComboBox.SelectedIndex = 0;
        _landUnitModifier1TypeComboBox.SelectedIndexChanged += (_, _) => RefreshOutput();

        _landUnitModifier2TypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _landUnitModifier2TypeComboBox.Items.AddRange(Enum.GetValues<OrbatLandUnitModifier2>()
            .Select(value => new LandUnitModifier2Selection(value)).Cast<object>().ToArray());
        _landUnitModifier2TypeComboBox.SelectedIndex = 0;
        _landUnitModifier2TypeComboBox.SelectedIndexChanged += (_, _) => RefreshOutput();

        _mobilityTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _mobilityTypeComboBox.Items.AddRange(Enum.GetValues<OrbatEquipmentMobilityMode>()
            .Select(value => new EquipmentMobilitySelection(value)).Cast<object>().ToArray());
        _mobilityTypeComboBox.SelectedIndex = 0;
        _mobilityTypeComboBox.SelectedIndexChanged += (_, _) => RefreshOutput();

        _affiliationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _affiliationComboBox.Items.AddRange(Enum.GetNames<SymbolAffiliation>().Cast<object>().ToArray());
        _affiliationComboBox.SelectedItem = SymbolAffiliation.Friendly.ToString();
        _affiliationComboBox.SelectedIndexChanged += (_, _) =>
        {
            _canvas.FrameShape = GetSelectedFrameShape();
            RefreshOutput();
            _canvas.Invalidate();
        };

        _physicalDomainComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _physicalDomainComboBox.Items.AddRange(Enum.GetNames<SymbolPhysicalDomain>().Cast<object>().ToArray());
        _physicalDomainComboBox.SelectedItem = SymbolPhysicalDomain.LandUnit.ToString();
        _physicalDomainComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (GetSelectedPhysicalDomain() == SymbolPhysicalDomain.Equipment
                && !_equipmentFunctionComboBox.Enabled)
            {
                _symbolRoleComboBox.SelectedItem = OrbatEquipmentSymbolRole.MainFunction.ToString();
                _compositionModeComboBox.SelectedItem = OrbatEquipmentCompositionMode.Composable.ToString();
            }

            _canvas.FrameShape = GetSelectedFrameShape();
            UpdateFunctionSelectorState();
            if (GetSelectedPhysicalDomain() == SymbolPhysicalDomain.Equipment)
                RefreshEquipmentVariantOptions();
            RefreshOutput();
            _canvas.Invalidate();
        };

        _frameStatusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
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

        var rotateAngleInput = new NumericUpDown
        {
            Minimum = -360m,
            Maximum = 360m,
            DecimalPlaces = 1,
            Increment = 1m,
            Value = 15m,
            Width = 64
        };
        var menu = CreateMainMenu(rotateAngleInput);
        MainMenuStrip = menu;

        var metadataBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8, 6, 8, 3),
            WrapContents = true,
            BackColor = SystemColors.ControlLight
        };
        metadataBar.Controls.Add(CreateLabeledField("Domain", _physicalDomainComboBox, leadingMargin: 0));
        _unitTypeField = CreateLabeledField("Unit type", _unitTypeComboBox);
        _equipmentCategoryField = CreateLabeledField("Category", _equipmentCategoryComboBox);
        _equipmentFunctionField = CreateLabeledField("Equipment function", _equipmentFunctionComboBox);
        _equipmentVariantField = CreateLabeledField("Variant", _equipmentVariantComboBox);
        _equipmentOperatingStateField = CreateLabeledField("Operating state", _equipmentOperatingStateComboBox);
        _symbolRoleField = CreateLabeledField("Symbol role", _symbolRoleComboBox);
        _compositionModeField = CreateLabeledField("Composition", _compositionModeComboBox);
        _modifier1TypeField = CreateLabeledField("Modifier 1", _modifier1TypeComboBox);
        _modifier2TypeField = CreateLabeledField("Modifier 2", _modifier2TypeComboBox);
        _landUnitModifier1TypeField = CreateLabeledField("Modifier 1", _landUnitModifier1TypeComboBox);
        _landUnitModifier2TypeField = CreateLabeledField("Modifier 2", _landUnitModifier2TypeComboBox);
        _mobilityTypeField = CreateLabeledField("Mobility", _mobilityTypeComboBox);
        metadataBar.Controls.Add(_unitTypeField);
        metadataBar.Controls.Add(_equipmentCategoryField);
        metadataBar.Controls.Add(_equipmentFunctionField);
        metadataBar.Controls.Add(_equipmentVariantField);
        metadataBar.Controls.Add(_equipmentOperatingStateField);
        metadataBar.Controls.Add(_symbolRoleField);
        metadataBar.Controls.Add(_compositionModeField);
        metadataBar.Controls.Add(_modifier1TypeField);
        metadataBar.Controls.Add(_modifier2TypeField);
        metadataBar.Controls.Add(_landUnitModifier1TypeField);
        metadataBar.Controls.Add(_landUnitModifier2TypeField);
        metadataBar.Controls.Add(_mobilityTypeField);
        metadataBar.Controls.Add(CreateLabeledField("Affiliation", _affiliationComboBox));
        metadataBar.Controls.Add(CreateLabeledField("Status", _frameStatusComboBox));

        ConfigureContextOptions(rotateAngleInput);
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
        var propertiesTab = new TabPage("Properties") { Padding = new Padding(8) };
        var previewTab = new TabPage("Preview") { Padding = new Padding(8) };
        var commandsTab = new TabPage("Commands") { Padding = new Padding(8) };
        var codeTab = new TabPage("C# code") { Padding = new Padding(8) };
        propertiesTab.Controls.Add(CreateSelectionEditor());
        previewTab.Controls.Add(_preview);
        commandsTab.Controls.Add(_commandListBox);
        codeTab.Controls.Add(_codeTextBox);
        rightTabs.Controls.Add(propertiesTab);
        rightTabs.Controls.Add(previewTab);
        rightTabs.Controls.Add(commandsTab);
        rightTabs.Controls.Add(codeTab);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2
        };
        split.Panel1.Controls.Add(_canvas);
        split.Panel2.Controls.Add(rightTabs);

        var workspace = new Panel { Dock = DockStyle.Fill };
        workspace.Controls.Add(split);
        workspace.Controls.Add(CreateToolbox());

        Controls.Add(workspace);
        Controls.Add(statusPanel);
        Controls.Add(_contextOptionsPanel);
        Controls.Add(metadataBar);
        Controls.Add(menu);

        Shown += (_, _) => ConfigureInitialSplitterLayout(split);

        UpdateFunctionSelectorState();
        RefreshOutput();
        RefreshSelectionControls();
        UpdateToolStatus();
    }

    public SymbolDesignerForm(string libraryFileName)
        : this()
    {
        LoadLibraryFile(libraryFileName);
    }

    private static void ConfigureInitialSplitterLayout(SplitContainer split)
    {
        var width = split.ClientSize.Width;
        if (width <= split.SplitterWidth + 50)
            return;

        var panel2Width = Math.Min(360, Math.Max(260, width / 3));
        var distance = Math.Clamp(
            width - panel2Width - split.SplitterWidth,
            25,
            width - split.SplitterWidth - 25);
        split.SplitterDistance = distance;
        split.Panel1MinSize = Math.Min(480, distance);
        split.Panel2MinSize = Math.Min(340, width - distance - split.SplitterWidth);
    }
    private MenuStrip CreateMainMenu(NumericUpDown rotateAngleInput)
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };
        var file = new ToolStripMenuItem("File");
        file.DropDownItems.Add(CreateMenuItem("Load reference...", LoadReferenceImage));
        file.DropDownItems.Add(CreateMenuItem("Load from clipboard", LoadReferenceFromClipboard));
        file.DropDownItems.Add(CreateMenuItem("Reset reference", () => _canvas.ResetReferenceTransform()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateMenuItem("Load base symbol...", LoadBaseSymbol));
        file.DropDownItems.Add(CreateMenuItem("Load library...", LoadLibrary));
        file.DropDownItems.Add(CreateMenuItem("Import drawing from library...", ImportDrawingFromLibrary));
        file.DropDownItems.Add(CreateMenuItem("Save to library...", SaveLibrary, Keys.Control | Keys.S));
        file.DropDownItems.Add(CreateMenuItem("View library", ViewLibrary));

        var edit = new ToolStripMenuItem("Edit");
        edit.DropDownItems.Add(CreateMenuItem("Undo", () => _canvas.Undo(), Keys.Control | Keys.Z));
        edit.DropDownItems.Add(CreateMenuItem("Redo", () => _canvas.Redo(), Keys.Control | Keys.Y));
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(CreateMenuItem("Duplicate", () => _canvas.DuplicateSelected(), Keys.Control | Keys.D));
        edit.DropDownItems.Add(CreateMenuItem("Copy", () => _canvas.CopySelected(), Keys.Control | Keys.C));
        edit.DropDownItems.Add(CreateMenuItem("Paste", () => _canvas.PasteCopied(), Keys.Control | Keys.V));
        edit.DropDownItems.Add(CreateMenuItem("Delete", DeleteSelectedCommand, Keys.Delete));
        edit.DropDownItems.Add(CreateMenuItem("Clear canvas", () => _canvas.ClearCanvas()));

        var arrange = new ToolStripMenuItem("Arrange");
        arrange.DropDownItems.Add(CreateMenuItem("Rotate 90 degrees", () => _canvas.RotateSelectedClockwise()));
        arrange.DropDownItems.Add(CreateMenuItem("Rotate by angle", () => _canvas.RotateSelected((float)rotateAngleInput.Value)));
        arrange.DropDownItems.Add(CreateMenuItem("Mirror horizontally", () => _canvas.MirrorSelectedHorizontal()));
        arrange.DropDownItems.Add(CreateMenuItem("Mirror vertically", () => _canvas.MirrorSelectedVertical()));
        arrange.DropDownItems.Add(new ToolStripSeparator());
        arrange.DropDownItems.Add(CreateMenuItem("Group", () => _canvas.GroupSelected(), Keys.Control | Keys.G));
        arrange.DropDownItems.Add(CreateMenuItem("Ungroup", () => _canvas.UngroupSelected(), Keys.Control | Keys.Shift | Keys.G));
        arrange.DropDownItems.Add(CreateMenuItem("Join lines", () => _canvas.JoinSelectedLines()));
        arrange.DropDownItems.Add(new ToolStripSeparator());
        arrange.DropDownItems.Add(CreateMenuItem("Align top", () => _canvas.AlignSelectedTop()));
        arrange.DropDownItems.Add(CreateMenuItem("Align bottom", () => _canvas.AlignSelectedBottom()));
        arrange.DropDownItems.Add(CreateMenuItem("Size to smallest", () => _canvas.SizeSelectedToSmallest()));
        arrange.DropDownItems.Add(CreateMenuItem("Size to largest", () => _canvas.SizeSelectedToLargest()));
        arrange.DropDownItems.Add(CreateMenuItem("Fit content to frame", () => _canvas.FitContentToFrame()));

        var draw = new ToolStripMenuItem("Draw");
        draw.DropDownItems.Add(CreateMenuItem("Close line path", CloseLinePath));
        draw.DropDownItems.Add(CreateMenuItem("Add air defense arc", AddAirDefenseArc));

        var view = new ToolStripMenuItem("View");
        view.DropDownItems.Add(CreateToggleMenuItem("Grid", _showGridCheckBox));
        view.DropDownItems.Add(CreateToggleMenuItem("Icon guide", _showIconGuideCheckBox));
        view.DropDownItems.Add(CreateToggleMenuItem("Snap", _snapCheckBox));

        var export = new ToolStripMenuItem("Export");
        export.DropDownItems.Add(CreateMenuItem("Copy C# code", CopyCode));

        menu.Items.AddRange(new ToolStripItem[] { file, edit, arrange, draw, view, export });
        return menu;
    }

    private static ToolStripMenuItem CreateMenuItem(string text, Action action, Keys shortcut = Keys.None)
    {
        var item = new ToolStripMenuItem(text) { ShortcutKeys = shortcut };
        item.Click += (_, _) => action();
        return item;
    }

    private static ToolStripMenuItem CreateToggleMenuItem(string text, CheckBox checkBox)
    {
        var item = new ToolStripMenuItem(text) { Checked = checkBox.Checked, CheckOnClick = true };
        item.CheckedChanged += (_, _) => checkBox.Checked = item.Checked;
        checkBox.CheckedChanged += (_, _) => item.Checked = checkBox.Checked;
        return item;
    }

    private static Control CreateLabeledField(string label, Control control, int leadingMargin = 8)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(leadingMargin, 0, 0, 0)
        };
        panel.Controls.Add(new Label { AutoSize = true, Text = label, Margin = new Padding(0, 6, 4, 0) });
        panel.Controls.Add(control);
        return panel;
    }

    private void ConfigureContextOptions(NumericUpDown rotateAngleInput)
    {
        _contextOptionsPanel.Dock = DockStyle.Top;
        _contextOptionsPanel.Height = 50;
        _contextOptionsPanel.AutoScroll = true;
        _contextOptionsPanel.WrapContents = false;
        _contextOptionsPanel.Padding = new Padding(8, 4, 8, 2);

        _contextOptionsPanel.Controls.Add(CreateLabeledField("Tool", _toolComboBox, leadingMargin: 0));
        _contextOptionsPanel.Controls.Add(CreateLabeledField("Stroke", _drawStrokeWidthInput));
        _textOptionsField = CreateLabeledField("Text", _drawTextInput);
        var textSizeField = CreateLabeledField("Size %", _drawTextSizeInput, leadingMargin: 4);
        ((FlowLayoutPanel)_textOptionsField).Controls.Add(textSizeField);
        _contextOptionsPanel.Controls.Add(_textOptionsField);
        _fillOptionsField = CreateLabeledField(string.Empty, _fillCheckBox);
        _contextOptionsPanel.Controls.Add(_fillOptionsField);
        _rotateOptionsField = CreateLabeledField("Angle", rotateAngleInput);
        ((FlowLayoutPanel)_rotateOptionsField).Controls.Add(CreateButton("Rotate", () => _canvas.RotateSelected((float)rotateAngleInput.Value)));
        _contextOptionsPanel.Controls.Add(_rotateOptionsField);
        _contextOptionsPanel.Controls.Add(CreateLabeledField(string.Empty, _snapCheckBox));
        _contextOptionsPanel.Controls.Add(CreateLabeledField(string.Empty, _showGridCheckBox));
        _contextOptionsPanel.Controls.Add(CreateLabeledField("Grid", _gridDivisionsInput, leadingMargin: 2));
        _contextOptionsPanel.Controls.Add(CreateLabeledField(string.Empty, _showIconGuideCheckBox));
        _contextOptionsPanel.Controls.Add(CreateLabeledField("Guide", _iconGuideShapeComboBox, leadingMargin: 2));
        _contextOptionsPanel.Controls.Add(CreateLabeledField("Reference", _referenceOpacityTrackBar));
    }

    private Control CreateToolbox()
    {
        var toolbox = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 144,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8, 8, 8, 8),
            BackColor = SystemColors.ControlLight
        };
        AddToolGroup(toolbox, "SELECT", (SymbolDesignerTool.SelectMove, "Select / Move"));
        AddToolGroup(toolbox, "LINES",
            (SymbolDesignerTool.Line, "Line"),
            (SymbolDesignerTool.ParallelLine, "Parallel"),
            (SymbolDesignerTool.PerpendicularLine, "Perpendicular"));
        AddToolGroup(toolbox, "SHAPES",
            (SymbolDesignerTool.Rectangle, "Rectangle"),
            (SymbolDesignerTool.Ellipse, "Ellipse"),
            (SymbolDesignerTool.Circle, "Circle"),
            (SymbolDesignerTool.Capsule, "Capsule"));
        AddToolGroup(toolbox, "CURVES",
            (SymbolDesignerTool.Arc, "Arc"),
            (SymbolDesignerTool.BezierArc, "Bezier"),
            (SymbolDesignerTool.SineWave, "Wave"));
        AddToolGroup(toolbox, "CONTENT",
            (SymbolDesignerTool.Dot, "Dot"),
            (SymbolDesignerTool.Text, "Text"));
        return toolbox;
    }

    private void AddToolGroup(FlowLayoutPanel toolbox, string title, params (SymbolDesignerTool Tool, string Label)[] tools)
    {
        toolbox.Controls.Add(new Label
        {
            Text = title,
            AutoSize = false,
            Width = 122,
            Height = 22,
            ForeColor = SystemColors.GrayText,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(2, 7, 2, 2)
        });
        foreach (var entry in tools)
        {
            var button = new Button
            {
                Text = entry.Label,
                Width = 122,
                Height = 29,
                Margin = new Padding(2, 1, 2, 1),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                UseVisualStyleBackColor = false
            };
            button.Click += (_, _) => SelectTool(entry.Tool);
            _toolButtons[entry.Tool] = button;
            toolbox.Controls.Add(button);
        }
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
            Maximum = 160m,
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
        return SymbolFrameMapping.GetFrameShape(
            GetSelectedAffiliation(),
            GetSelectedPhysicalDomain(),
            GetSelectedEquipmentOperatingState());
    }

    private OrbatEquipmentOperatingState GetSelectedEquipmentOperatingState() =>
        Enum.TryParse(Convert.ToString(_equipmentOperatingStateComboBox.SelectedItem), out OrbatEquipmentOperatingState state)
            ? state
            : OrbatEquipmentOperatingState.Ground;

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

    private Components.OrbatEquipmentFunction GetSelectedEquipmentFunction()
    {
        if (_equipmentFunctionComboBox.SelectedItem is EquipmentFunctionSelection selection)
            return selection.Value;

        return OrbatEquipmentFunctionCatalog.TryParseDisplayName(
            _equipmentFunctionComboBox.Text,
            out Components.OrbatEquipmentFunction function)
            ? function
            : Components.OrbatEquipmentFunction.Unspecified;
    }

    private OrbatEquipmentFunctionCategory GetSelectedEquipmentCategory() =>
        _equipmentCategoryComboBox.SelectedItem is EquipmentFunctionCategorySelection selection
            ? selection.Value
            : OrbatEquipmentFunctionCategory.All;

    private void RefreshEquipmentFunctionOptions(Components.OrbatEquipmentFunction? preferredFunction = null)
    {
        var currentFunction = preferredFunction ?? GetSelectedEquipmentFunction();
        var functions = OrbatEquipmentFunctionCatalog.GetFunctions(GetSelectedEquipmentCategory());
        var selectedFunction = functions.Contains(currentFunction)
            ? currentFunction
            : functions.FirstOrDefault();

        _updatingEquipmentFunctionOptions = true;
        try
        {
            _equipmentFunctionComboBox.BeginUpdate();
            _equipmentFunctionComboBox.Items.Clear();
            _equipmentFunctionComboBox.Items.AddRange(functions
                .Select(value => new EquipmentFunctionSelection(value)).Cast<object>().ToArray());
            _equipmentFunctionComboBox.SelectedItem = _equipmentFunctionComboBox.Items
                .Cast<EquipmentFunctionSelection>()
                .FirstOrDefault(item => item.Value == selectedFunction);
        }
        finally
        {
            _equipmentFunctionComboBox.EndUpdate();
            _updatingEquipmentFunctionOptions = false;
        }

        RefreshEquipmentVariantOptions(selectFirstWhenNoMatch: true);
        UpdateFunctionSelectorState();
        RefreshOutput();
    }

    private void SelectEquipmentFunction(Components.OrbatEquipmentFunction function)
    {
        var category = OrbatEquipmentFunctionCatalog.GetCategory(function);
        _updatingEquipmentFunctionOptions = true;
        try
        {
            _equipmentCategoryComboBox.SelectedItem = _equipmentCategoryComboBox.Items
                .Cast<EquipmentFunctionCategorySelection>()
                .FirstOrDefault(item => item.Value == category);
        }
        finally
        {
            _updatingEquipmentFunctionOptions = false;
        }

        RefreshEquipmentFunctionOptions(function);
    }

    private string GetSelectedEquipmentVariant() =>
        _equipmentVariantComboBox.SelectedItem is EquipmentVariantSelection selection
            ? selection.Variant
            : _equipmentVariantComboBox.Text.Trim();

    private void RefreshEquipmentVariantOptions(bool selectFirstWhenNoMatch = false)
    {
        var currentVariant = GetSelectedEquipmentVariant();
        var currentFunction = GetSelectedEquipmentFunction().ToString();
        var options = LoadEquipmentVariantOptions();
        var selectedOption = options.FirstOrDefault(option =>
            option.Function.Equals(currentFunction, StringComparison.OrdinalIgnoreCase)
            && NormalizeEquipmentVariant(option.Variant).Equals(
                NormalizeEquipmentVariant(currentVariant),
                StringComparison.OrdinalIgnoreCase));

        if (selectedOption == null && selectFirstWhenNoMatch)
        {
            selectedOption = options.FirstOrDefault(option =>
                option.Function.Equals(currentFunction, StringComparison.OrdinalIgnoreCase));
        }

        _updatingEquipmentVariantOptions = true;
        try
        {
            _equipmentVariantComboBox.BeginUpdate();
            _equipmentVariantComboBox.Items.Clear();
            _equipmentVariantComboBox.Items.AddRange(options.Cast<object>().ToArray());
            if (selectedOption != null)
                _equipmentVariantComboBox.SelectedItem = selectedOption;
            else
            {
                _equipmentVariantComboBox.SelectedIndex = -1;
                _equipmentVariantComboBox.Text = selectFirstWhenNoMatch ? string.Empty : currentVariant;
            }
        }
        finally
        {
            _equipmentVariantComboBox.EndUpdate();
            _updatingEquipmentVariantOptions = false;
        }
    }

    private static List<EquipmentVariantSelection> LoadEquipmentVariantOptions()
    {
        var variants = new Dictionary<string, EquipmentVariantSelection>(StringComparer.OrdinalIgnoreCase);
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        foreach (var file in GetRecentLibraryFiles().OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(
                    File.ReadAllText(file, Encoding.UTF8),
                    jsonOptions);
                if (definition == null
                    || definition.GetEffectivePhysicalDomain() != SymbolPhysicalDomain.Equipment
                    || definition.SymbolRole is OrbatEquipmentSymbolRole.Modifier1 or OrbatEquipmentSymbolRole.Modifier2
                    || string.IsNullOrWhiteSpace(definition.EquipmentFunction)
                    || string.IsNullOrWhiteSpace(definition.Variant))
                    continue;

                var function = definition.EquipmentFunction.Trim();
                var variant = definition.Variant.Trim();
                var key = $"{NormalizeEquipmentVariant(function)}|{NormalizeEquipmentVariant(variant)}";
                if (key.Length > 1)
                    variants.TryAdd(key, new EquipmentVariantSelection(function, variant));
            }
            catch
            {
                // Keep the designer usable when one library file cannot be read.
            }
        }

        return variants.Values
            .OrderBy(option => option.Function, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(option => option.Variant, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> GetRecentLibraryFiles()
    {
        var settingsFileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OrgHierarchy.Demo",
            "symbol-library-viewer.json");
        try
        {
            if (!File.Exists(settingsFileName))
                return Array.Empty<string>();

            var settings = JsonSerializer.Deserialize<DesignerLibrarySettings>(
                File.ReadAllText(settingsFileName, Encoding.UTF8));
            if (settings == null)
                return Array.Empty<string>();

            if (settings.Mode == 1)
                return settings.Files.Where(File.Exists).ToArray();

            return Directory.Exists(settings.Folder)
                ? Directory.EnumerateFiles(settings.Folder, "*.orbatsymbol.json", SearchOption.TopDirectoryOnly).ToArray()
                : Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string NormalizeEquipmentVariant(string? value) =>
        string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit));

    private OrbatEquipmentSymbolRole GetSelectedSymbolRole() =>
        Enum.TryParse(Convert.ToString(_symbolRoleComboBox.SelectedItem), out OrbatEquipmentSymbolRole role)
            ? role
            : OrbatEquipmentSymbolRole.MainFunction;

    private OrbatEquipmentCompositionMode GetSelectedCompositionMode() =>
        Enum.TryParse(Convert.ToString(_compositionModeComboBox.SelectedItem), out OrbatEquipmentCompositionMode mode)
            ? mode
            : OrbatEquipmentCompositionMode.Composable;

    private OrbatEquipmentModifier1 GetSelectedModifier1Type() =>
        _modifier1TypeComboBox.SelectedItem is EquipmentModifier1Selection selection
            ? selection.Value
            : OrbatEquipmentModifier1.Unspecified;

    private OrbatEquipmentModifier2 GetSelectedModifier2Type() =>
        _modifier2TypeComboBox.SelectedItem is EquipmentModifier2Selection selection
            ? selection.Value
            : OrbatEquipmentModifier2.Unspecified;

    private OrbatLandUnitModifier1 GetSelectedLandUnitModifier1Type() =>
        _landUnitModifier1TypeComboBox.SelectedItem is LandUnitModifier1Selection selection
            ? selection.Value
            : OrbatLandUnitModifier1.Unspecified;

    private OrbatLandUnitModifier2 GetSelectedLandUnitModifier2Type() =>
        _landUnitModifier2TypeComboBox.SelectedItem is LandUnitModifier2Selection selection
            ? selection.Value
            : OrbatLandUnitModifier2.Unspecified;

    private OrbatEquipmentMobilityMode GetSelectedMobilityType() =>
        _mobilityTypeComboBox.SelectedItem is EquipmentMobilitySelection selection
            ? selection.Value
            : OrbatEquipmentMobilityMode.Unspecified;

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

    private void ApplyDefaultEquipmentOperatingState()
    {
        var defaultState = OrbatEquipmentFunctionCatalog.GetDefaultOperatingState(GetSelectedEquipmentFunction());
        if (defaultState != OrbatEquipmentOperatingState.Ground)
            _equipmentOperatingStateComboBox.SelectedItem = defaultState.ToString();
    }

    private void UpdateFunctionSelectorState()
    {
        var equipment = GetSelectedPhysicalDomain() == SymbolPhysicalDomain.Equipment;
        var role = GetSelectedSymbolRole();
        if (!equipment && role == OrbatEquipmentSymbolRole.MobilityIndicator)
        {
            _symbolRoleComboBox.SelectedItem = OrbatEquipmentSymbolRole.MainFunction.ToString();
            role = OrbatEquipmentSymbolRole.MainFunction;
        }

        var supportsInFlight = equipment
            && OrbatEquipmentFunctionCatalog.SupportsInFlightOperatingState(GetSelectedEquipmentFunction());

        SetFieldVisible(_unitTypeField, !equipment);
        SetFieldVisible(_equipmentCategoryField, equipment);
        SetFieldVisible(_equipmentFunctionField, equipment);
        SetFieldVisible(_equipmentVariantField, equipment);
        SetFieldVisible(_equipmentOperatingStateField, supportsInFlight);
        SetFieldVisible(_symbolRoleField, true);
        SetFieldVisible(_compositionModeField, true);
        SetFieldVisible(_modifier1TypeField, equipment && role == OrbatEquipmentSymbolRole.Modifier1);
        SetFieldVisible(_modifier2TypeField, equipment && role == OrbatEquipmentSymbolRole.Modifier2);
        SetFieldVisible(_landUnitModifier1TypeField, !equipment && role == OrbatEquipmentSymbolRole.Modifier1);
        SetFieldVisible(_landUnitModifier2TypeField, !equipment && role == OrbatEquipmentSymbolRole.Modifier2);
        SetFieldVisible(_mobilityTypeField, equipment && role == OrbatEquipmentSymbolRole.MobilityIndicator);

        _unitTypeComboBox.Enabled = !equipment;
        _equipmentCategoryComboBox.Enabled = equipment;
        _equipmentFunctionComboBox.Enabled = equipment;
        _equipmentVariantComboBox.Enabled = equipment;
        _equipmentOperatingStateComboBox.Enabled = supportsInFlight;
        _symbolRoleComboBox.Enabled = true;
        _compositionModeComboBox.Enabled = true;
        _modifier1TypeComboBox.Enabled = equipment && role == OrbatEquipmentSymbolRole.Modifier1;
        _modifier2TypeComboBox.Enabled = equipment && role == OrbatEquipmentSymbolRole.Modifier2;
        _landUnitModifier1TypeComboBox.Enabled = !equipment && role == OrbatEquipmentSymbolRole.Modifier1;
        _landUnitModifier2TypeComboBox.Enabled = !equipment && role == OrbatEquipmentSymbolRole.Modifier2;
        _mobilityTypeComboBox.Enabled = equipment && role == OrbatEquipmentSymbolRole.MobilityIndicator;

        if (!supportsInFlight && GetSelectedEquipmentOperatingState() != OrbatEquipmentOperatingState.Ground)
            _equipmentOperatingStateComboBox.SelectedItem = OrbatEquipmentOperatingState.Ground.ToString();
    }

    private static void SetFieldVisible(Control? field, bool visible)
    {
        if (field is not null)
            field.Visible = visible;
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
        var tool = GetSelectedTool();
        _statusLabel.Text = tool switch
        {
            SymbolDesignerTool.SelectMove => "SelectMove: Ctrl+click selects multiple shapes; drag to move the selection or group.",
            SymbolDesignerTool.ParallelLine => "ParallelLine: select an existing line or segment, then drag a new line parallel to it.",
            SymbolDesignerTool.PerpendicularLine => "PerpendicularLine: select an existing line or segment, then drag a new line perpendicular to it.",
            SymbolDesignerTool.Arc => "Arc: click start, click highest point, click end.",
            SymbolDesignerTool.SineWave => "SineWave: drag a box to set the wave width and height.",
            SymbolDesignerTool.Circle => "Circle: drag from the center outward. Use Fill closed for a solid circle.",
            SymbolDesignerTool.Text => "Text: enter text in the options bar, then click the canvas to place it.",
            _ => "Draw: drag on the canvas. Reference: right-drag to move, mouse wheel to zoom, or use File > Reset reference."
        };

        foreach (var entry in _toolButtons)
        {
            var selected = entry.Key == tool;
            entry.Value.BackColor = selected ? Color.FromArgb(210, 232, 255) : SystemColors.Control;
            entry.Value.FlatAppearance.BorderColor = selected ? Color.FromArgb(0, 120, 215) : SystemColors.ControlDark;
            entry.Value.FlatAppearance.BorderSize = selected ? 2 : 1;
        }

        SetFieldVisible(_textOptionsField, tool == SymbolDesignerTool.Text);
        SetFieldVisible(_fillOptionsField, tool is SymbolDesignerTool.Rectangle
            or SymbolDesignerTool.Ellipse
            or SymbolDesignerTool.Circle
            or SymbolDesignerTool.Capsule);
        SetFieldVisible(_rotateOptionsField, tool == SymbolDesignerTool.SelectMove);
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
        IReadOnlyList<SymbolDrawCommand> commands;
        if (GetSelectedPhysicalDomain() == SymbolPhysicalDomain.Equipment)
        {
            if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.Modifier1)
            {
                commands = BuiltInSymbolLibrary.Create(GetSelectedModifier1Type());
                if (commands.Count == 0)
                {
                    MessageBox.Show(this, "Select a Modifier 1 type first.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _canvas.SetCommands(commands);
                return;
            }

            if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.Modifier2)
            {
                commands = BuiltInSymbolLibrary.Create(GetSelectedModifier2Type());
                if (commands.Count == 0)
                {
                    MessageBox.Show(this, "Select a Modifier 2 type first.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _canvas.SetCommands(commands);
                return;
            }

            if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.MobilityIndicator)
            {
                commands = LoadMobilityBaseCommands(GetSelectedMobilityType());
                if (commands.Count == 0)
                {
                    MessageBox.Show(this, "Select a Mobility type first.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _canvas.SetCommands(commands);
                return;
            }

            var equipmentFunction = GetSelectedEquipmentFunction();
            commands = BuiltInSymbolLibrary.Create(equipmentFunction);
            if (commands.Count == 0)
            {
                MessageBox.Show(this, "No editable base symbol is available for this equipment function yet.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _canvas.SetCommands(commands);
            return;
        }

        if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.Modifier1)
        {
            commands = BuiltInSymbolLibrary.Create(GetSelectedLandUnitModifier1Type());
            if (commands.Count == 0)
            {
                MessageBox.Show(this, "Select a LandUnit Modifier 1 type first.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _canvas.SetCommands(commands);
            return;
        }

        if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.Modifier2)
        {
            commands = BuiltInSymbolLibrary.Create(GetSelectedLandUnitModifier2Type());
            if (commands.Count == 0)
            {
                MessageBox.Show(this, "Select a LandUnit Modifier 2 type first.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _canvas.SetCommands(commands);
            return;
        }

        var selected = Convert.ToString(_unitTypeComboBox.SelectedItem);
        if (!Enum.TryParse(selected, out Components.OrbatUnitType unitType))
            return;

        commands = BuiltInSymbolLibrary.Create(unitType);
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
            FileName = $"{GetDefaultLibraryFileBaseName()}.orbatsymbol.json",
            InitialDirectory = SymbolLibraryLocator.FindDefaultFolder() ?? string.Empty
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var libraryName = SymbolLibraryFileNaming.GetLogicalName(dialog.FileName);
        var selectedUnitType = Convert.ToString(_unitTypeComboBox.SelectedItem) ?? Components.OrbatUnitType.Unspecified.ToString();
        var libraryUnitType = InferUnitTypeFromLibraryName(libraryName, selectedUnitType);
        var definition = new SymbolLibraryDefinition
        {
            Name = libraryName,
            UnitType = libraryUnitType,
            EquipmentFunction = GetSelectedEquipmentFunction().ToString(),
            Variant = GetSelectedEquipmentVariant(),
            SymbolRole = GetSelectedSymbolRole(),
            CompositionMode = GetSelectedCompositionMode(),
            Layout = OrbatEquipmentSymbolLayout.CreateDefault(),
            Modifier1Type = GetSelectedModifier1Type().ToString(),
            Modifier2Type = GetSelectedModifier2Type().ToString(),
            LandUnitModifier1Type = GetSelectedLandUnitModifier1Type().ToString(),
            LandUnitModifier2Type = GetSelectedLandUnitModifier2Type().ToString(),
            MobilityType = GetSelectedMobilityType().ToString(),
            Affiliation = GetSelectedAffiliation(),
            PhysicalDomain = GetSelectedPhysicalDomain(),
            FrameShape = GetSelectedFrameShape(),
            FrameStatus = GetSelectedFrameStatus(),
            OperatingState = GetSelectedEquipmentOperatingState(),
            Version = 5,
            Commands = _canvas.Commands.Select(command => command.Clone()).ToList()
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(definition, options), Encoding.UTF8);
        RefreshEquipmentVariantOptions();
    }

    private string GetDefaultLibraryFileBaseName()
    {
        if (GetSelectedPhysicalDomain() == SymbolPhysicalDomain.Equipment)
        {
            if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.Modifier1
                && GetSelectedModifier1Type() != OrbatEquipmentModifier1.Unspecified)
                return $"Equipment.Modifier1.{GetSelectedModifier1Type()}";
            if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.Modifier2
                && GetSelectedModifier2Type() != OrbatEquipmentModifier2.Unspecified)
                return $"Equipment.Modifier2.{GetSelectedModifier2Type()}";
            if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.MobilityIndicator
                && GetSelectedMobilityType() != OrbatEquipmentMobilityMode.Unspecified)
                return $"Equipment.Amplifier.R.{GetSelectedMobilityType()}";

            var baseName = GetSelectedEquipmentFunction().ToString();
            var variant = GetSelectedEquipmentVariant();
            var roleSuffix = GetSelectedSymbolRole() is OrbatEquipmentSymbolRole.Modifier1 or OrbatEquipmentSymbolRole.Modifier2 or OrbatEquipmentSymbolRole.MobilityIndicator
                ? $".{GetSelectedSymbolRole()}"
                : string.Empty;
            return string.IsNullOrWhiteSpace(variant)
                ? $"Equipment.{baseName}{roleSuffix}"
                : $"Equipment.{baseName}.{SanitizeFileNamePart(variant)}{roleSuffix}";
        }

        if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.Modifier1
            && GetSelectedLandUnitModifier1Type() != OrbatLandUnitModifier1.Unspecified)
            return $"LandUnit.Modifier1.{GetSelectedLandUnitModifier1Type()}";
        if (GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.Modifier2
            && GetSelectedLandUnitModifier2Type() != OrbatLandUnitModifier2.Unspecified)
            return $"LandUnit.Modifier2.{GetSelectedLandUnitModifier2Type()}";

        var unitType = Convert.ToString(_unitTypeComboBox.SelectedItem);
        var unitTypeName = string.IsNullOrWhiteSpace(unitType)
            ? Components.OrbatUnitType.Unspecified.ToString()
            : unitType;
        return $"LandUnit.{unitTypeName}";
    }

    private static string SanitizeFileNamePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "Variant" : sanitized;
    }

    private static string GetLibraryNameFromFileName(string fileName)
    {
        var name = SymbolLibraryFileNaming.GetLogicalName(fileName);
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

    private static IReadOnlyList<SymbolDrawCommand> LoadMobilityBaseCommands(OrbatEquipmentMobilityMode mobility)
    {
        IReadOnlyList<SymbolDrawCommand>? selected = null;
        foreach (var file in SymbolOverlayDemoForm.GetRecentLibraryFiles().OrderBy(File.GetLastWriteTimeUtc))
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(
                    File.ReadAllText(file, Encoding.UTF8),
                    SymbolOverlayDemoForm.LibraryJsonOptions);
                if (definition == null
                    || definition.GetEffectivePhysicalDomain() != SymbolPhysicalDomain.Equipment
                    || definition.SymbolRole != OrbatEquipmentSymbolRole.MobilityIndicator
                    || !Enum.TryParse(definition.MobilityType, ignoreCase: true, out OrbatEquipmentMobilityMode fileMobility)
                    || fileMobility != mobility
                    || definition.Commands.Count == 0)
                    continue;

                selected = definition.Commands.Select(command => command.Clone()).ToList();
            }
            catch
            {
                // Keep the built-in mobility symbol available when an override cannot be read.
            }
        }

        return selected ?? BuiltInSymbolLibrary.Create(mobility);
    }

    private void ImportDrawingFromLibrary()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import drawing only from symbol library",
            Filter = "ORBAT symbol library|*.orbatsymbol.json;*.json|All files|*.*",
            InitialDirectory = SymbolLibraryLocator.FindDefaultFolder() ?? string.Empty
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(
                File.ReadAllText(dialog.FileName, Encoding.UTF8),
                SymbolOverlayDemoForm.LibraryJsonOptions);
            if (definition == null || definition.Commands.Count == 0)
            {
                MessageBox.Show(this, "The selected library file does not contain drawing commands.", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _canvas.SetCommands(definition.Commands.Select(command => command.Clone()).ToList());
            _statusLabel.Text = $"Imported drawing only from {Path.GetFileName(dialog.FileName)}. Current LandUnit metadata was preserved.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"The drawing could not be imported.\r\n\r\n{exception.Message}", "Symbol Designer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadLibrary()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load symbol library",
            Filter = "ORBAT symbol library|*.orbatsymbol.json;*.json|All files|*.*",
            InitialDirectory = SymbolLibraryLocator.FindDefaultFolder() ?? string.Empty
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

        _affiliationComboBox.SelectedItem = definition.GetEffectiveAffiliation().ToString();
        _physicalDomainComboBox.SelectedItem = definition.GetEffectivePhysicalDomain().ToString();
        if (Enum.TryParse(definition.UnitType, out Components.OrbatUnitType unitType))
            _unitTypeComboBox.SelectedItem = unitType.ToString();
        if (Enum.TryParse(definition.EquipmentFunction, out Components.OrbatEquipmentFunction equipmentFunction))
            SelectEquipmentFunction(equipmentFunction);
        _equipmentVariantComboBox.SelectedIndex = -1;
        _equipmentVariantComboBox.Text = definition.Variant;
        RefreshEquipmentVariantOptions();
        _symbolRoleComboBox.SelectedItem = definition.SymbolRole.ToString();
        _compositionModeComboBox.SelectedItem = definition.CompositionMode.ToString();
        if (Enum.TryParse(definition.Modifier1Type, out OrbatEquipmentModifier1 modifier1Type))
            _modifier1TypeComboBox.SelectedItem = _modifier1TypeComboBox.Items.Cast<EquipmentModifier1Selection>()
                .FirstOrDefault(item => item.Value == modifier1Type);
        if (Enum.TryParse(definition.Modifier2Type, out OrbatEquipmentModifier2 modifier2Type))
            _modifier2TypeComboBox.SelectedItem = _modifier2TypeComboBox.Items.Cast<EquipmentModifier2Selection>()
                .FirstOrDefault(item => item.Value == modifier2Type);
        if (Enum.TryParse(definition.LandUnitModifier1Type, out OrbatLandUnitModifier1 landUnitModifier1Type))
            _landUnitModifier1TypeComboBox.SelectedItem = _landUnitModifier1TypeComboBox.Items.Cast<LandUnitModifier1Selection>()
                .FirstOrDefault(item => item.Value == landUnitModifier1Type);
        if (Enum.TryParse(definition.LandUnitModifier2Type, out OrbatLandUnitModifier2 landUnitModifier2Type))
            _landUnitModifier2TypeComboBox.SelectedItem = _landUnitModifier2TypeComboBox.Items.Cast<LandUnitModifier2Selection>()
                .FirstOrDefault(item => item.Value == landUnitModifier2Type);
        if (Enum.TryParse(definition.MobilityType, out OrbatEquipmentMobilityMode mobilityType))
            _mobilityTypeComboBox.SelectedItem = _mobilityTypeComboBox.Items.Cast<EquipmentMobilitySelection>()
                .FirstOrDefault(item => item.Value == mobilityType);
        UpdateFunctionSelectorState();
        _frameStatusComboBox.SelectedItem = definition.FrameStatus.ToString();
        _equipmentOperatingStateComboBox.SelectedItem = definition.GetEffectiveOperatingState().ToString();
        _canvas.FrameShape = definition.GetEffectiveFrameShape();
        _canvas.SymbolRole = definition.SymbolRole;
        _canvas.FrameStatus = definition.FrameStatus;
        _canvas.SetCommands(definition.Commands);
        Text = $"ORBAT Symbol Designer - {GetLibraryNameFromFileName(fileName)}";
    }

    private void ViewLibrary()
    {
        using var form = new SymbolLibraryViewerForm();
        form.ShowDialog(this);
        RefreshEquipmentVariantOptions();
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
        var fillUpperFrameCap = GetSelectedPhysicalDomain() == SymbolPhysicalDomain.Equipment
            && GetSelectedEquipmentFunction() == Components.OrbatEquipmentFunction.CommunicationsSatellite
            && GetSelectedEquipmentOperatingState() == OrbatEquipmentOperatingState.InFlight;
        _canvas.FillUpperFrameCap = fillUpperFrameCap;
        _canvas.SymbolRole = GetSelectedSymbolRole();
        _canvas.Invalidate();
        _preview.SetFrame(GetSelectedFrameShape(), GetSelectedFrameStatus());
        _preview.FillUpperFrameCap = fillUpperFrameCap;
        _preview.PhysicalDomain = GetSelectedPhysicalDomain();
        _preview.SymbolRole = GetSelectedSymbolRole();
        _preview.CompositionMode = GetSelectedCompositionMode();
        _preview.SymbolLayout = OrbatEquipmentSymbolLayout.CreateDefault();
        _preview.ComponentOnly = GetSelectedSymbolRole() == OrbatEquipmentSymbolRole.MobilityIndicator;
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
            command.FontSize = Math.Clamp((float)_fontSizeInput.Value, 4f, 160f);
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

    private static decimal ToFontSizeDecimal(float value) => Math.Min(160m, Math.Max(4m, (decimal)value));

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
    BezierArc,
    SineWave
}

internal enum SymbolFrameShape
{
    FriendlyUnit,
    FriendlyEquipment,
    FriendlyEquipmentInFlight,
    Hostile,
    HostileEquipmentInFlight,
    Neutral,
    NeutralEquipmentInFlight,
    Unknown,
    UnknownEquipmentInFlight
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
    public static SymbolFrameShape GetFrameShape(
        SymbolAffiliation affiliation,
        SymbolPhysicalDomain domain,
        OrbatEquipmentOperatingState operatingState = OrbatEquipmentOperatingState.Ground)
    {
        if (domain == SymbolPhysicalDomain.Equipment && operatingState == OrbatEquipmentOperatingState.InFlight)
        {
            return affiliation switch
            {
                SymbolAffiliation.Friendly => SymbolFrameShape.FriendlyEquipmentInFlight,
                SymbolAffiliation.Hostile => SymbolFrameShape.HostileEquipmentInFlight,
                SymbolAffiliation.Neutral => SymbolFrameShape.NeutralEquipmentInFlight,
                SymbolAffiliation.Unknown => SymbolFrameShape.UnknownEquipmentInFlight,
                _ => SymbolFrameShape.FriendlyEquipmentInFlight
            };
        }

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
            SymbolFrameShape.Hostile or SymbolFrameShape.HostileEquipmentInFlight => SymbolAffiliation.Hostile,
            SymbolFrameShape.Neutral or SymbolFrameShape.NeutralEquipmentInFlight => SymbolAffiliation.Neutral,
            SymbolFrameShape.Unknown or SymbolFrameShape.UnknownEquipmentInFlight => SymbolAffiliation.Unknown,
            _ => SymbolAffiliation.Friendly
        };
    }

    public static SymbolPhysicalDomain GetPhysicalDomain(SymbolFrameShape frameShape)
    {
        return frameShape is SymbolFrameShape.FriendlyEquipment
            or SymbolFrameShape.FriendlyEquipmentInFlight
            or SymbolFrameShape.HostileEquipmentInFlight
            or SymbolFrameShape.NeutralEquipmentInFlight
            or SymbolFrameShape.UnknownEquipmentInFlight
                ? SymbolPhysicalDomain.Equipment
                : SymbolPhysicalDomain.LandUnit;
    }

    public static OrbatEquipmentOperatingState GetOperatingState(SymbolFrameShape frameShape) =>
        IsInFlightFrame(frameShape)
            ? OrbatEquipmentOperatingState.InFlight
            : OrbatEquipmentOperatingState.Ground;

    public static bool IsInFlightFrame(SymbolFrameShape frameShape) =>
        frameShape is SymbolFrameShape.FriendlyEquipmentInFlight
            or SymbolFrameShape.HostileEquipmentInFlight
            or SymbolFrameShape.NeutralEquipmentInFlight
            or SymbolFrameShape.UnknownEquipmentInFlight;
}

internal enum IconGuideShape
{
    FlatTopBottom,
    PointedTopBottom
}

internal sealed class SymbolDesignerCanvas : Control
{
    private const float SnapThreshold = 0.025f;
    private const float AxisSnapThreshold = 0.035f;
    private const float PointSnapThreshold = 0.045f;
    private const float LineSnapThreshold = 0.032f;
    private const float StandardFrameAspectRatio = 1.5f;
    private const int HistoryLimit = 100;
    private readonly List<SymbolDrawCommand> _commands = new();
    private readonly HashSet<int> _selectedIndices = new();
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
    private SymbolDrawCommand? _resizeStartCommand;
    private bool _editHistoryCommitted;

    public event EventHandler? CommandsChanged;
    public event EventHandler? SelectionChanged;

    public SymbolFrameShape FrameShape { get; set; } = SymbolFrameShape.FriendlyUnit;
    public OrbatEquipmentSymbolRole SymbolRole { get; set; } = OrbatEquipmentSymbolRole.MainFunction;
    public SymbolFrameStatus FrameStatus { get; set; } = SymbolFrameStatus.Present;
    public bool FillUpperFrameCap { get; set; }

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
    public IReadOnlyCollection<int> SelectedIndices => _selectedIndices;
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
        SetSelection(_commands.Count > 0 ? new[] { 0 } : Array.Empty<int>(), _commands.Count > 0 ? 0 : -1);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void AddCommand(SymbolDrawCommand command)
    {
        SaveHistory();
        _commands.Add(command);
        SetSelection(new[] { _commands.Count - 1 }, _commands.Count - 1);
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
        if (_selectedIndices.Count == 0)
            return;

        SaveHistory();
        var duplicateGroupId = _selectedIndices.Count > 1 ? Guid.NewGuid().ToString("N") : null;
        var duplicates = _selectedIndices.OrderBy(index => index)
            .Select(index =>
            {
                var duplicate = _commands[index].Clone();
                duplicate.Move(new SymbolPoint(0.04f, 0.04f));
                duplicate.GroupId = duplicateGroupId;
                return duplicate;
            })
            .ToList();
        var firstIndex = _commands.Count;
        _commands.AddRange(duplicates);
        SetSelection(Enumerable.Range(firstIndex, duplicates.Count), firstIndex);
        _copiedCommand = duplicates[0].Clone();
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void FitContentToFrame()
    {
        if (_commands.Count == 0)
            return;

        var bounds = _commands
            .Select(command => command.GetNormalizedVisualBounds())
            .Aggregate(RectangleF.Union);
        if (bounds.Width <= 0.0001f || bounds.Height <= 0.0001f)
            return;

        var target = SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator
            ? new RectangleF(0.06f, 0.10f, 0.88f, 0.80f)
            : new RectangleF(0.08f, 0.08f, 0.84f, 0.84f);
        var scale = Math.Min(target.Width / bounds.Width, target.Height / bounds.Height);
        var offset = new SymbolPoint(
            target.Left + (target.Width - bounds.Width * scale) / 2f - bounds.Left * scale,
            target.Top + (target.Height - bounds.Height * scale) / 2f - bounds.Top * scale);

        SaveHistory();
        foreach (var command in _commands)
            command.ScaleAndTranslate(scale, offset);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }
    public void RotateSelectedClockwise() => RotateSelected(90f);

    public void RotateSelected(float degrees)
    {
        if (Math.Abs(degrees) < 0.001f || _selectedIndices.Count == 0)
            return;

        var center = GetSelectionCenter();
        TransformSelected(command => command.Rotate(degrees, center));
    }

    public void MirrorSelectedHorizontal()
    {
        var center = GetSelectionCenter();
        TransformSelected(command => command.MirrorHorizontal(center.X));
    }

    public void MirrorSelectedVertical()
    {
        var center = GetSelectionCenter();
        TransformSelected(command => command.MirrorVertical(center.Y));
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

    public bool JoinSelectedLines()
    {
        var indices = _selectedIndices
            .Where(index => _commands[index].Kind == SymbolDrawCommandKind.Line)
            .OrderBy(index => index)
            .ToList();
        if (indices.Count < 2)
            return false;

        var segments = indices.Select(index => _commands[index].Clone()).ToList();
        var points = BuildConnectedPath(segments);
        if (segments.Count > 0 || points.Count < 3)
            return false;

        SaveHistory();
        var strokeWidth = indices.Select(index => _commands[index].StrokeWidth).DefaultIfEmpty(2f).Max();
        foreach (var index in indices.OrderByDescending(index => index))
            _commands.RemoveAt(index);
        var polyline = SymbolDrawCommand.Polyline(points).WithStrokeWidth(strokeWidth);
        _commands.Add(polyline);
        SetSelection(new[] { _commands.Count - 1 }, _commands.Count - 1);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
        return true;
    }

    public bool GroupSelected()
    {
        if (_selectedIndices.Count < 2)
            return false;

        SaveHistory();
        var groupId = Guid.NewGuid().ToString("N");
        foreach (var index in _selectedIndices)
            _commands[index].GroupId = groupId;
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
        return true;
    }

    public bool UngroupSelected()
    {
        var groupedIndices = _selectedIndices
            .Where(index => !string.IsNullOrWhiteSpace(_commands[index].GroupId))
            .ToList();
        if (groupedIndices.Count == 0)
            return false;

        SaveHistory();
        foreach (var index in groupedIndices)
            _commands[index].GroupId = null;
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
        return true;
    }

    public void AlignSelectedTop() => AlignSelected(alignTop: true);

    public void AlignSelectedBottom() => AlignSelected(alignTop: false);

    public void SizeSelectedToSmallest() => SizeSelected(useSmallest: true);

    public void SizeSelectedToLargest() => SizeSelected(useSmallest: false);

    private void AlignSelected(bool alignTop)
    {
        var units = GetSelectedTransformUnits();
        if (units.Count < 2)
            return;

        var unitBounds = units.Select(GetUnitBounds).ToList();
        var target = alignTop
            ? unitBounds.Min(bounds => bounds.Top)
            : unitBounds.Max(bounds => bounds.Bottom);

        SaveHistory();
        for (var unitIndex = 0; unitIndex < units.Count; unitIndex++)
        {
            var bounds = unitBounds[unitIndex];
            var deltaY = alignTop ? target - bounds.Top : target - bounds.Bottom;
            foreach (var commandIndex in units[unitIndex])
                _commands[commandIndex].Move(new SymbolPoint(0f, deltaY));
        }

        NotifyCommandsChanged(includeSelection: true);
    }

    private void SizeSelected(bool useSmallest)
    {
        var units = GetSelectedTransformUnits();
        if (units.Count < 2)
            return;

        var unitBounds = units.Select(GetUnitBounds).ToList();
        var extents = unitBounds
            .Select(bounds => Math.Max(bounds.Width, bounds.Height))
            .ToList();
        var positiveExtents = extents.Where(extent => extent > 0.0001f).ToList();
        if (positiveExtents.Count == 0)
            return;

        var targetExtent = useSmallest ? positiveExtents.Min() : positiveExtents.Max();
        SaveHistory();
        for (var unitIndex = 0; unitIndex < units.Count; unitIndex++)
        {
            var extent = extents[unitIndex];
            if (extent <= 0.0001f)
                continue;

            var bounds = unitBounds[unitIndex];
            var center = new SymbolPoint(
                bounds.Left + bounds.Width / 2f,
                bounds.Top + bounds.Height / 2f);
            var scale = targetExtent / extent;
            var offset = new SymbolPoint(
                center.X * (1f - scale),
                center.Y * (1f - scale));
            foreach (var commandIndex in units[unitIndex])
                _commands[commandIndex].ScaleAndTranslate(scale, offset);
        }

        NotifyCommandsChanged(includeSelection: true);
    }

    private List<List<int>> GetSelectedTransformUnits()
    {
        var units = new List<List<int>>();
        var handledGroups = new HashSet<string>(StringComparer.Ordinal);
        foreach (var index in _selectedIndices.OrderBy(index => index))
        {
            var groupId = _commands[index].GroupId;
            if (string.IsNullOrWhiteSpace(groupId))
            {
                units.Add(new List<int> { index });
                continue;
            }

            if (!handledGroups.Add(groupId))
                continue;

            units.Add(_selectedIndices
                .Where(selectedIndex => _commands[selectedIndex].GroupId == groupId)
                .OrderBy(selectedIndex => selectedIndex)
                .ToList());
        }

        return units;
    }

    private RectangleF GetUnitBounds(IReadOnlyList<int> indices) =>
        indices
            .Select(index => _commands[index].GetNormalizedVisualBounds())
            .Aggregate(RectangleF.Union);

    public void SelectCommand(int index)
    {
        var normalized = index >= 0 && index < _commands.Count ? index : -1;
        if (normalized < 0)
        {
            SetSelection(Array.Empty<int>(), -1);
        }
        else
        {
            var groupId = _commands[normalized].GroupId;
            var indices = string.IsNullOrWhiteSpace(groupId)
                ? new[] { normalized }
                : _commands.Select((command, commandIndex) => (command, commandIndex))
                    .Where(item => item.command.GroupId == groupId)
                    .Select(item => item.commandIndex);
            SetSelection(indices, normalized);
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void ToggleCommandSelection(int index)
    {
        if (index < 0 || index >= _commands.Count)
            return;

        var groupId = _commands[index].GroupId;
        var indices = string.IsNullOrWhiteSpace(groupId)
            ? new[] { index }
            : _commands.Select((command, commandIndex) => (command, commandIndex))
                .Where(item => item.command.GroupId == groupId)
                .Select(item => item.commandIndex)
                .ToArray();
        var remove = indices.All(selectedIndex => _selectedIndices.Contains(selectedIndex));
        foreach (var selectedIndex in indices)
        {
            if (remove)
                _selectedIndices.Remove(selectedIndex);
            else
                _selectedIndices.Add(selectedIndex);
        }

        SelectedIndex = _selectedIndices.Contains(index) ? index : _selectedIndices.DefaultIfEmpty(-1).Max();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void SetSelection(IEnumerable<int> indices, int primaryIndex)
    {
        _selectedIndices.Clear();
        foreach (var index in indices.Where(index => index >= 0 && index < _commands.Count))
            _selectedIndices.Add(index);
        SelectedIndex = _selectedIndices.Contains(primaryIndex)
            ? primaryIndex
            : _selectedIndices.DefaultIfEmpty(-1).Max();
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
        if (_selectedIndices.Count == 0)
            return;

        SaveHistory();
        foreach (var index in _selectedIndices.OrderBy(index => index))
            transform(_commands[index]);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private SymbolPoint GetSelectionCenter()
    {
        var bounds = _selectedIndices
            .Select(index => _commands[index].GetNormalizedVisualBounds())
            .Aggregate(RectangleF.Union);
        return new SymbolPoint(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
    }

    public void DeleteSelected()
    {
        if (_selectedIndices.Count == 0)
            return;

        SaveHistory();
        var nextIndex = _selectedIndices.Min();
        foreach (var index in _selectedIndices.OrderByDescending(index => index))
            _commands.RemoveAt(index);
        nextIndex = Math.Min(nextIndex, _commands.Count - 1);
        SetSelection(nextIndex >= 0 ? new[] { nextIndex } : Array.Empty<int>(), nextIndex);
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
        SetSelection(Array.Empty<int>(), -1);
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
        new(_commands.Select(command => command.Clone()).ToList(), SelectedIndex, _selectedIndices.OrderBy(index => index).ToList());

    private void RestoreSnapshot(SymbolCanvasSnapshot snapshot)
    {
        _commands.Clear();
        _commands.AddRange(snapshot.Commands.Select(command => command.Clone()));
        var selectedIndices = snapshot.SelectedIndices.Where(index => index >= 0 && index < _commands.Count).ToArray();
        var primaryIndex = snapshot.SelectedIndex >= 0 && snapshot.SelectedIndex < _commands.Count
            ? snapshot.SelectedIndex
            : selectedIndices.DefaultIfEmpty(-1).Max();
        SetSelection(selectedIndices, primaryIndex);
    }

    private bool SnapshotEquals(SymbolCanvasSnapshot snapshot)
    {
        if (SelectedIndex != snapshot.SelectedIndex
            || !_selectedIndices.SetEquals(snapshot.SelectedIndices)
            || _commands.Count != snapshot.Commands.Count)
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
            BeginEditDrag(e.Location, symbolPoint, (ModifierKeys & Keys.Control) == Keys.Control);
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
            _resizeStartCommand = null;
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
        e.Graphics.FillRectangle(Brushes.White, workspace);
        DrawReference(e.Graphics, drawingFrame);
        if (ShowGrid)
            DrawGrid(e.Graphics, drawingFrame, drawingFrame);
        if (SymbolRole != OrbatEquipmentSymbolRole.MobilityIndicator)
        {
            if (ShowIconGuide)
                DrawIconGuide(e.Graphics, FrameShape == SymbolFrameShape.FriendlyEquipment ? frame : drawingFrame);
            SymbolFrameRenderer.DrawFrame(e.Graphics, frame, FrameShape, FrameStatus, fillFrame: false, IconGuideShape, fillUpperCap: FillUpperFrameCap);
        }
        DrawDrawingBounds(e.Graphics, drawingFrame);

        for (var index = 0; index < _commands.Count; index++)
        {
            var selected = _selectedIndices.Contains(index);
            using var pen = new Pen(selected ? Color.FromArgb(40, 120, 220) : Color.Black, selected ? 2.4f : 2f);
            if (SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator)
                _commands[index].Draw(e.Graphics, drawingFrame, pen, Brushes.Black);
            else
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

    private void BeginEditDrag(Point mousePoint, PointF symbolPoint, bool toggleSelection)
    {
        var frame = GetDrawingFrameBounds();
        if (toggleSelection)
        {
            ToggleCommandSelection(HitTestCommand(mousePoint, frame));
            return;
        }
        var target = HitTestHandle(mousePoint, frame);
        if (target.Target != DragTarget.None)
        {
            SelectCommand(target.Index);
            _pendingEditSnapshot = CaptureSnapshot();
            _resizeStartCommand = IsRectangleResizeTarget(target.Target)
                ? _commands[target.Index].Clone()
                : null;
            _editHistoryCommitted = false;
            _dragTarget = target.Target;
            _lastDragPoint = symbolPoint;
            return;
        }

        _resizeStartCommand = null;
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

            foreach (var index in _selectedIndices)
                _commands[index].Move(delta);
            _lastDragPoint = symbolPoint;
        }
        else
        {
            var point = new SymbolPoint(symbolPoint);
            if (_resizeStartCommand != null && IsRectangleResizeTarget(_dragTarget))
                command.ResizeFrom(_resizeStartCommand, _dragTarget, point);
            else
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

    private static bool IsRectangleResizeTarget(DragTarget target) =>
        target is DragTarget.TopLeft
            or DragTarget.Top
            or DragTarget.TopRight
            or DragTarget.Right
            or DragTarget.BottomRight
            or DragTarget.Bottom
            or DragTarget.BottomLeft
            or DragTarget.Left;

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
        if (_selectedIndices.Count == 0)
            return;

        foreach (var index in _selectedIndices.OrderBy(index => index))
        {
            var command = _commands[index];
            var commandFrame = GetCommandDrawingFrame(frame, command);
            var primary = index == SelectedIndex;
            var bounds = command.GetNormalizedVisualBounds();
            var absoluteBounds = new RectangleF(
                commandFrame.Left + bounds.Left * commandFrame.Width,
                commandFrame.Top + bounds.Top * commandFrame.Height,
                bounds.Width * commandFrame.Width,
                bounds.Height * commandFrame.Height);

            if (absoluteBounds.Width > 0.5f && absoluteBounds.Height > 0.5f)
            {
                using var outlinePen = new Pen(
                    primary ? Color.DodgerBlue : Color.FromArgb(190, 60, 155, 235),
                    primary ? 1.6f : 1.2f)
                {
                    DashStyle = DashStyle.Dash
                };
                graphics.DrawRectangle(outlinePen, Rectangle.Round(absoluteBounds));
            }

            var handleSize = primary ? 8f : 7f;
            using var handleBrush = new SolidBrush(primary ? Color.White : Color.FromArgb(210, 185, 225, 255));
            using var handlePen = new Pen(primary ? Color.DodgerBlue : Color.FromArgb(40, 120, 220), primary ? 1.4f : 1.1f);
            foreach (var handle in command.GetHandles())
            {
                var absolute = ToAbsolute(commandFrame, handle.Point);
                var rect = new RectangleF(
                    absolute.X - handleSize / 2f,
                    absolute.Y - handleSize / 2f,
                    handleSize,
                    handleSize);
                graphics.FillRectangle(handleBrush, rect);
                graphics.DrawRectangle(handlePen, Rectangle.Round(rect));
            }
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
        if (SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator)
        {
            using var mobilityPen = new Pen(Color.FromArgb(175, 40, 120, 220), 1.2f);
            graphics.DrawRectangle(mobilityPen, Rectangle.Round(drawingFrame));
            return;
        }

        if (FrameShape is SymbolFrameShape.FriendlyUnit or SymbolFrameShape.FriendlyEquipment)
            return;

        using var pen = new Pen(Color.FromArgb(170, 40, 120, 220), 1f) { DashStyle = DashStyle.Dash };
        graphics.DrawRectangle(pen, Rectangle.Round(drawingFrame));
    }

    private SymbolDrawCommand? CreateCommand(PointF start, PointF end)
    {
        if (Distance(start, end) < 0.012f && Tool != SymbolDesignerTool.Dot && Tool != SymbolDesignerTool.Text)
            return null;

        var requiresArea = Tool == SymbolDesignerTool.Rectangle
            || Tool == SymbolDesignerTool.Ellipse
            || Tool == SymbolDesignerTool.Capsule
            || Tool == SymbolDesignerTool.SineWave
            || Tool == SymbolDesignerTool.Arc;
        if (requiresArea && !HasRenderableArea(start, end))
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
            SymbolDesignerTool.SineWave => SymbolDrawCommand.SineWave(start, end),
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
        var priorityCandidates = GetPrioritySnapCandidates().ToList();
        var best = point;
        var bestDistance = PointSnapThreshold;
        foreach (var candidate in priorityCandidates)
        {
            var distance = Distance(point, candidate);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        if (bestDistance < PointSnapThreshold)
            return best;

        var axisCandidates = priorityCandidates.Concat(GetGridSnapCandidates()).ToList();
        var nearestX = axisCandidates
            .Select(candidate => candidate.X)
            .OrderBy(value => Math.Abs(value - point.X))
            .FirstOrDefault(point.X);
        var nearestY = axisCandidates
            .Select(candidate => candidate.Y)
            .OrderBy(value => Math.Abs(value - point.Y))
            .FirstOrDefault(point.Y);
        best = new PointF(
            Math.Abs(nearestX - point.X) <= AxisSnapThreshold ? nearestX : point.X,
            Math.Abs(nearestY - point.Y) <= AxisSnapThreshold ? nearestY : point.Y);
        bestDistance = Distance(point, best);

        foreach (var candidate in GetLineSnapCandidates(point))
        {
            var distance = Distance(point, candidate);
            if (distance < Math.Min(bestDistance, LineSnapThreshold))
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private IEnumerable<PointF> GetPrioritySnapCandidates()
    {
        for (var index = 0; index < _commands.Count; index++)
        {
            if (_dragTarget != DragTarget.None && _selectedIndices.Contains(index))
                continue;

            foreach (var point in _commands[index].GetSnapPoints())
                yield return point;
        }

        foreach (var intersection in GetLineIntersections())
            yield return intersection;
    }

    private IEnumerable<PointF> GetGridSnapCandidates()
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
    }

    private IEnumerable<PointF> GetLineSnapCandidates(PointF point)
    {
        for (var index = 0; index < _commands.Count; index++)
        {
            if (_dragTarget != DragTarget.None && _selectedIndices.Contains(index))
                continue;

            var command = _commands[index];
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
        var segments = _commands.SelectMany((command, index) =>
            _dragTarget != DragTarget.None && _selectedIndices.Contains(index)
                ? Enumerable.Empty<SymbolSegment>()
                : command.GetSegments()).ToArray();
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
        var mobilityIndicator = SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator;
        var width = mobilityIndicator
            ? Math.Min(maxWidth, maxHeight * 3f)
            : Math.Min(maxWidth, maxHeight);
        var height = mobilityIndicator ? width / 3f : width / StandardFrameAspectRatio;

        return new RectangleF(
            (ClientSize.Width - width) / 2f,
            (ClientSize.Height - height) / 2f,
            width,
            height);
    }

    private RectangleF GetDrawingFrameBounds() => GetDrawingFrameBounds(GetFrameBounds());

    private RectangleF GetDrawingFrameBounds(RectangleF frame) =>
        SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator
            ? frame
            : SymbolFrameRenderer.GetInteriorFrame(frame, FrameShape, IconGuideShape);

    private RectangleF GetGuideFrameBounds(RectangleF frame) =>
        SymbolFrameRenderer.GetGuideFrame(frame, FrameShape, IconGuideShape);

    private RectangleF GetCommandDrawingFrame(RectangleF drawingFrame, SymbolDrawCommand command) =>
        SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator
            ? drawingFrame
            : SymbolFrameRenderer.GetCommandFrame(drawingFrame, FrameShape, command);

    private RectangleF GetWorkspaceBounds()
    {
        var frame = GetFrameBounds();
        if (SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator)
            return frame;

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
        var frame = GetDrawingFrameBounds();
        var aspectRatio = frame.Width / Math.Max(1f, frame.Height);
        var horizontalStepInFrame = 1f / divisions / aspectRatio;
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
    private static readonly (SymbolAffiliation Affiliation, string Label)[] AffiliationPreviews =
    {
        (SymbolAffiliation.Friendly, "Friendly"),
        (SymbolAffiliation.Hostile, "Hostile"),
        (SymbolAffiliation.Neutral, "Neutral"),
        (SymbolAffiliation.Unknown, "Unknown")
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
    public OrbatEquipmentSymbolRole SymbolRole { get; set; } = OrbatEquipmentSymbolRole.Composite;
    public OrbatEquipmentCompositionMode CompositionMode { get; set; } = OrbatEquipmentCompositionMode.Composite;
    public OrbatEquipmentSymbolLayout SymbolLayout { get; set; } = OrbatEquipmentSymbolLayout.CreateDefault();
    public bool FillUpperFrameCap { get; set; }
    public bool ComponentOnly { get; set; }

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

        if (ComponentOnly)
        {
            DrawComponentPreview(e.Graphics);
            return;
        }

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
            var shape = SymbolFrameMapping.GetFrameShape(
                AffiliationPreviews[index].Affiliation,
                PhysicalDomain,
                SymbolFrameMapping.GetOperatingState(_frameShape));
            DrawAffiliationPreview(e.Graphics, tile, shape, AffiliationPreviews[index].Label);
        }
    }

    private void DrawComponentPreview(Graphics graphics)
    {
        var title = SymbolRole switch
        {
            OrbatEquipmentSymbolRole.Modifier2 => "Modifier 2 component",
            OrbatEquipmentSymbolRole.MobilityIndicator => "R mobility indicator",
            _ => "Modifier 1 component"
        };
        TextRenderer.DrawText(
            graphics,
            title,
            Font,
            new Rectangle(0, 12, ClientSize.Width, 24),
            SystemColors.ControlText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var contentBounds = new RectangleF(20, 48, Math.Max(1, ClientSize.Width - 40), Math.Max(1, ClientSize.Height - 68));
        using var pen = new Pen(Color.Black, 2f);
        if (SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator)
        {
            var mobilityFrame = SymbolFrameRenderer.GetMobilityFrame(contentBounds);
            using var border = new Pen(Color.FromArgb(160, 160, 160), 1f);
            graphics.DrawRectangle(border, Rectangle.Round(mobilityFrame));
            foreach (var command in _commands)
                command.Draw(graphics, mobilityFrame, pen, Brushes.Black);
        }
        else
        {
            SymbolFrameRenderer.DrawComponentCommands(graphics, contentBounds, _commands, pen, Brushes.Black);
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
        var isModifierComponent = SymbolRole is OrbatEquipmentSymbolRole.Modifier1 or OrbatEquipmentSymbolRole.Modifier2;
        var previewRole = isModifierComponent
            ? SymbolRole
            : CompositionMode == OrbatEquipmentCompositionMode.Composite
                ? OrbatEquipmentSymbolRole.Composite
                : SymbolRole;
        var interiorFrame = SymbolFrameRenderer.GetInteriorFrame(frame, shape, IconGuideShape.FlatTopBottom);
        var symbolFrame = PhysicalDomain == SymbolPhysicalDomain.Equipment || isModifierComponent
            ? SymbolFrameRenderer.GetEquipmentComponentFrame(
                interiorFrame,
                previewRole,
                SymbolLayout,
                hasModifier1: SymbolRole == OrbatEquipmentSymbolRole.Modifier1,
                hasModifier2: SymbolRole == OrbatEquipmentSymbolRole.Modifier2)
            : interiorFrame;
        if (isModifierComponent)
        {
            symbolFrame = AdjustModifierPreviewSlot(symbolFrame, frame, shape, SymbolRole);
            symbolFrame = SymbolFrameRenderer.FitCommandsPreservingAspect(symbolFrame, _commands);
            symbolFrame = SymbolFrameRenderer.ContainComponentFrame(
                frame,
                shape,
                symbolFrame,
                _commands,
                SymbolRole,
                IconGuideShape.FlatTopBottom);
        }

        using var pen = new Pen(Color.Black, 2f);
        SymbolFrameRenderer.DrawFrame(graphics, frame, shape, _frameStatus, fillFrame: true, IconGuideShape.FlatTopBottom, fillUpperCap: FillUpperFrameCap);
        foreach (var command in _commands)
            SymbolFrameRenderer.DrawCommand(graphics, frame, SymbolFrameRenderer.GetCommandFrame(symbolFrame, shape, command), shape, command, pen, Brushes.Black, IconGuideShape.FlatTopBottom);

        if (shape == _frameShape)
        {
            using var selectedPen = new Pen(Color.FromArgb(40, 120, 220), 1.4f);
            var selection = RectangleF.Inflate(frame, 5f, 5f);
            graphics.DrawRectangle(selectedPen, Rectangle.Round(selection));
        }
    }

    private static RectangleF AdjustModifierPreviewSlot(
        RectangleF slot,
        RectangleF affiliationFrame,
        SymbolFrameShape shape,
        OrbatEquipmentSymbolRole role)
    {
        if (shape is SymbolFrameShape.Hostile or SymbolFrameShape.HostileEquipmentInFlight)
        {
            var direction = role == OrbatEquipmentSymbolRole.Modifier1 ? 1f : -1f;
            slot.Offset(0f, affiliationFrame.Height * 0.08f * direction);
        }

        if (shape is SymbolFrameShape.Unknown or SymbolFrameShape.UnknownEquipmentInFlight)
        {
            const float scale = 1.35f;
            var center = new PointF(slot.Left + slot.Width / 2f, slot.Top + slot.Height / 2f);
            slot = new RectangleF(
                center.X - slot.Width * scale / 2f,
                center.Y - slot.Height * scale / 2f,
                slot.Width * scale,
                slot.Height * scale);
        }

        return slot;
    }

    private RectangleF GetPreviewFrame(RectangleF contentBounds, SymbolFrameShape shape)
    {
        if (PhysicalDomain == SymbolPhysicalDomain.Equipment)
            return GetEquipmentPreviewFrame(contentBounds, shape);

        return GetLandUnitPreviewFrame(contentBounds, shape);
    }

    private RectangleF GetLandUnitPreviewFrame(RectangleF contentBounds, SymbolFrameShape shape)
    {
        var baseWidth = Math.Min(contentBounds.Width, contentBounds.Height);
        baseWidth = Math.Min(baseWidth * PreviewScale, Math.Min(contentBounds.Width, contentBounds.Height));
        var friendlyHeight = baseWidth / StandardFrameAspectRatio;
        var center = new PointF(contentBounds.Left + contentBounds.Width / 2f, contentBounds.Top + contentBounds.Height / 2f);

        return shape switch
        {
            SymbolFrameShape.Hostile => new RectangleF(
                center.X - baseWidth / 2f,
                center.Y - baseWidth / 2f,
                baseWidth,
                baseWidth),
            SymbolFrameShape.Neutral => new RectangleF(
                center.X - friendlyHeight / 2f,
                center.Y - friendlyHeight / 2f,
                friendlyHeight,
                friendlyHeight),
            SymbolFrameShape.Unknown => new RectangleF(
                center.X - baseWidth / 2f,
                center.Y - friendlyHeight / 2f,
                baseWidth,
                friendlyHeight),
            _ => new RectangleF(
                center.X - baseWidth / 2f,
                center.Y - friendlyHeight / 2f,
                baseWidth,
                friendlyHeight)
        };

    }

    private RectangleF GetEquipmentPreviewFrame(RectangleF contentBounds, SymbolFrameShape shape)
    {
        var guide = GetEquipmentPreviewGuide(contentBounds);
        var center = new PointF(guide.Left + guide.Width / 2f, guide.Top + guide.Height / 2f);

        if (SymbolFrameMapping.IsInFlightFrame(shape))
            return guide;

        return shape switch
        {
            SymbolFrameShape.Hostile => CenteredFrame(center, guide.Width * 1.18f, guide.Height * 1.18f),
            SymbolFrameShape.Unknown => CenteredFrame(center, guide.Width * 1.42f, guide.Height * 0.96f),
            _ => guide
        };
    }

    private RectangleF GetEquipmentPreviewGuide(RectangleF contentBounds)
    {
        var maxByWidth = contentBounds.Width / 1.5f;
        var maxByHeight = contentBounds.Height / 1.5f;
        var side = Math.Min(maxByWidth, maxByHeight);
        side = Math.Min(side * PreviewScale, Math.Min(maxByWidth, maxByHeight));
        return CenteredFrame(
            new PointF(contentBounds.Left + contentBounds.Width / 2f, contentBounds.Top + contentBounds.Height / 2f),
            side,
            side);
    }

    private static RectangleF CenteredFrame(PointF center, float width, float height) =>
        new(center.X - width / 2f, center.Y - height / 2f, width, height);

}

internal sealed record EquipmentVariantSelection(string Function, string Variant)
{
    public override string ToString() => $"{Function}  |  {Variant}";
}

internal sealed record EquipmentFunctionSelection(OrbatEquipmentFunction Value)
{
    public override string ToString() => OrbatEquipmentFunctionCatalog.GetDisplayName(Value);
}

internal sealed record EquipmentFunctionCategorySelection(OrbatEquipmentFunctionCategory Value)
{
    public override string ToString() => OrbatEquipmentFunctionCatalog.GetCategoryDisplayName(Value);
}

internal sealed class DesignerLibrarySettings
{
    public int Mode { get; set; }
    public string Folder { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
}

internal static class SymbolLibraryFileNaming
{
    private const string LibraryExtension = ".orbatsymbol.json";
    private static readonly string[] DomainPrefixes = Enum.GetNames<SymbolPhysicalDomain>()
        .Select(domain => $"{domain}.")
        .OrderByDescending(prefix => prefix.Length)
        .ToArray();
    private static readonly string[] EquipmentRolePrefixes = { "Modifier1.", "Modifier2." };

    public static string GetLogicalName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (name.EndsWith(LibraryExtension, StringComparison.OrdinalIgnoreCase))
            name = name[..^LibraryExtension.Length];
        else
            name = Path.GetFileNameWithoutExtension(name);

        foreach (var prefix in DomainPrefixes)
        {
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            name = name[prefix.Length..];
            break;
        }

        foreach (var prefix in EquipmentRolePrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return name[prefix.Length..];
        }

        return name;
    }
}
internal sealed class SymbolLibraryDefinition
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public string EquipmentFunction { get; set; } = Components.OrbatEquipmentFunction.Unspecified.ToString();
    public string Variant { get; set; } = string.Empty;
    public OrbatEquipmentSymbolRole SymbolRole { get; set; } = OrbatEquipmentSymbolRole.Composite;
    public OrbatEquipmentCompositionMode CompositionMode { get; set; } = OrbatEquipmentCompositionMode.Composite;
    public OrbatEquipmentSymbolLayout Layout { get; set; } = OrbatEquipmentSymbolLayout.CreateDefault();
    public string Modifier1Type { get; set; } = OrbatEquipmentModifier1.Unspecified.ToString();
    public string Modifier2Type { get; set; } = OrbatEquipmentModifier2.Unspecified.ToString();
    public string LandUnitModifier1Type { get; set; } = OrbatLandUnitModifier1.Unspecified.ToString();
    public string LandUnitModifier2Type { get; set; } = OrbatLandUnitModifier2.Unspecified.ToString();
    public string MobilityType { get; set; } = OrbatEquipmentMobilityMode.Unspecified.ToString();
    public SymbolAffiliation Affiliation { get; set; } = SymbolAffiliation.Friendly;
    public SymbolPhysicalDomain PhysicalDomain { get; set; } = SymbolPhysicalDomain.LandUnit;
    public SymbolFrameShape FrameShape { get; set; } = SymbolFrameShape.FriendlyUnit;
    public SymbolFrameStatus FrameStatus { get; set; } = SymbolFrameStatus.Present;
    public OrbatEquipmentOperatingState OperatingState { get; set; } = OrbatEquipmentOperatingState.Ground;
    public List<SymbolDrawCommand> Commands { get; set; } = new();

    private bool HasConsistentFrameMetadata() =>
        FrameShape == SymbolFrameMapping.GetFrameShape(Affiliation, PhysicalDomain, OperatingState);

    public SymbolAffiliation GetEffectiveAffiliation() =>
        HasConsistentFrameMetadata()
            ? Affiliation
            : SymbolFrameMapping.GetAffiliation(FrameShape);

    public SymbolPhysicalDomain GetEffectivePhysicalDomain() =>
        HasConsistentFrameMetadata()
            ? PhysicalDomain
            : SymbolFrameMapping.GetPhysicalDomain(FrameShape);

    public OrbatEquipmentOperatingState GetEffectiveOperatingState() =>
        HasConsistentFrameMetadata()
            ? OperatingState
            : SymbolFrameMapping.GetOperatingState(FrameShape);

    public SymbolFrameShape GetEffectiveFrameShape() =>
        SymbolFrameMapping.GetFrameShape(
            GetEffectiveAffiliation(),
            GetEffectivePhysicalDomain(),
            GetEffectiveOperatingState());
}

internal sealed record EquipmentModifier1Selection(OrbatEquipmentModifier1 Value)
{
    public override string ToString() => Value.GetDisplayName();
}

internal sealed record EquipmentModifier2Selection(OrbatEquipmentModifier2 Value)
{
    public override string ToString() => Value.GetDisplayName();
}

internal sealed record LandUnitModifier1Selection(OrbatLandUnitModifier1 Value)
{
    public override string ToString() => Value.GetDisplayName();
}

internal sealed record LandUnitModifier2Selection(OrbatLandUnitModifier2 Value)
{
    public override string ToString() => Value.GetDisplayName();
}

internal sealed record EquipmentMobilitySelection(OrbatEquipmentMobilityMode Value)
{
    public override string ToString() => Value.GetDisplayName();
}

internal static class SymbolFrameRenderer
{
    private const float StandardFrameAspectRatio = 1.5f;
    private const float PreviewStrokeReferenceSize = 160f;
    private const float MinimumPreviewStrokeScale = 0.3f;
    private const float MinimumPreviewStrokeWidth = 0.6f;

    public static void DrawFrame(Graphics graphics, RectangleF frame, SymbolFrameShape shape, SymbolFrameStatus status, bool fillFrame, IconGuideShape guideShape, float? strokeScale = null, bool fillUpperCap = false)
    {
        var effectiveStrokeScale = strokeScale ?? GetPreviewStrokeScale(frame);
        using var pen = new Pen(Color.Black, Math.Max(MinimumPreviewStrokeWidth, 2f * effectiveStrokeScale));
        if (status == SymbolFrameStatus.PlannedAnticipated)
            pen.DashStyle = DashStyle.Dash;

        using var path = CreatePath(frame, shape, guideShape);
        if (fillFrame)
        {
            var palette = GetPalette(shape);
            using var fill = new SolidBrush(palette.Fill);
            graphics.FillPath(fill, path);
        }

        if (fillUpperCap && SymbolFrameMapping.IsInFlightFrame(shape))
        {
            var state = graphics.Save();
            graphics.SetClip(path, CombineMode.Intersect);
            var bounds = GetIconGuideBounds(frame);
            graphics.FillRectangle(
                Brushes.Black,
                bounds.Left,
                bounds.Top,
                bounds.Width,
                bounds.Height * 0.14f);
            graphics.Restore(state);
        }

        graphics.DrawPath(pen, path);
    }

    public static void DrawCommand(Graphics graphics, RectangleF frame, RectangleF commandFrame, SymbolFrameShape shape, SymbolDrawCommand command, Pen pen, Brush brush, IconGuideShape guideShape, float? strokeScale = null)
    {
        var state = graphics.Save();
        using var path = CreatePath(frame, shape, guideShape);
        graphics.SetClip(path, CombineMode.Intersect);
        command.Draw(graphics, commandFrame, pen, brush, strokeScale ?? GetPreviewStrokeScale(frame));
        graphics.Restore(state);
    }

    private static float GetPreviewStrokeScale(RectangleF frame)
    {
        var frameSize = Math.Max(1f, Math.Min(frame.Width, frame.Height));
        return Math.Clamp(frameSize / PreviewStrokeReferenceSize, MinimumPreviewStrokeScale, 1f);
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
            SymbolFrameShape.FriendlyEquipment
                or SymbolFrameShape.FriendlyEquipmentInFlight
                or SymbolFrameShape.HostileEquipmentInFlight
                or SymbolFrameShape.NeutralEquipmentInFlight
                or SymbolFrameShape.UnknownEquipmentInFlight => GetEquipmentInteriorFrame(frame, guideShape),
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
            SymbolFrameShape.FriendlyEquipment
                or SymbolFrameShape.FriendlyEquipmentInFlight
                or SymbolFrameShape.HostileEquipmentInFlight
                or SymbolFrameShape.NeutralEquipmentInFlight
                or SymbolFrameShape.UnknownEquipmentInFlight => GetEquipmentGuideFrame(frame, guideShape),
            _ => frame
        };
    }

    public static RectangleF GetCommandFrame(RectangleF interiorFrame, SymbolFrameShape shape, SymbolDrawCommand command)
    {
        if (shape != SymbolFrameShape.Hostile || command.Kind != SymbolDrawCommandKind.Capsule)
            return interiorFrame;

        return ScaleRectangle(interiorFrame, 1.24f, 1f);
    }
    public static PointF GetEquipmentS2Anchor(
        RectangleF frame,
        SymbolFrameShape shape,
        IconGuideShape guideShape)
    {
        var guideBounds = GetIconGuideBounds(frame);
        return shape switch
        {
            SymbolFrameShape.FriendlyEquipment => GetCircleLowerLeftAnchor(
                GetGuideCircumcircleBounds(frame, guideShape)),
            SymbolFrameShape.Hostile => GetHostileLowerLeftAnchor(frame, guideShape),
            SymbolFrameShape.Neutral => new PointF(guideBounds.Left, guideBounds.Bottom),
            SymbolFrameShape.Unknown => ToAbsolute(frame, new PointF(0.26850507f, 0.86030483f)),
            SymbolFrameShape.UnknownEquipmentInFlight =>
                ToAbsolute(guideBounds, new PointF(0.28f, 0.82f)),
            SymbolFrameShape.FriendlyEquipmentInFlight
                or SymbolFrameShape.HostileEquipmentInFlight
                or SymbolFrameShape.NeutralEquipmentInFlight =>
                new PointF(guideBounds.Left, guideBounds.Bottom),
            _ => new PointF(frame.Left, frame.Bottom)
        };
    }

    private static PointF GetCircleLowerLeftAnchor(RectangleF bounds)
    {
        const float diagonal = 0.70710678f;
        var radius = bounds.Width / 2f;
        return new PointF(
            bounds.Left + radius - radius * diagonal,
            bounds.Top + radius + radius * diagonal);
    }

    private static PointF GetHostileLowerLeftAnchor(RectangleF frame, IconGuideShape guideShape)
    {
        var diamond = GetHostileDiamondPoints(frame, guideShape);
        return new PointF(
            (diamond[3].X + diamond[2].X) / 2f,
            (diamond[3].Y + diamond[2].Y) / 2f);
    }

    public static RectangleF GetEquipmentComponentFrame(
        RectangleF interiorFrame,
        OrbatEquipmentSymbolRole role,
        OrbatEquipmentSymbolLayout? layout,
        bool hasModifier1,
        bool hasModifier2)
    {
        layout ??= OrbatEquipmentSymbolLayout.CreateDefault();
        if (role == OrbatEquipmentSymbolRole.Composite)
            return interiorFrame;

        if (role == OrbatEquipmentSymbolRole.MainFunction)
        {
            var modifierCount = (hasModifier1 ? 1 : 0) + (hasModifier2 ? 1 : 0);
            var scale = modifierCount switch
            {
                0 => layout.MainScaleWithoutModifiers,
                1 => layout.MainScaleWithOneModifier,
                _ => layout.MainScaleWithTwoModifiers
            };
            var modifierHeight = Math.Clamp(layout.ModifierHeightScale, 0.01f, 1f);
            var modifierCenterOffset = Math.Clamp(
                Math.Max(layout.ModifierCenterOffset, 0.40f),
                0f,
                0.5f - modifierHeight / 2f);
            const float componentGap = 0.01f;
            if (hasModifier1 && hasModifier2)
            {
                var availableMainHeight = 2f * (modifierCenterOffset - modifierHeight / 2f - componentGap);
                scale = Math.Min(scale, Math.Max(0.01f, availableMainHeight));
            }

            var frame = ScaleRectangle(interiorFrame, scale, scale);
            var automaticOffset = hasModifier1 && !hasModifier2
                ? layout.MainSingleModifierOffset
                : hasModifier2 && !hasModifier1
                    ? -layout.MainSingleModifierOffset
                    : 0f;
            frame.Offset(
                interiorFrame.Width * layout.MainOffsetX,
                interiorFrame.Height * (layout.MainOffsetY + automaticOffset));

            if (hasModifier1 && !hasModifier2)
            {
                var minimumTop = interiorFrame.Top + interiorFrame.Height
                    * (0.5f - modifierCenterOffset + modifierHeight / 2f + componentGap);
                if (frame.Top < minimumTop)
                    frame.Offset(0f, minimumTop - frame.Top);
            }
            else if (hasModifier2 && !hasModifier1)
            {
                var maximumBottom = interiorFrame.Top + interiorFrame.Height
                    * (0.5f + modifierCenterOffset - modifierHeight / 2f - componentGap);
                if (frame.Bottom > maximumBottom)
                    frame.Offset(0f, maximumBottom - frame.Bottom);
            }

            return frame;
        }

        var modifierFrame = ScaleRectangle(interiorFrame, layout.ModifierWidthScale, layout.ModifierHeightScale);
        var direction = role == OrbatEquipmentSymbolRole.Modifier1 ? -1f : 1f;
        var safeCenterOffset = Math.Clamp(
            Math.Max(layout.ModifierCenterOffset, 0.40f),
            0f,
            0.5f - Math.Clamp(layout.ModifierHeightScale, 0.01f, 1f) / 2f);
        modifierFrame.Offset(0f, interiorFrame.Height * safeCenterOffset * direction);
        return modifierFrame;
    }

    public static RectangleF FitCommandsPreservingAspect(
        RectangleF availableFrame,
        IReadOnlyList<SymbolDrawCommand> commands)
    {
        var hasBounds = false;
        var visualBounds = RectangleF.Empty;
        foreach (var command in commands)
        {
            var commandBounds = command.GetNormalizedVisualBounds();
            if (commandBounds.Width <= 0.0001f && commandBounds.Height <= 0.0001f)
                continue;

            visualBounds = hasBounds ? Union(visualBounds, commandBounds) : commandBounds;
            hasBounds = true;
        }

        if (!hasBounds)
            return availableFrame;

        var visualWidth = Math.Max(0.0001f, visualBounds.Width);
        var visualHeight = Math.Max(0.0001f, visualBounds.Height);
        var scale = Math.Min(availableFrame.Width / visualWidth, availableFrame.Height / visualHeight);
        return new RectangleF(
            availableFrame.Left + (availableFrame.Width - visualWidth * scale) / 2f - visualBounds.Left * scale,
            availableFrame.Top + (availableFrame.Height - visualHeight * scale) / 2f - visualBounds.Top * scale,
            scale,
            scale);
    }

    public static RectangleF ContainComponentFrame(
        RectangleF affiliationFrame,
        SymbolFrameShape shape,
        RectangleF commandFrame,
        IReadOnlyList<SymbolDrawCommand> commands,
        OrbatEquipmentSymbolRole role,
        IconGuideShape guideShape)
    {
        if (commands.Count == 0
            || role is not (OrbatEquipmentSymbolRole.Modifier1 or OrbatEquipmentSymbolRole.Modifier2))
            return commandFrame;

        var normalizedBounds = commands
            .Select(command => command.GetNormalizedVisualBounds())
            .Aggregate(RectangleF.Union);
        using var affiliationPath = CreatePath(affiliationFrame, shape, guideShape);
        var direction = role == OrbatEquipmentSymbolRole.Modifier1 ? 1f : -1f;
        var step = Math.Max(1f, affiliationFrame.Height * 0.01f);
        var candidate = commandFrame;
        var bestCandidate = candidate;
        var bestVisiblePoints = -1;

        for (var attempt = 0; attempt <= 50; attempt++)
        {
            var renderedBounds = new RectangleF(
                candidate.Left + normalizedBounds.Left * candidate.Width,
                candidate.Top + normalizedBounds.Top * candidate.Height,
                normalizedBounds.Width * candidate.Width,
                normalizedBounds.Height * candidate.Height);
            renderedBounds.Inflate(1.5f, 1.5f);
            var points = new[]
            {
                new PointF(renderedBounds.Left, renderedBounds.Top),
                new PointF(renderedBounds.Left + renderedBounds.Width / 2f, renderedBounds.Top),
                new PointF(renderedBounds.Right, renderedBounds.Top),
                new PointF(renderedBounds.Left, renderedBounds.Top + renderedBounds.Height / 2f),
                new PointF(renderedBounds.Left + renderedBounds.Width / 2f, renderedBounds.Top + renderedBounds.Height / 2f),
                new PointF(renderedBounds.Right, renderedBounds.Top + renderedBounds.Height / 2f),
                new PointF(renderedBounds.Left, renderedBounds.Bottom),
                new PointF(renderedBounds.Left + renderedBounds.Width / 2f, renderedBounds.Bottom),
                new PointF(renderedBounds.Right, renderedBounds.Bottom)
            };
            var visiblePoints = points.Count(affiliationPath.IsVisible);
            if (visiblePoints == points.Length)
                return candidate;
            if (visiblePoints > bestVisiblePoints)
            {
                bestVisiblePoints = visiblePoints;
                bestCandidate = candidate;
            }

            candidate.Offset(0f, direction * step);
        }

        return bestCandidate;
    }

    public static RectangleF GetMobilityFrame(RectangleF availableFrame)
    {
        const float sourceAspectRatio = 3f;
        var width = Math.Min(availableFrame.Width, availableFrame.Height * sourceAspectRatio);
        var height = width / sourceAspectRatio;
        return new RectangleF(
            availableFrame.Left + (availableFrame.Width - width) / 2f,
            availableFrame.Top + (availableFrame.Height - height) / 2f,
            width,
            height);
    }

    public static RectangleF FitMobilityThumbnailCommands(
        RectangleF availableFrame,
        IReadOnlyList<SymbolDrawCommand> commands)
    {
        const float sourceAspectRatio = 3f;
        var hasBounds = false;
        var visualBounds = RectangleF.Empty;
        foreach (var command in commands)
        {
            var commandBounds = command.GetNormalizedVisualBounds();
            if (commandBounds.Width <= 0.0001f && commandBounds.Height <= 0.0001f)
                continue;

            visualBounds = hasBounds ? Union(visualBounds, commandBounds) : commandBounds;
            hasBounds = true;
        }

        if (!hasBounds)
            return GetMobilityFrame(availableFrame);

        var visualWidth = Math.Max(0.0001f, visualBounds.Width * sourceAspectRatio);
        var visualHeight = Math.Max(0.0001f, visualBounds.Height);
        var scale = Math.Min(availableFrame.Width / visualWidth, availableFrame.Height / visualHeight);
        return new RectangleF(
            availableFrame.Left + (availableFrame.Width - visualWidth * scale) / 2f
                - visualBounds.Left * sourceAspectRatio * scale,
            availableFrame.Top + (availableFrame.Height - visualHeight * scale) / 2f
                - visualBounds.Top * scale,
            sourceAspectRatio * scale,
            scale);
    }
    public static void DrawComponentCommands(
        Graphics graphics,
        RectangleF availableFrame,
        IReadOnlyList<SymbolDrawCommand> commands,
        Pen pen,
        Brush brush,
        float strokeScale = 1f)
    {
        var commandFrame = FitCommandsPreservingAspect(availableFrame, commands);
        foreach (var command in commands)
            command.Draw(graphics, commandFrame, pen, brush, strokeScale);
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
        return GetIconGuideBounds(frame);
    }

    private static RectangleF GetEquipmentGuideFrame(RectangleF frame, IconGuideShape guideShape)
    {
        return frame;
    }

    public static SymbolPalette GetPalette(SymbolFrameShape shape)
    {
        return shape switch
        {
            SymbolFrameShape.Hostile or SymbolFrameShape.HostileEquipmentInFlight =>
                new SymbolPalette(Color.FromArgb(255, 128, 128), Color.FromArgb(255, 0, 0)),
            SymbolFrameShape.Neutral or SymbolFrameShape.NeutralEquipmentInFlight =>
                new SymbolPalette(Color.FromArgb(170, 255, 170), Color.FromArgb(0, 255, 0)),
            SymbolFrameShape.Unknown or SymbolFrameShape.UnknownEquipmentInFlight =>
                new SymbolPalette(Color.FromArgb(255, 255, 128), Color.FromArgb(255, 255, 0)),
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
            case SymbolFrameShape.FriendlyEquipmentInFlight:
                AddFriendlyInFlightFrame(path, frame);
                break;
            case SymbolFrameShape.HostileEquipmentInFlight:
                AddHostileInFlightFrame(path, frame);
                break;
            case SymbolFrameShape.NeutralEquipmentInFlight:
                AddNeutralInFlightFrame(path, frame);
                break;
            case SymbolFrameShape.UnknownEquipmentInFlight:
                AddUnknownInFlightFrame(path, frame);
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

    private static void AddFriendlyInFlightFrame(GraphicsPath path, RectangleF frame)
    {
        var bounds = GetIconGuideBounds(frame);
        var centerY = bounds.Top + bounds.Height / 2f;
        path.StartFigure();
        path.AddLine(bounds.Left, bounds.Bottom, bounds.Left, centerY);
        path.AddArc(bounds, 180f, 180f);
        path.AddLine(bounds.Right, centerY, bounds.Right, bounds.Bottom);
    }

    private static void AddHostileInFlightFrame(GraphicsPath path, RectangleF frame)
    {
        var bounds = GetIconGuideBounds(frame);
        var shoulderY = bounds.Top + bounds.Height * 0.36f;
        path.StartFigure();
        path.AddLines(new[]
        {
            new PointF(bounds.Left, bounds.Bottom),
            new PointF(bounds.Left, shoulderY),
            new PointF(bounds.Left + bounds.Width / 2f, bounds.Top),
            new PointF(bounds.Right, shoulderY),
            new PointF(bounds.Right, bounds.Bottom)
        });
    }

    private static void AddNeutralInFlightFrame(GraphicsPath path, RectangleF frame)
    {
        var bounds = GetIconGuideBounds(frame);
        path.StartFigure();
        path.AddLines(new[]
        {
            new PointF(bounds.Left, bounds.Bottom),
            new PointF(bounds.Left, bounds.Top),
            new PointF(bounds.Right, bounds.Top),
            new PointF(bounds.Right, bounds.Bottom)
        });
    }

    private static void AddUnknownInFlightFrame(GraphicsPath path, RectangleF frame)
    {
        var bounds = GetIconGuideBounds(frame);
        path.StartFigure();
        path.AddBezier(
            ToAbsolute(bounds, new PointF(0.28f, 0.82f)),
            ToAbsolute(bounds, new PointF(-0.08f, 0.98f)),
            ToAbsolute(bounds, new PointF(-0.08f, 0.28f)),
            ToAbsolute(bounds, new PointF(0.28f, 0.34f)));
        path.AddBezier(
            ToAbsolute(bounds, new PointF(0.28f, 0.34f)),
            ToAbsolute(bounds, new PointF(0.18f, -0.08f)),
            ToAbsolute(bounds, new PointF(0.82f, -0.08f)),
            ToAbsolute(bounds, new PointF(0.72f, 0.34f)));
        path.AddBezier(
            ToAbsolute(bounds, new PointF(0.72f, 0.34f)),
            ToAbsolute(bounds, new PointF(1.08f, 0.28f)),
            ToAbsolute(bounds, new PointF(1.08f, 0.98f)),
            ToAbsolute(bounds, new PointF(0.72f, 0.82f)));
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

internal sealed record SymbolCanvasSnapshot(List<SymbolDrawCommand> Commands, int SelectedIndex, List<int> SelectedIndices);

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
    public float RotationDegrees { get; set; }
    public string? GroupId { get; set; }
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

    public static SymbolDrawCommand Polyline(IEnumerable<SymbolPoint> points)
    {
        var pointList = points.ToList();
        return new()
        {
            Kind = SymbolDrawCommandKind.Polyline,
            Start = pointList.Count > 0 ? pointList[0] : default,
            End = pointList.Count > 0 ? pointList[^1] : default,
            Points = pointList
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

    public static SymbolDrawCommand SineWave() =>
        SineWave(new PointF(0f, 0.32f), new PointF(1f, 0.68f));

    public static SymbolDrawCommand SineWave(PointF start, PointF end) =>
        new()
        {
            Kind = SymbolDrawCommandKind.SineWave,
            Start = new SymbolPoint(start),
            End = new SymbolPoint(end)
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
            RotationDegrees = RotationDegrees,
            GroupId = GroupId,
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
        && RotationDegrees.Equals(other.RotationDegrees)
        && GroupId == other.GroupId
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

    public void ScaleAndTranslate(float scale, SymbolPoint offset)
    {
        TransformPoints(point => new SymbolPoint(point.X * scale + offset.X, point.Y * scale + offset.Y));
        Radius *= scale;
        FontSize *= scale;
    }
    public void RotateClockwise(SymbolPoint center) => Rotate(90f, center);

    public void Rotate(float degrees, SymbolPoint center)
    {
        var ownBounds = GetUnrotatedNormalizedVisualBounds();
        var ownCenter = new SymbolPoint(ownBounds.Left + ownBounds.Width / 2f, ownBounds.Top + ownBounds.Height / 2f);
        var rotatedCenter = RotatePoint(ownCenter, center, degrees);
        Move(new SymbolPoint(rotatedCenter.X - ownCenter.X, rotatedCenter.Y - ownCenter.Y));
        RotationDegrees = NormalizeDegrees(RotationDegrees + degrees);
    }

    public void MirrorHorizontal(float centerX)
    {
        TransformPoints(point => new SymbolPoint(centerX * 2f - point.X, point.Y));
        RotationDegrees = NormalizeDegrees(-RotationDegrees);
    }

    public void MirrorVertical(float centerY)
    {
        TransformPoints(point => new SymbolPoint(point.X, centerY * 2f - point.Y));
        RotationDegrees = NormalizeDegrees(-RotationDegrees);
    }

    public void SetPoint(DragTarget target, SymbolPoint point)
    {
        point = InverseRotatePoint(point);
        switch (target)
        {
            case DragTarget.Start:
                Start = point;
                if (Kind is SymbolDrawCommandKind.Dot or SymbolDrawCommandKind.Text)
                    End = point;
                if (Kind == SymbolDrawCommandKind.Polyline && Points.Count > 0)
                    Points[0] = point;
                break;
            case DragTarget.End:
                End = point;
                if (Kind == SymbolDrawCommandKind.Polyline && Points.Count > 0)
                    Points[^1] = point;
                if (Kind == SymbolDrawCommandKind.Circle)
                    Radius = Distance(Start, End);
                break;
            case DragTarget.Control1:
                Control1 = point;
                break;
            case DragTarget.Control2:
                Control2 = point;
                break;
            case DragTarget.TopLeft:
            case DragTarget.TopRight:
            case DragTarget.BottomRight:
            case DragTarget.BottomLeft:
                SetRectangleCorner(target, point, GetNormalizedRect());
                break;
            case DragTarget.Top:
            case DragTarget.Right:
            case DragTarget.Bottom:
            case DragTarget.Left:
                SetRectangleEdge(target, point, GetNormalizedRect());
                break;
            case DragTarget.Peak:
                var updated = ThreePointArc(Start, point, End);
                Control1 = updated.Control1;
                Control2 = updated.Control2;
                break;
        }
    }

    public void ResizeFrom(SymbolDrawCommand source, DragTarget target, SymbolPoint pointer)
    {
        var bounds = source.GetNormalizedRect();
        var sourceCenter = new SymbolPoint(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
        var localPointer = Math.Abs(source.RotationDegrees) < 0.001f
            ? pointer
            : RotatePoint(pointer, sourceCenter, -source.RotationDegrees);
        const float minimumExtent = 0.001f;

        float left = bounds.Left;
        float top = bounds.Top;
        float right = bounds.Right;
        float bottom = bounds.Bottom;
        SymbolPoint fixedAnchor;

        switch (target)
        {
            case DragTarget.TopLeft:
                left = Math.Min(localPointer.X, right - minimumExtent);
                top = Math.Min(localPointer.Y, bottom - minimumExtent);
                fixedAnchor = new SymbolPoint(right, bottom);
                break;
            case DragTarget.Top:
                top = Math.Min(localPointer.Y, bottom - minimumExtent);
                fixedAnchor = new SymbolPoint((left + right) / 2f, bottom);
                break;
            case DragTarget.TopRight:
                right = Math.Max(localPointer.X, left + minimumExtent);
                top = Math.Min(localPointer.Y, bottom - minimumExtent);
                fixedAnchor = new SymbolPoint(left, bottom);
                break;
            case DragTarget.Right:
                right = Math.Max(localPointer.X, left + minimumExtent);
                fixedAnchor = new SymbolPoint(left, (top + bottom) / 2f);
                break;
            case DragTarget.BottomRight:
                right = Math.Max(localPointer.X, left + minimumExtent);
                bottom = Math.Max(localPointer.Y, top + minimumExtent);
                fixedAnchor = new SymbolPoint(left, top);
                break;
            case DragTarget.Bottom:
                bottom = Math.Max(localPointer.Y, top + minimumExtent);
                fixedAnchor = new SymbolPoint((left + right) / 2f, top);
                break;
            case DragTarget.BottomLeft:
                left = Math.Min(localPointer.X, right - minimumExtent);
                bottom = Math.Max(localPointer.Y, top + minimumExtent);
                fixedAnchor = new SymbolPoint(right, top);
                break;
            case DragTarget.Left:
                left = Math.Min(localPointer.X, right - minimumExtent);
                fixedAnchor = new SymbolPoint(right, (top + bottom) / 2f);
                break;
            default:
                return;
        }

        var candidateCenter = new SymbolPoint((left + right) / 2f, (top + bottom) / 2f);
        var fixedWorld = Math.Abs(source.RotationDegrees) < 0.001f
            ? fixedAnchor
            : RotatePoint(fixedAnchor, sourceCenter, source.RotationDegrees);
        var candidateFixedWorld = Math.Abs(source.RotationDegrees) < 0.001f
            ? fixedAnchor
            : RotatePoint(fixedAnchor, candidateCenter, source.RotationDegrees);
        var offset = new SymbolPoint(
            fixedWorld.X - candidateFixedWorld.X,
            fixedWorld.Y - candidateFixedWorld.Y);

        Start = new SymbolPoint(left + offset.X, top + offset.Y);
        End = new SymbolPoint(right + offset.X, bottom + offset.Y);
        RotationDegrees = source.RotationDegrees;
    }
    private void SetRectangleCorner(DragTarget target, SymbolPoint point, RectangleF bounds)
    {
        const float minimumExtent = 0.001f;
        point = target switch
        {
            DragTarget.TopLeft => new SymbolPoint(Math.Min(point.X, bounds.Right - minimumExtent), Math.Min(point.Y, bounds.Bottom - minimumExtent)),
            DragTarget.TopRight => new SymbolPoint(Math.Max(point.X, bounds.Left + minimumExtent), Math.Min(point.Y, bounds.Bottom - minimumExtent)),
            DragTarget.BottomRight => new SymbolPoint(Math.Max(point.X, bounds.Left + minimumExtent), Math.Max(point.Y, bounds.Top + minimumExtent)),
            DragTarget.BottomLeft => new SymbolPoint(Math.Min(point.X, bounds.Right - minimumExtent), Math.Max(point.Y, bounds.Top + minimumExtent)),
            _ => point
        };

        Start = target switch
        {
            DragTarget.TopLeft => point,
            DragTarget.TopRight => new SymbolPoint(bounds.Left, point.Y),
            DragTarget.BottomRight => new SymbolPoint(bounds.Left, bounds.Top),
            DragTarget.BottomLeft => new SymbolPoint(point.X, bounds.Top),
            _ => Start
        };
        End = target switch
        {
            DragTarget.TopLeft => new SymbolPoint(bounds.Right, bounds.Bottom),
            DragTarget.TopRight => new SymbolPoint(point.X, bounds.Bottom),
            DragTarget.BottomRight => point,
            DragTarget.BottomLeft => new SymbolPoint(bounds.Right, point.Y),
            _ => End
        };
    }

    private void SetRectangleEdge(DragTarget target, SymbolPoint point, RectangleF bounds)
    {
        const float minimumExtent = 0.001f;
        point = target switch
        {
            DragTarget.Top => new SymbolPoint(point.X, Math.Min(point.Y, bounds.Bottom - minimumExtent)),
            DragTarget.Right => new SymbolPoint(Math.Max(point.X, bounds.Left + minimumExtent), point.Y),
            DragTarget.Bottom => new SymbolPoint(point.X, Math.Max(point.Y, bounds.Top + minimumExtent)),
            DragTarget.Left => new SymbolPoint(Math.Min(point.X, bounds.Right - minimumExtent), point.Y),
            _ => point
        };

        Start = target switch
        {
            DragTarget.Top => new SymbolPoint(bounds.Left, point.Y),
            DragTarget.Left => new SymbolPoint(point.X, bounds.Top),
            _ => new SymbolPoint(bounds.Left, bounds.Top)
        };
        End = target switch
        {
            DragTarget.Right => new SymbolPoint(point.X, bounds.Bottom),
            DragTarget.Bottom => new SymbolPoint(bounds.Right, point.Y),
            _ => new SymbolPoint(bounds.Right, bounds.Bottom)
        };
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
        if (Kind is SymbolDrawCommandKind.Rectangle or SymbolDrawCommandKind.SineWave)
        {
            var rect = GetNormalizedRect();
            yield return new SymbolHandle(DragTarget.TopLeft, RotateOwnPoint(new SymbolPoint(rect.Left, rect.Top)));
            yield return new SymbolHandle(DragTarget.Top, RotateOwnPoint(new SymbolPoint(rect.Left + rect.Width / 2f, rect.Top)));
            yield return new SymbolHandle(DragTarget.TopRight, RotateOwnPoint(new SymbolPoint(rect.Right, rect.Top)));
            yield return new SymbolHandle(DragTarget.Right, RotateOwnPoint(new SymbolPoint(rect.Right, rect.Top + rect.Height / 2f)));
            yield return new SymbolHandle(DragTarget.BottomRight, RotateOwnPoint(new SymbolPoint(rect.Right, rect.Bottom)));
            yield return new SymbolHandle(DragTarget.Bottom, RotateOwnPoint(new SymbolPoint(rect.Left + rect.Width / 2f, rect.Bottom)));
            yield return new SymbolHandle(DragTarget.BottomLeft, RotateOwnPoint(new SymbolPoint(rect.Left, rect.Bottom)));
            yield return new SymbolHandle(DragTarget.Left, RotateOwnPoint(new SymbolPoint(rect.Left, rect.Top + rect.Height / 2f)));
            yield break;
        }

        yield return new SymbolHandle(DragTarget.Start, RotateOwnPoint(Start));
        if (Kind is not SymbolDrawCommandKind.Dot and not SymbolDrawCommandKind.Text)
            yield return new SymbolHandle(DragTarget.End, RotateOwnPoint(End));
        if (Kind == SymbolDrawCommandKind.Bezier)
        {
            yield return new SymbolHandle(DragTarget.Peak, RotateOwnPoint(new SymbolPoint(EvaluateBezier(0.5f))));
            yield return new SymbolHandle(DragTarget.Control1, RotateOwnPoint(Control1));
            yield return new SymbolHandle(DragTarget.Control2, RotateOwnPoint(Control2));
        }
    }

    public IEnumerable<PointF> GetSnapPoints()
    {
        if (Kind is SymbolDrawCommandKind.Rectangle or SymbolDrawCommandKind.SineWave)
        {
            var rect = GetNormalizedRect();
            yield return RotateOwnPoint(new SymbolPoint(rect.Left, rect.Top));
            yield return RotateOwnPoint(new SymbolPoint(rect.Right, rect.Top));
            yield return RotateOwnPoint(new SymbolPoint(rect.Right, rect.Bottom));
            yield return RotateOwnPoint(new SymbolPoint(rect.Left, rect.Bottom));
            yield return RotateOwnPoint(new SymbolPoint(rect.Left + rect.Width / 2f, rect.Top + rect.Height / 2f));
            yield break;
        }

        yield return RotateOwnPoint(Start);
        yield return RotateOwnPoint(End);
        if (Kind == SymbolDrawCommandKind.Bezier)
        {
            yield return RotateOwnPoint(Control1);
            yield return RotateOwnPoint(Control2);
        }
    }

    public IEnumerable<SymbolSegment> GetSegments()
    {
        if (Kind is SymbolDrawCommandKind.Path or SymbolDrawCommandKind.Polyline)
        {
            for (var index = 0; index < Points.Count - 1; index++)
                yield return new SymbolSegment(Points[index], Points[index + 1]);
            if (Kind == SymbolDrawCommandKind.Path && Points.Count > 2 && Distance(Points[0], Points[^1]) > 0.0001f)
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
        mousePoint = InverseRotatePoint(mousePoint, frame);
        return Kind switch
        {
            SymbolDrawCommandKind.Line => DistanceToSegment(mousePoint, ToAbsolute(frame, Start), ToAbsolute(frame, End)) <= threshold,
            SymbolDrawCommandKind.Bezier => HitTestBezier(mousePoint, frame, threshold),
            SymbolDrawCommandKind.Circle => Distance(mousePoint, ToAbsolute(frame, Start)) <= Radius * Math.Min(frame.Width, frame.Height) + threshold,
            SymbolDrawCommandKind.Dot => Distance(mousePoint, ToAbsolute(frame, Start)) <= Radius * Math.Min(frame.Width, frame.Height) + threshold,
            SymbolDrawCommandKind.Text => Distance(mousePoint, ToAbsolute(frame, Start)) <= Math.Max(24f, GetScaledFontSize(frame) * 1.5f),
            SymbolDrawCommandKind.Path => HitTestPath(mousePoint, frame, threshold),
            SymbolDrawCommandKind.Polyline => GetSegments().Any(segment =>
                DistanceToSegment(mousePoint, ToAbsolute(frame, segment.Start), ToAbsolute(frame, segment.End)) <= threshold),
            _ => ToRectangle(frame).Contains(mousePoint) || DistanceToRect(mousePoint, ToRectangle(frame)) <= threshold
        };
    }

    public void Draw(Graphics graphics, RectangleF frame, Pen pen, Brush brush, float strokeScale = 1f)
    {
        using var commandPen = CreateStrokePen(pen, strokeScale);
        var graphicsState = graphics.Save();
        if (Math.Abs(RotationDegrees) > 0.001f)
        {
            var rotationCenter = ToAbsolute(frame, GetUnrotatedCenter());
            graphics.TranslateTransform(rotationCenter.X, rotationCenter.Y);
            graphics.RotateTransform(RotationDegrees);
            graphics.TranslateTransform(-rotationCenter.X, -rotationCenter.Y);
        }
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
            case SymbolDrawCommandKind.Polyline:
                if (Points.Count > 1)
                    graphics.DrawLines(commandPen, Points.Select(point => ToAbsolute(frame, point)).ToArray());
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
            case SymbolDrawCommandKind.SineWave:
                DrawSineWave(graphics, commandPen, ToRectangle(frame));
                break;
        }
        graphics.Restore(graphicsState);
    }

    private Pen CreateStrokePen(Pen basePen, float strokeScale)
    {
        strokeScale = Math.Clamp(strokeScale, 0.3f, 1f);
        var selectedOffset = Math.Max(0f, basePen.Width - 2f) * strokeScale;
        var scaledStrokeWidth = StrokeWidth * strokeScale + selectedOffset;
        return new Pen(basePen.Color, Math.Max(0.6f, scaledStrokeWidth))
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
            SymbolDrawCommandKind.Polyline => $"{Kind} {Points.Count} points",
            _ => $"{Kind} {FormatPoint(Start)} to {FormatPoint(End)}"
        };
    }

    public RectangleF GetNormalizedVisualBounds()
    {
        var bounds = GetUnrotatedNormalizedVisualBounds();
        if (Math.Abs(RotationDegrees) < 0.001f)
            return bounds;

        var center = new SymbolPoint(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
        return GetPointsBounds(new[]
        {
            RotatePoint(new SymbolPoint(bounds.Left, bounds.Top), center, RotationDegrees),
            RotatePoint(new SymbolPoint(bounds.Right, bounds.Top), center, RotationDegrees),
            RotatePoint(new SymbolPoint(bounds.Right, bounds.Bottom), center, RotationDegrees),
            RotatePoint(new SymbolPoint(bounds.Left, bounds.Bottom), center, RotationDegrees)
        });
    }

    private RectangleF GetUnrotatedNormalizedVisualBounds()
    {
        return Kind switch
        {
            SymbolDrawCommandKind.Circle => RectangleF.FromLTRB(Start.X - Radius, Start.Y - Radius, Start.X + Radius, Start.Y + Radius),
            SymbolDrawCommandKind.Dot => RectangleF.FromLTRB(Start.X - Radius, Start.Y - Radius, Start.X + Radius, Start.Y + Radius),
            SymbolDrawCommandKind.Text => GetTextNormalizedBounds(),
            SymbolDrawCommandKind.Path or SymbolDrawCommandKind.Polyline => GetPointsBounds(Points),
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
            SymbolDrawCommandKind.Polyline =>
                PolylineCode(graphics, strokePen, bounds),
            SymbolDrawCommandKind.Dot =>
                $"{graphics}.FillEllipse({brush}, {PointCode(bounds, Start)}.X - {RadiusCode(bounds)}, {PointCode(bounds, Start)}.Y - {RadiusCode(bounds)}, {RadiusCode(bounds)} * 2f, {RadiusCode(bounds)} * 2f);",
            SymbolDrawCommandKind.Text =>
                $"{{\r\n    var textValue = \"{EscapeCSharpString(Text)}\";\r\n    var textLocation = {PointCode(bounds, Start)};\r\n    var textSize = {bounds}.Height * {Format(FontSize / 100f)}f;\r\n    var textWidth = Math.Min({bounds}.Width, Math.Max({bounds}.Width * 0.45f, Math.Max(1, textValue.Length) * textSize * 1.15f));\r\n    var textHeight = textSize * 1.6f;\r\n    using var textFont = new Font(font.FontFamily, textSize, FontStyle.Bold, GraphicsUnit.Pixel);\r\n    using var textFormat = new StringFormat {{ Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap }};\r\n    {graphics}.DrawString(textValue, textFont, {brush}, new RectangleF(textLocation.X - textWidth / 2f, textLocation.Y - textHeight / 2f, textWidth, textHeight), textFormat);\r\n}}",
            SymbolDrawCommandKind.Arc =>
                $"{graphics}.DrawArc({strokePen}, {RectCode(bounds)}, 200f, 140f);",
            SymbolDrawCommandKind.Bezier =>
                $"{graphics}.DrawBezier({strokePen}, {PointCode(bounds, Start)}, {PointCode(bounds, Control1)}, {PointCode(bounds, Control2)}, {PointCode(bounds, End)});",
            SymbolDrawCommandKind.SineWave =>
                SineWaveCode(graphics, strokePen, bounds),
            _ => string.Empty
        };

        if (Math.Abs(RotationDegrees) > 0.001f)
            commandCode = WrapRotationCode(commandCode, graphics, bounds);

        if (!UsesStroke || Math.Abs(StrokeWidth - 2f) < 0.001f)
            return commandCode;

        return $"{{\r\n    using var strokePen = (Pen){pen}.Clone();\r\n    strokePen.Width = {Format(StrokeWidth)}f;\r\n    {IndentCode(commandCode, 4)}\r\n}}";
    }

    private string WrapRotationCode(string commandCode, string graphics, string bounds)
    {
        var center = GetUnrotatedCenter();
        var newline = Environment.NewLine;
        return "{" + newline
            + $"    var rotationState = {graphics}.Save();" + newline
            + $"    var rotationCenter = {PointCode(bounds, center)};" + newline
            + $"    {graphics}.TranslateTransform(rotationCenter.X, rotationCenter.Y);" + newline
            + $"    {graphics}.RotateTransform({Format(RotationDegrees)}f);" + newline
            + $"    {graphics}.TranslateTransform(-rotationCenter.X, -rotationCenter.Y);" + newline
            + IndentCode(commandCode, 4) + newline
            + $"    {graphics}.Restore(rotationState);" + newline
            + "}";
    }
    [JsonIgnore]
    public bool UsesStroke => Kind is not SymbolDrawCommandKind.Dot and not SymbolDrawCommandKind.Text;

    private SymbolPoint GetUnrotatedCenter()
    {
        var bounds = GetUnrotatedNormalizedVisualBounds();
        return new SymbolPoint(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
    }

    private SymbolPoint RotateOwnPoint(SymbolPoint point) =>
        Math.Abs(RotationDegrees) < 0.001f ? point : RotatePoint(point, GetUnrotatedCenter(), RotationDegrees);

    private SymbolPoint InverseRotatePoint(SymbolPoint point) =>
        Math.Abs(RotationDegrees) < 0.001f ? point : RotatePoint(point, GetUnrotatedCenter(), -RotationDegrees);

    private Point InverseRotatePoint(Point point, RectangleF frame)
    {
        if (Math.Abs(RotationDegrees) < 0.001f)
            return point;

        var center = ToAbsolute(frame, GetUnrotatedCenter());
        var rotated = RotatePoint(new SymbolPoint(point.X, point.Y), new SymbolPoint(center), -RotationDegrees);
        return Point.Round(rotated);
    }

    private static SymbolPoint RotatePoint(SymbolPoint point, SymbolPoint center, float degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);
        var dx = point.X - center.X;
        var dy = point.Y - center.Y;
        return new SymbolPoint(
            center.X + dx * cosine - dy * sine,
            center.Y + dx * sine + dy * cosine);
    }

    private static float NormalizeDegrees(float degrees)
    {
        degrees %= 360f;
        return degrees <= -180f ? degrees + 360f : degrees > 180f ? degrees - 360f : degrees;
    }
    private static void DrawCapsule(Graphics graphics, Pen pen, Brush brush, RectangleF rect, bool filled)
    {
        var height = Math.Min(rect.Height, rect.Width);
        if (height <= 0.01f || rect.Width <= 0.01f)
            return;
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

    private static void DrawSineWave(Graphics graphics, Pen pen, RectangleF frame)
    {
        var centerY = frame.Top + frame.Height / 2f;
        var amplitude = frame.Height * 0.5f;
        const int cycles = 4;
        const int sampleCount = 65;
        var points = new PointF[sampleCount];
        for (var index = 0; index < sampleCount; index++)
        {
            var t = index / (float)(sampleCount - 1);
            points[index] = new PointF(
                frame.Left + frame.Width * t,
                centerY - (float)Math.Sin(t * Math.PI * 2d * cycles) * amplitude);
        }

        graphics.DrawCurve(pen, points, 0.25f);
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
        var height = Math.Clamp(FontSize, 4f, 160f) / 100f;
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

    private string SineWaveCode(string graphics, string pen, string bounds)
    {
        var newline = Environment.NewLine;
        return "{" + newline
            + $"    var waveBounds = {RectCode(bounds)};" + newline
            + "    var wavePoints = Enumerable.Range(0, 65).Select(index =>" + newline
            + "    {" + newline
            + "        var t = index / 64f;" + newline
            + "        return new PointF(" + newline
            + "            waveBounds.Left + waveBounds.Width * t," + newline
            + "            waveBounds.Top + waveBounds.Height / 2f - (float)Math.Sin(t * Math.PI * 8d) * waveBounds.Height * 0.5f);" + newline
            + "    }).ToArray();" + newline
            + $"    {graphics}.DrawCurve({pen}, wavePoints, 0.25f);" + newline
            + "}";
    }
    private string PolylineCode(string graphics, string pen, string bounds)
    {
        if (Points.Count < 2)
            return string.Empty;

        var newline = Environment.NewLine;
        var pointLines = string.Join(
            "," + newline + "        ",
            Points.Select(point => PointCode(bounds, point)));
        return "{" + newline
            + $"    {graphics}.DrawLines({pen}, new[]" + newline
            + "    {" + newline
            + $"        {pointLines}" + newline
            + "    });" + newline
            + "}";
    }

    private float GetScaledFontSize(RectangleF frame) =>
        Math.Clamp(FontSize, 4f, 160f) / 100f * frame.Height;

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
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
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
    Path,
    Polyline,
    SineWave
}

internal static class BuiltInSymbolLibrary
{
    public static IReadOnlyList<SymbolDrawCommand> Create(OrbatEquipmentMobilityMode mode)
    {
        return mode switch
        {
            OrbatEquipmentMobilityMode.Wheeled => MobilityAxle(2),
            OrbatEquipmentMobilityMode.WheeledCrossCountry => MobilityAxle(3),
            OrbatEquipmentMobilityMode.Tracked => new[]
            {
                SymbolDrawCommand.Capsule(new PointF(0.10f, 0.24f), new PointF(0.90f, 0.76f))
            },
            OrbatEquipmentMobilityMode.WheeledTracked => new[]
            {
                MobilityCircle(0.18f, 0.5f, 0.18f),
                SymbolDrawCommand.Capsule(new PointF(0.36f, 0.25f), new PointF(0.92f, 0.75f))
            },
            OrbatEquipmentMobilityMode.Towed => new[]
            {
                MobilityCircle(0.16f, 0.5f, 0.17f),
                MobilityCircle(0.84f, 0.5f, 0.17f),
                SymbolDrawCommand.Line(new PointF(0.16f, 0.5f), new PointF(0.84f, 0.5f))
            },
            OrbatEquipmentMobilityMode.Railway => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.06f, 0.46f), new PointF(0.94f, 0.46f)),
                MobilityCircle(0.16f, 0.58f, 0.12f),
                MobilityCircle(0.28f, 0.58f, 0.12f),
                MobilityCircle(0.72f, 0.58f, 0.12f),
                MobilityCircle(0.84f, 0.58f, 0.12f)
            },
            OrbatEquipmentMobilityMode.OverSnow => new[]
            {
                MobilityBezier(
                    new PointF(0.14f, 0.18f),
                    new PointF(0.18f, 0.72f),
                    new PointF(0.24f, 0.72f),
                    new PointF(0.88f, 0.72f))
            },
            OrbatEquipmentMobilityMode.Sled => new[]
            {
                MobilityBezier(
                    new PointF(0.14f, 0.18f),
                    new PointF(0.18f, 0.72f),
                    new PointF(0.24f, 0.72f),
                    new PointF(0.32f, 0.72f)),
                SymbolDrawCommand.Line(new PointF(0.32f, 0.72f), new PointF(0.76f, 0.72f)),
                MobilityBezier(
                    new PointF(0.76f, 0.72f),
                    new PointF(0.84f, 0.72f),
                    new PointF(0.86f, 0.38f),
                    new PointF(0.88f, 0.18f))
            },
            OrbatEquipmentMobilityMode.PackAnimals => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.12f, 0.82f), new PointF(0.32f, 0.18f)),
                SymbolDrawCommand.Line(new PointF(0.32f, 0.18f), new PointF(0.50f, 0.78f)),
                SymbolDrawCommand.Line(new PointF(0.50f, 0.78f), new PointF(0.68f, 0.18f)),
                SymbolDrawCommand.Line(new PointF(0.68f, 0.18f), new PointF(0.88f, 0.82f))
            },
            OrbatEquipmentMobilityMode.Barge => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.12f, 0.38f), new PointF(0.88f, 0.38f)),
                MobilityBezier(
                    new PointF(0.12f, 0.38f),
                    new PointF(0.24f, 1.028f),
                    new PointF(0.76f, 1.028f),
                    new PointF(0.88f, 0.38f))
            },
            OrbatEquipmentMobilityMode.Amphibious => new[]
            {
                SymbolDrawCommand.SineWave()
            },
            _ => Array.Empty<SymbolDrawCommand>()
        };
    }

    private static IReadOnlyList<SymbolDrawCommand> MobilityAxle(int wheelCount)
    {
        var commands = new List<SymbolDrawCommand>
        {
            SymbolDrawCommand.Line(new PointF(0.12f, 0.45f), new PointF(0.88f, 0.45f))
        };
        for (var index = 0; index < wheelCount; index++)
        {
            var x = wheelCount == 1 ? 0.5f : 0.12f + 0.76f * index / (wheelCount - 1f);
            commands.Add(MobilityCircle(x, 0.61f, 0.16f));
        }

        return commands;
    }

    private static SymbolDrawCommand MobilityCircle(float centerX, float centerY, float radius) =>
        new()
        {
            Kind = SymbolDrawCommandKind.Circle,
            Start = new SymbolPoint(centerX, centerY),
            End = new SymbolPoint(centerX + radius, centerY),
            Radius = radius
        };

    private static SymbolDrawCommand MobilityBezier(PointF start, PointF control1, PointF control2, PointF end) =>
        new()
        {
            Kind = SymbolDrawCommandKind.Bezier,
            Start = new SymbolPoint(start),
            End = new SymbolPoint(end),
            Control1 = new SymbolPoint(control1),
            Control2 = new SymbolPoint(control2)
        };

    public static IReadOnlyList<SymbolDrawCommand> Create(OrbatLandUnitModifier1 modifier) =>
        Enum.TryParse<OrbatEquipmentModifier1>(modifier.ToString(), out var equipmentModifier)
            ? Create(equipmentModifier).Select(command => command.Clone()).ToList()
            : Array.Empty<SymbolDrawCommand>();

    public static IReadOnlyList<SymbolDrawCommand> Create(OrbatLandUnitModifier2 modifier) =>
        Enum.TryParse<OrbatEquipmentModifier2>(modifier.ToString(), out var equipmentModifier)
            ? Create(equipmentModifier).Select(command => command.Clone()).ToList()
            : Array.Empty<SymbolDrawCommand>();

    public static IReadOnlyList<SymbolDrawCommand> Create(OrbatEquipmentModifier2 modifier)
    {
        var text = modifier.GetSymbolText();
        if (!string.IsNullOrWhiteSpace(text))
            return new[] { SymbolDrawCommand.TextCommand(new PointF(0.5f, 0.5f), text, 108f) };

        return modifier switch
        {
            OrbatEquipmentModifier2.ArmoredTracked => new[]
            {
                SymbolDrawCommand.Capsule(new PointF(0.08f, 0.24f), new PointF(0.92f, 0.76f))
            },
            OrbatEquipmentModifier2.Amphibious => new[]
            {
                SymbolDrawCommand.SineWave()
            },
            OrbatEquipmentModifier2.Launcher => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.3f, 0.5f), new PointF(0.7f, 0.5f)).WithStrokeWidth(6f)
            },
            OrbatEquipmentModifier2.PackAnimal => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.12f, 0.82f), new PointF(0.32f, 0.18f)),
                SymbolDrawCommand.Line(new PointF(0.32f, 0.18f), new PointF(0.5f, 0.78f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.78f), new PointF(0.68f, 0.18f)),
                SymbolDrawCommand.Line(new PointF(0.68f, 0.18f), new PointF(0.88f, 0.82f))
            },
            OrbatEquipmentModifier2.Rail => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.08f, 0.34f), new PointF(0.92f, 0.34f)).WithStrokeWidth(4f),
                Wheel(0.18f), Wheel(0.29f), Wheel(0.71f), Wheel(0.82f)
            },
            OrbatEquipmentModifier2.TractorTrailer => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.08f, 0.34f), new PointF(0.92f, 0.34f)).WithStrokeWidth(4f),
                Wheel(0.18f), Wheel(0.29f), Wheel(0.82f)
            },
            OrbatEquipmentModifier2.WheeledHighMobility => new[]
            {
                LargeWheel(0.22f), LargeWheel(0.5f), LargeWheel(0.78f)
            },
            OrbatEquipmentModifier2.WheeledStandardMobility => new[]
            {
                LargeWheel(0.24f), LargeWheel(0.76f)
            },
            _ => Array.Empty<SymbolDrawCommand>()
        };
    }

    private static SymbolDrawCommand Wheel(float centerX) =>
        SymbolDrawCommand.Ellipse(new PointF(centerX - 0.055f, 0.48f), new PointF(centerX + 0.055f, 0.76f));

    private static SymbolDrawCommand LargeWheel(float centerX) =>
        SymbolDrawCommand.Ellipse(new PointF(centerX - 0.09f, 0.27f), new PointF(centerX + 0.09f, 0.73f));

    public static IReadOnlyList<SymbolDrawCommand> Create(OrbatEquipmentModifier1 modifier)
    {
        var text = modifier.GetSymbolText();
        if (!string.IsNullOrWhiteSpace(text))
        {
            var fontSize = text.Length switch
            {
                1 => 108f,
                2 => 93f,
                _ => 81f
            };
            return new[] { SymbolDrawCommand.TextCommand(new PointF(0.5f, 0.5f), text, fontSize) };
        }

        return modifier switch
        {
            OrbatEquipmentModifier1.ArmoredProtection => new[]
            {
                SymbolDrawCommand.Capsule(new PointF(0.08f, 0.24f), new PointF(0.92f, 0.76f))
            },
            OrbatEquipmentModifier1.EchelonOfSupport => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.42f, 0.18f), new PointF(0.42f, 0.82f)),
                SymbolDrawCommand.Line(new PointF(0.58f, 0.18f), new PointF(0.58f, 0.82f))
            },
            OrbatEquipmentModifier1.Cargo => new[]
            {
                SymbolDrawCommand.Rectangle(new PointF(0.12f, 0.24f), new PointF(0.88f, 0.76f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.24f), new PointF(0.5f, 0.76f))
            },
            OrbatEquipmentModifier1.MedicalEvacuation => new[]
            {
                SymbolDrawCommand.Path(new[]
                {
                    new SymbolPoint(0.38f, 0.08f), new SymbolPoint(0.62f, 0.08f),
                    new SymbolPoint(0.62f, 0.38f), new SymbolPoint(0.92f, 0.38f),
                    new SymbolPoint(0.92f, 0.62f), new SymbolPoint(0.62f, 0.62f),
                    new SymbolPoint(0.62f, 0.92f), new SymbolPoint(0.38f, 0.92f),
                    new SymbolPoint(0.38f, 0.62f), new SymbolPoint(0.08f, 0.62f),
                    new SymbolPoint(0.08f, 0.38f), new SymbolPoint(0.38f, 0.38f)
                }, filled: true)
            },
            OrbatEquipmentModifier1.PetroleumOilsAndLubricants => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.18f, 0.18f), new PointF(0.5f, 0.62f)),
                SymbolDrawCommand.Line(new PointF(0.82f, 0.18f), new PointF(0.5f, 0.62f)),
                SymbolDrawCommand.Line(new PointF(0.18f, 0.18f), new PointF(0.82f, 0.18f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.62f), new PointF(0.5f, 0.92f))
            },
            OrbatEquipmentModifier1.RecoveryAndMaintenance => new[]
            {
                Curve(new PointF(0.12f, 0.22f), new PointF(0.12f, 0.78f), new PointF(0.42f, 0.22f), new PointF(0.42f, 0.78f)),
                SymbolDrawCommand.Line(new PointF(0.31f, 0.5f), new PointF(0.69f, 0.5f)),
                Curve(new PointF(0.88f, 0.22f), new PointF(0.88f, 0.78f), new PointF(0.58f, 0.22f), new PointF(0.58f, 0.78f))
            },
            OrbatEquipmentModifier1.RoboticGuidedAndAutonomous => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.5f, 0.2f), new PointF(0.18f, 0.7f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.2f), new PointF(0.82f, 0.7f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.2f), new PointF(0.5f, 0.78f)),
                SymbolDrawCommand.Dot(new PointF(0.18f, 0.7f), 0.07f),
                SymbolDrawCommand.Dot(new PointF(0.5f, 0.78f), 0.07f),
                SymbolDrawCommand.Dot(new PointF(0.82f, 0.7f), 0.07f)
            },
            OrbatEquipmentModifier1.Water => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.08f, 0.54f), new PointF(0.58f, 0.54f)),
                SymbolDrawCommand.Line(new PointF(0.48f, 0.54f), new PointF(0.48f, 0.34f)),
                SymbolDrawCommand.Line(new PointF(0.34f, 0.34f), new PointF(0.72f, 0.34f)),
                Curve(new PointF(0.58f, 0.54f), new PointF(0.9f, 0.88f), new PointF(0.78f, 0.54f), new PointF(0.9f, 0.66f))
            },
            _ => Array.Empty<SymbolDrawCommand>()
        };
    }

    private static SymbolDrawCommand Curve(PointF start, PointF end, PointF control1, PointF control2) =>
        new()
        {
            Kind = SymbolDrawCommandKind.Bezier,
            Start = new SymbolPoint(start),
            End = new SymbolPoint(end),
            Control1 = new SymbolPoint(control1),
            Control2 = new SymbolPoint(control2)
        };

    public static IReadOnlyList<SymbolDrawCommand> Create(Components.OrbatEquipmentFunction equipmentFunction)
    {
        return equipmentFunction switch
        {
            Components.OrbatEquipmentFunction.Mortar => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.5f, 0.24f), new PointF(0.5f, 0.72f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.24f), new PointF(0.36f, 0.42f)),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.24f), new PointF(0.64f, 0.42f)),
                SymbolDrawCommand.Ellipse(new PointF(0.42f, 0.72f), new PointF(0.58f, 0.88f))
            },
            Components.OrbatEquipmentFunction.Gun or
            Components.OrbatEquipmentFunction.DirectFireGun => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.5f, 0.18f), new PointF(0.5f, 0.82f)),
                SymbolDrawCommand.Line(new PointF(0.36f, 0.34f), new PointF(0.64f, 0.34f))
            },
            Components.OrbatEquipmentFunction.AntiTankGun => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.5f, 0.18f), new PointF(0.5f, 0.82f)),
                SymbolDrawCommand.Line(new PointF(0.34f, 0.46f), new PointF(0.5f, 0.28f)),
                SymbolDrawCommand.Line(new PointF(0.66f, 0.46f), new PointF(0.5f, 0.28f))
            },
            Components.OrbatEquipmentFunction.Howitzer => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.5f, 0.2f), new PointF(0.5f, 0.78f)),
                SymbolDrawCommand.Dot(new PointF(0.5f, 0.78f), 0.08f)
            },
            Components.OrbatEquipmentFunction.Radar or
            Components.OrbatEquipmentFunction.Sensor => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.5f, 0.72f), new PointF(0.5f, 0.34f)),
                SymbolDrawCommand.Arc(new PointF(0.24f, 0.42f), new PointF(0.76f, 0.42f)),
                SymbolDrawCommand.Arc(new PointF(0.14f, 0.32f), new PointF(0.86f, 0.32f))
            },
            Components.OrbatEquipmentFunction.Launcher or
            Components.OrbatEquipmentFunction.MissileLauncher => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.3f, 0.72f), new PointF(0.7f, 0.28f)),
                SymbolDrawCommand.Line(new PointF(0.7f, 0.28f), new PointF(0.5f, 0.32f)),
                SymbolDrawCommand.Line(new PointF(0.7f, 0.28f), new PointF(0.66f, 0.48f))
            },
            Components.OrbatEquipmentFunction.GrenadeLauncher => new[]
            {
                SymbolDrawCommand.Line(new PointF(0.32f, 0.72f), new PointF(0.68f, 0.36f)),
                SymbolDrawCommand.Line(new PointF(0.68f, 0.36f), new PointF(0.5f, 0.4f)),
                SymbolDrawCommand.Line(new PointF(0.68f, 0.36f), new PointF(0.64f, 0.54f)),
                SymbolDrawCommand.Ellipse(new PointF(0.25f, 0.66f), new PointF(0.39f, 0.8f))
            },
            Components.OrbatEquipmentFunction.AirDefenseGun => new[]
            {
                SymbolDrawCommand.AirDefenseArc(),
                SymbolDrawCommand.Line(new PointF(0.5f, 0.22f), new PointF(0.5f, 0.78f))
            },
            Components.OrbatEquipmentFunction.Vehicle or
            Components.OrbatEquipmentFunction.ArmoredVehicle => new[]
            {
                SymbolDrawCommand.Capsule(new PointF(0.18f, 0.34f), new PointF(0.82f, 0.66f))
            },
            Components.OrbatEquipmentFunction.WheeledVehicle => new[]
            {
                SymbolDrawCommand.Capsule(new PointF(0.18f, 0.34f), new PointF(0.82f, 0.66f)),
                SymbolDrawCommand.Dot(new PointF(0.32f, 0.7f), 0.05f),
                SymbolDrawCommand.Dot(new PointF(0.68f, 0.7f), 0.05f)
            },
            Components.OrbatEquipmentFunction.TrackedVehicle => new[]
            {
                SymbolDrawCommand.Capsule(new PointF(0.14f, 0.32f), new PointF(0.86f, 0.68f)),
                SymbolDrawCommand.Line(new PointF(0.3f, 0.5f), new PointF(0.7f, 0.5f))
            },
            _ => Array.Empty<SymbolDrawCommand>()
        };
    }

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
