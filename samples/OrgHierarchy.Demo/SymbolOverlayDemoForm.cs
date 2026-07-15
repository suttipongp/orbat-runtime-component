using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrgHierarchy.Components;

namespace OrgHierarchy.Demo;

public sealed class SymbolOverlayDemoForm : Form
{
    private readonly OverlayCanvas _canvas = new();
    private readonly ComboBox _domainComboBox = new();
    private readonly ComboBox _affiliationComboBox = new();
    private readonly ComboBox _statusComboBox = new();
    private readonly ComboBox _equipmentOperatingStateComboBox = new();
    private readonly ComboBox _unitTypeComboBox = new();
    private readonly ComboBox _unitCategoryComboBox = new() { Width = 230, DropDownWidth = 280 };
    private readonly ComboBox _unitMainFunctionComboBox = new() { Width = 230, DropDownWidth = 380 };
    private readonly ComboBox _equipmentCategoryComboBox = new() { Width = 230, DropDownWidth = 250 };
    private readonly ComboBox _equipmentFunctionComboBox = new() { Width = 230, DropDownWidth = 300 };
    private readonly ComboBox _equipmentVariantComboBox = new();
    private readonly ComboBox _modifier1ComboBox = new();
    private readonly ComboBox _modifier2ComboBox = new();
    private readonly TableLayoutPanel _fieldsPanel = new();
    private readonly ToolTip _fieldToolTip = new();
    private readonly Dictionary<string, Control> _fieldInputs = new(StringComparer.OrdinalIgnoreCase);
    internal static readonly JsonSerializerOptions LibraryJsonOptions = CreateLibraryJsonOptions();
    private bool _updatingVariantList;
    private bool _updatingEquipmentFunctionOptions;
    private bool _updatingUnitMainFunctionOptions;
    private OrbatEquipmentFunction? _lastVariantFunction;

    public SymbolOverlayDemoForm()
    {
        Text = "ORBAT Symbol Overlay Demo";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1100, 760);
        Size = new Size(1280, 820);

        _domainComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _domainComboBox.Items.AddRange(Enum.GetNames<SymbolPhysicalDomain>().Cast<object>().ToArray());
        _domainComboBox.SelectedItem = SymbolPhysicalDomain.LandUnit.ToString();
        _domainComboBox.SelectedIndexChanged += (_, _) =>
        {
            UpdateFunctionSelectorState();
            RebuildFieldEditor(loadSample: true);
        };

        _affiliationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _affiliationComboBox.Items.AddRange(Enum.GetNames<SymbolAffiliation>().Cast<object>().ToArray());
        _affiliationComboBox.SelectedItem = SymbolAffiliation.Friendly.ToString();
        _affiliationComboBox.SelectedIndexChanged += (_, _) => ApplyModelToCanvas();

        _statusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusComboBox.Items.AddRange(Enum.GetNames<SymbolFrameStatus>().Cast<object>().ToArray());
        _statusComboBox.SelectedItem = SymbolFrameStatus.Present.ToString();
        _statusComboBox.SelectedIndexChanged += (_, _) => ApplyModelToCanvas();

        _equipmentOperatingStateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _equipmentOperatingStateComboBox.Items.AddRange(Enum.GetNames<OrbatEquipmentOperatingState>().Cast<object>().ToArray());
        _equipmentOperatingStateComboBox.SelectedItem = OrbatEquipmentOperatingState.Ground.ToString();
        _equipmentOperatingStateComboBox.SelectedIndexChanged += (_, _) => ApplyModelToCanvas();

        _unitCategoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _unitCategoryComboBox.Items.AddRange(Enum.GetValues<OrbatUnitMainFunctionCategory>()
            .Select(value => new UnitMainFunctionCategorySelection(value)).Cast<object>().ToArray());
        _unitCategoryComboBox.SelectedIndex = 0;
        _unitCategoryComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (!_updatingUnitMainFunctionOptions)
                RefreshUnitMainFunctionOptions();
        };

        _unitMainFunctionComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        _unitMainFunctionComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _unitMainFunctionComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        RefreshUnitMainFunctionOptions(OrbatUnitMainFunction.Infantry);
        _unitMainFunctionComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (!_updatingUnitMainFunctionOptions)
                ApplyModelToCanvas();
        };
        _unitMainFunctionComboBox.TextChanged += (_, _) =>
        {
            if (!_updatingUnitMainFunctionOptions)
                ApplyModelToCanvas();
        };

        _unitTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _unitTypeComboBox.Items.AddRange(Enum.GetNames<OrbatUnitType>().Cast<object>().ToArray());
        _unitTypeComboBox.SelectedItem = OrbatUnitType.Armor.ToString();
        _unitTypeComboBox.SelectedIndexChanged += (_, _) => ApplyModelToCanvas();

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
        RefreshEquipmentFunctionOptions(OrbatEquipmentFunction.Mortar);
        _equipmentFunctionComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingEquipmentFunctionOptions)
                return;

            RefreshEquipmentVariants();
            ApplyDefaultEquipmentOperatingState();
            UpdateFunctionSelectorState();
            SyncEquipmentFunctionField();
            ApplyModelToCanvas();
        };
        _equipmentVariantComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        _equipmentVariantComboBox.Text = "MediumRange";
        _equipmentVariantComboBox.TextChanged += (_, _) =>
        {
            if (_updatingVariantList)
                return;

            SyncEquipmentFunctionField();
            ApplyModelToCanvas();
        };
        _equipmentVariantComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingVariantList)
                return;

            SyncEquipmentFunctionField();
            ApplyModelToCanvas();
        };

        ConfigureModifierComboBox(_modifier1ComboBox);
        ConfigureModifierComboBox(_modifier2ComboBox);

        var resetButton = new Button { Text = "Load sample", AutoSize = true };
        resetButton.Click += (_, _) => RebuildFieldEditor(loadSample: true);

        _fieldsPanel.Dock = DockStyle.Top;
        _fieldsPanel.AutoSize = true;
        _fieldsPanel.ColumnCount = 3;
        _fieldsPanel.Padding = new Padding(0, 8, 0, 0);
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

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
        UpdateFunctionSelectorState();
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
        AddRow(panel, "Unit category", _unitCategoryComboBox);
        AddRow(panel, "Main function", _unitMainFunctionComboBox);
        AddRow(panel, "Category", _equipmentCategoryComboBox);
        AddRow(panel, "Equipment fn", _equipmentFunctionComboBox);
        AddRow(panel, "Variant", _equipmentVariantComboBox);
        AddRow(panel, "Operating state", _equipmentOperatingStateComboBox);
        AddRow(panel, "Modifier 1", _modifier1ComboBox);
        AddRow(panel, "Modifier 2", _modifier2ComboBox);
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

    private void ConfigureModifierComboBox(ComboBox comboBox)
    {
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.SelectedIndexChanged += (_, _) => ApplyModelToCanvas();
    }

    private void RebuildFieldEditor(bool loadSample)
    {
        var existingValues = _fieldInputs.ToDictionary(pair => pair.Key, pair => GetInputText(pair.Value), StringComparer.OrdinalIgnoreCase);
        _fieldInputs.Clear();
        _fieldsPanel.Controls.Clear();
        _fieldsPanel.RowStyles.Clear();
        _fieldsPanel.RowCount = 0;

        var layout = OrbatSymbolAmplifierLayouts.GetLayout(ToComponentDomain(GetSelectedDomain()));
        var sample = loadSample ? CreateSampleValues(layout.Domain, GetSelectedEquipmentFunction(), GetSelectedEquipmentVariant()) : existingValues;

        foreach (var field in layout.Fields)
        {
            var input = CreateAmplifierInput(field);
            input.Margin = new Padding(0, 2, 6, 2);
            SetInputText(input, sample.TryGetValue(field.Key, out var value) ? value : string.Empty);
            _fieldInputs[field.Key] = input;

            var row = _fieldsPanel.RowCount;
            _fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            var label = new Label { Text = field.Key, AutoSize = true, Margin = new Padding(0, 6, 6, 0) };
            SetFieldToolTip(label, field);
            SetFieldToolTip(input, field);
            _fieldsPanel.Controls.Add(label, 0, row);
            _fieldsPanel.Controls.Add(input, 1, row);
            _fieldsPanel.Controls.Add(new Label { Text = GetFieldHint(field), ForeColor = SystemColors.GrayText, AutoSize = true, Margin = new Padding(4, 6, 0, 0) }, 2, row);
            _fieldsPanel.RowCount++;
        }

        ApplyModelToCanvas();
    }

    private void ApplyModelToCanvas()
    {
        var mainSymbol = GetSelectedDomain() == SymbolPhysicalDomain.Equipment
            ? LoadEquipmentLibrarySymbol(GetSelectedEquipmentFunction(), GetSelectedEquipmentVariant())
            : LoadLandUnitLibrarySymbol(GetSelectedUnitMainFunction());
        _canvas.Model = new OverlaySymbolModel
        {
            Domain = GetSelectedDomain(),
            Affiliation = GetSelectedAffiliation(),
            Status = GetSelectedStatus(),
            OperatingState = GetSelectedEquipmentOperatingState(),
            UnitType = GetSelectedUnitType(),
            UnitMainFunction = GetSelectedUnitMainFunction(),
            EquipmentFunction = GetSelectedEquipmentFunction(),
            EquipmentVariant = GetSelectedEquipmentVariant(),
            EquipmentCommands = mainSymbol.Commands,
            EquipmentSymbolRole = mainSymbol.Role,
            EquipmentCompositionMode = mainSymbol.CompositionMode,
            EquipmentLayout = mainSymbol.Layout,
            Modifier1Commands = LoadSelectedModifierCommands(_modifier1ComboBox),
            Modifier2Commands = LoadSelectedModifierCommands(_modifier2ComboBox),
            Amplifiers = _fieldInputs.ToDictionary(pair => pair.Key, pair => GetInputText(pair.Value), StringComparer.OrdinalIgnoreCase)
        };
        _canvas.Invalidate();
    }

    private Control CreateAmplifierInput(OrbatSymbolAmplifierField field)
    {
        if (field.HasOptions)
        {
            var comboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.System };
            comboBox.Items.AddRange(field.Options.Cast<object>().ToArray());
            comboBox.TextChanged += (_, _) => ApplyModelToCanvas();
            comboBox.SelectedIndexChanged += (_, _) => ApplyModelToCanvas();
            return comboBox;
        }

        var textBox = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        if (field.MaxLength.HasValue)
            textBox.MaxLength = field.MaxLength.Value;
        textBox.TextChanged += (_, _) => ApplyModelToCanvas();
        return textBox;
    }

    private static string GetInputText(Control control)
    {
        return control switch
        {
            ComboBox comboBox => comboBox.Text,
            TextBox textBox => textBox.Text,
            _ => control.Text
        };
    }

    private static void SetInputText(Control control, string value)
    {
        switch (control)
        {
            case ComboBox comboBox:
                comboBox.Text = value;
                break;
            case TextBox textBox:
                textBox.Text = value;
                break;
            default:
                control.Text = value;
                break;
        }
    }

    private void SetFieldToolTip(Control control, OrbatSymbolAmplifierField field)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(field.Title))
            parts.Add(field.Title);
        if (!string.IsNullOrWhiteSpace(field.Description))
            parts.Add(field.Description);
        if (field.MinLength.HasValue || field.MaxLength.HasValue)
            parts.Add($"Length: {field.MinLength?.ToString() ?? "0"}-{field.MaxLength?.ToString() ?? "unlimited"}");
        if (field.HasOptions)
            parts.Add("Dropdown suggestions are common/standard values; typing custom text is still allowed.");

        if (parts.Count > 0)
            _fieldToolTip.SetToolTip(control, string.Join(Environment.NewLine, parts));
    }

    private static string GetFieldHint(OrbatSymbolAmplifierField field)
    {
        if (field.ValueKind == OrbatAmplifierValueKind.ColorStatus)
            return "Color";
        if (field.HasOptions)
            return "List";
        return field.MaxLength.HasValue ? $"Max {field.MaxLength}" : field.Area.ToString();
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

    private OrbatEquipmentOperatingState GetSelectedEquipmentOperatingState() =>
        Enum.TryParse(Convert.ToString(_equipmentOperatingStateComboBox.SelectedItem), out OrbatEquipmentOperatingState state)
            ? state
            : OrbatEquipmentOperatingState.Ground;

    private OrbatUnitMainFunction GetSelectedUnitMainFunction() =>
    _unitMainFunctionComboBox.SelectedItem is UnitMainFunctionSelection selection
        ? selection.Value
        : OrbatUnitMainFunctionCatalog.TryParseDisplayName(
            _unitMainFunctionComboBox.Text,
            out OrbatUnitMainFunction function)
            ? function
            : OrbatUnitMainFunction.Unspecified;

    private OrbatUnitMainFunctionCategory GetSelectedUnitCategory() =>
        _unitCategoryComboBox.SelectedItem is UnitMainFunctionCategorySelection selection
            ? selection.Value
            : OrbatUnitMainFunctionCategory.All;

    private void RefreshUnitMainFunctionOptions(OrbatUnitMainFunction? preferredFunction = null)
    {
        var currentFunction = preferredFunction ?? GetSelectedUnitMainFunction();
        var functions = OrbatUnitMainFunctionCatalog.GetFunctions(GetSelectedUnitCategory());
        var selectedFunction = functions.Contains(currentFunction)
            ? currentFunction
            : functions.FirstOrDefault();

        _updatingUnitMainFunctionOptions = true;
        try
        {
            _unitMainFunctionComboBox.BeginUpdate();
            _unitMainFunctionComboBox.AutoCompleteMode = AutoCompleteMode.None;
            _unitMainFunctionComboBox.SelectedIndex = -1;
            _unitMainFunctionComboBox.Items.Clear();
            _unitMainFunctionComboBox.Items.AddRange(functions
                .Select(value => new UnitMainFunctionSelection(value)).Cast<object>().ToArray());
            _unitMainFunctionComboBox.SelectedItem = _unitMainFunctionComboBox.Items
                .Cast<UnitMainFunctionSelection>()
                .FirstOrDefault(item => item.Value == selectedFunction);
        }
        finally
        {
            _unitMainFunctionComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _unitMainFunctionComboBox.EndUpdate();
            _updatingUnitMainFunctionOptions = false;
        }

        ApplyModelToCanvas();
    }

    private OrbatUnitType GetSelectedUnitType() =>
        OrbatUnitMainFunctionCatalog.ToLegacyUnitType(GetSelectedUnitMainFunction());
    private OrbatEquipmentFunction GetSelectedEquipmentFunction() =>
            _equipmentFunctionComboBox.SelectedItem is EquipmentFunctionSelection selection
                ? selection.Value
                : OrbatEquipmentFunctionCatalog.TryParseDisplayName(
                    _equipmentFunctionComboBox.Text,
                    out OrbatEquipmentFunction equipmentFunction)
                    ? equipmentFunction
                    : OrbatEquipmentFunction.Unspecified;

    private OrbatEquipmentFunctionCategory GetSelectedEquipmentCategory() =>
        _equipmentCategoryComboBox.SelectedItem is EquipmentFunctionCategorySelection selection
            ? selection.Value
            : OrbatEquipmentFunctionCategory.All;

    private void RefreshEquipmentFunctionOptions(OrbatEquipmentFunction? preferredFunction = null)
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

        RefreshEquipmentVariants();
        UpdateFunctionSelectorState();
        SyncEquipmentFunctionField();
        ApplyModelToCanvas();
    }

    private string GetSelectedEquipmentVariant() => _equipmentVariantComboBox.Text.Trim();

    private void ApplyDefaultEquipmentOperatingState()
    {
        var defaultState = OrbatEquipmentFunctionCatalog.GetDefaultOperatingState(GetSelectedEquipmentFunction());
        if (defaultState != OrbatEquipmentOperatingState.Ground)
            _equipmentOperatingStateComboBox.SelectedItem = defaultState.ToString();
    }
    private void UpdateFunctionSelectorState()
    {
        var equipment = GetSelectedDomain() == SymbolPhysicalDomain.Equipment;
        _unitTypeComboBox.Enabled = false;
        _unitCategoryComboBox.Enabled = !equipment;
        _unitMainFunctionComboBox.Enabled = !equipment;
        _equipmentCategoryComboBox.Enabled = equipment;
        _equipmentFunctionComboBox.Enabled = equipment;
        _equipmentVariantComboBox.Enabled = equipment;
        var supportsInFlight = equipment
            && OrbatEquipmentFunctionCatalog.SupportsInFlightOperatingState(GetSelectedEquipmentFunction());
        _equipmentOperatingStateComboBox.Enabled = supportsInFlight;
        if (!supportsInFlight && GetSelectedEquipmentOperatingState() != OrbatEquipmentOperatingState.Ground)
            _equipmentOperatingStateComboBox.SelectedItem = OrbatEquipmentOperatingState.Ground.ToString();
        _modifier1ComboBox.Enabled = true;
        _modifier2ComboBox.Enabled = true;
        if (equipment)
            RefreshEquipmentVariants();

        RefreshModifierOptionsForDomain();
    }

    private static OrbatSymbolDomain ToComponentDomain(SymbolPhysicalDomain domain) =>
        domain == SymbolPhysicalDomain.Equipment ? OrbatSymbolDomain.Equipment : OrbatSymbolDomain.LandUnit;

    private void SyncEquipmentFunctionField()
    {
        if (GetSelectedDomain() != SymbolPhysicalDomain.Equipment)
            return;

        if (_fieldInputs.TryGetValue("A", out var input))
            SetInputText(input, GetEquipmentFunctionLabel(GetSelectedEquipmentFunction(), GetSelectedEquipmentVariant()));
    }

    private void RefreshEquipmentVariants()
    {
        if (_updatingVariantList)
            return;

        var equipmentFunction = GetSelectedEquipmentFunction();
        var functionChanged = _lastVariantFunction.HasValue && _lastVariantFunction.Value != equipmentFunction;
        _lastVariantFunction = equipmentFunction;
        var currentText = functionChanged ? string.Empty : GetSelectedEquipmentVariant();
        var variants = LoadLibraryVariants(equipmentFunction);
        var canonicalVariant = variants.FirstOrDefault(variant => EquipmentVariantsMatch(variant, currentText));
        if (canonicalVariant == null && !string.IsNullOrWhiteSpace(currentText))
            variants.Insert(0, currentText);
        if (variants.Count == 0)
            variants.AddRange(new[] { "ShortRange", "MediumRange", "LongRange" });

        _updatingVariantList = true;
        try
        {
            _equipmentVariantComboBox.BeginUpdate();
            _equipmentVariantComboBox.Items.Clear();
            _equipmentVariantComboBox.Items.AddRange(variants.Cast<object>().ToArray());
            _equipmentVariantComboBox.Text = canonicalVariant
                ?? (string.IsNullOrWhiteSpace(currentText) ? variants[0] : currentText);
        }
        finally
        {
            _equipmentVariantComboBox.EndUpdate();
            _updatingVariantList = false;
        }
    }

    private static List<string> LoadLibraryVariants(OrbatEquipmentFunction equipmentFunction)
    {
        var files = GetRecentLibraryFiles();
        var variants = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(File.ReadAllText(file, Encoding.UTF8), LibraryJsonOptions);
                if (definition == null || definition.GetEffectivePhysicalDomain() != SymbolPhysicalDomain.Equipment)
                    continue;
                if (!Enum.TryParse(definition.EquipmentFunction, out OrbatEquipmentFunction fileFunction) || fileFunction != equipmentFunction)
                    continue;
                if (!string.IsNullOrWhiteSpace(definition.Variant))
                    variants.Add(definition.Variant.Trim());
            }
            catch
            {
                // Ignore unreadable library files; the variant dropdown remains editable.
            }
        }

        return variants.ToList();
    }

    private static LoadedEquipmentSymbol LoadLandUnitLibrarySymbol(OrbatUnitMainFunction mainFunction)
    {
        LoadedEquipmentSymbol? fallback = null;
        foreach (var file in GetRecentLibraryFiles().OrderBy(File.GetLastWriteTimeUtc))
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(
                    File.ReadAllText(file, Encoding.UTF8), LibraryJsonOptions);
                if (definition == null
                    || definition.GetEffectivePhysicalDomain() != SymbolPhysicalDomain.LandUnit
                    || definition.SymbolRole is OrbatEquipmentSymbolRole.Modifier1 or OrbatEquipmentSymbolRole.Modifier2
                    || definition.Commands.Count == 0)
                    continue;

                var fileFunction = OrbatUnitMainFunction.Unspecified;
                if (!string.IsNullOrWhiteSpace(definition.UnitMainFunction))
                    OrbatUnitMainFunctionCatalog.TryParseDisplayName(definition.UnitMainFunction, out fileFunction);
                if (fileFunction == OrbatUnitMainFunction.Unspecified
                    && Enum.TryParse(definition.UnitType, out OrbatUnitType legacyType))
                    fileFunction = OrbatUnitMainFunctionCatalog.FromLegacyUnitType(legacyType);

                var loaded = new LoadedEquipmentSymbol(
                    definition.Commands,
                    definition.SymbolRole,
                    definition.CompositionMode,
                    definition.Layout ?? OrbatEquipmentSymbolLayout.CreateDefault());
                if (fileFunction == mainFunction)
                    return loaded;
                if (fileFunction == OrbatUnitMainFunction.Unspecified)
                    fallback ??= loaded;
            }
            catch
            {
                // Ignore unreadable files and fall back to the built-in unit drawing.
            }
        }

        return fallback ?? LoadedEquipmentSymbol.Empty;
    }

    private static LoadedEquipmentSymbol LoadEquipmentLibrarySymbol(OrbatEquipmentFunction equipmentFunction, string equipmentVariant)
    {
        LoadedEquipmentSymbol? fallback = null;
        LoadedEquipmentSymbol? exactComposite = null;
        foreach (var file in SymbolOverlayDemoForm.GetRecentLibraryFiles().OrderBy(File.GetLastWriteTimeUtc))
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(File.ReadAllText(file, Encoding.UTF8), LibraryJsonOptions);
                if (definition == null || definition.GetEffectivePhysicalDomain() != SymbolPhysicalDomain.Equipment)
                    continue;
                if (!Enum.TryParse(definition.EquipmentFunction, out OrbatEquipmentFunction fileFunction) || fileFunction != equipmentFunction)
                    continue;
                if (definition.Commands.Count == 0 || definition.SymbolRole is OrbatEquipmentSymbolRole.Modifier1 or OrbatEquipmentSymbolRole.Modifier2 or OrbatEquipmentSymbolRole.MobilityIndicator)
                    continue;

                var loaded = new LoadedEquipmentSymbol(
                    definition.Commands,
                    definition.SymbolRole,
                    definition.CompositionMode,
                    definition.Layout ?? OrbatEquipmentSymbolLayout.CreateDefault());

                if (EquipmentVariantsMatch(definition.Variant, equipmentVariant))
                {
                    if (definition.SymbolRole == OrbatEquipmentSymbolRole.MainFunction)
                        return loaded;
                    exactComposite ??= loaded;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.Variant)
                    && (fallback == null || definition.SymbolRole == OrbatEquipmentSymbolRole.MainFunction))
                    fallback = loaded;
            }
            catch
            {
                // Ignore unreadable files and fall back to built-in equipment drawing.
            }
        }

        return exactComposite ?? fallback ?? LoadedEquipmentSymbol.Empty;
    }

    private static bool EquipmentVariantsMatch(string? left, string? right)
    {
        var normalizedLeft = NormalizeEquipmentVariant(left);
        return normalizedLeft.Length > 0
            && normalizedLeft.Equals(NormalizeEquipmentVariant(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEquipmentVariant(string? value) =>
        string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit));

    private void RefreshModifierOptionsForDomain()
    {
        var domain = GetSelectedDomain();
        RefreshModifierOptions(_modifier1ComboBox, OrbatEquipmentSymbolRole.Modifier1, domain);
        RefreshModifierOptions(_modifier2ComboBox, OrbatEquipmentSymbolRole.Modifier2, domain);
    }

    private static void RefreshModifierOptions(
        ComboBox comboBox,
        OrbatEquipmentSymbolRole role,
        SymbolPhysicalDomain domain)
    {
        var selected = comboBox.SelectedItem as EquipmentModifierOption;
        var options = new List<EquipmentModifierOption> { new("None", string.Empty) };

        if (domain == SymbolPhysicalDomain.LandUnit)
        {
            if (role == OrbatEquipmentSymbolRole.Modifier1)
            {
                options.AddRange(Enum.GetValues<OrbatLandUnitModifier1>()
                    .Where(value => value != OrbatLandUnitModifier1.Unspecified)
                    .Select(value => new EquipmentModifierOption(
                        value.GetDisplayName(), string.Empty, LandUnitModifier1: value)));
            }
            else
            {
                options.AddRange(Enum.GetValues<OrbatLandUnitModifier2>()
                    .Where(value => value != OrbatLandUnitModifier2.Unspecified)
                    .Select(value => new EquipmentModifierOption(
                        value.GetDisplayName(), string.Empty, LandUnitModifier2: value)));
            }
        }
        else if (role == OrbatEquipmentSymbolRole.Modifier1)
        {
            options.AddRange(Enum.GetValues<OrbatEquipmentModifier1>()
                .Where(value => value != OrbatEquipmentModifier1.Unspecified)
                .Select(value => new EquipmentModifierOption(
                    value.GetDisplayName(), string.Empty, EquipmentModifier1: value)));
        }
        else
        {
            options.AddRange(Enum.GetValues<OrbatEquipmentModifier2>()
                .Where(value => value != OrbatEquipmentModifier2.Unspecified)
                .Select(value => new EquipmentModifierOption(
                    value.GetDisplayName(), string.Empty, EquipmentModifier2: value)));
        }

        foreach (var file in SymbolOverlayDemoForm.GetRecentLibraryFiles().OrderBy(File.GetLastWriteTimeUtc))
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(
                    File.ReadAllText(file, Encoding.UTF8), LibraryJsonOptions);
                if (definition == null
                    || definition.GetEffectivePhysicalDomain() != domain
                    || definition.SymbolRole != role)
                    continue;

                if (domain == SymbolPhysicalDomain.LandUnit)
                {
                    var modifierType = role == OrbatEquipmentSymbolRole.Modifier1
                        ? definition.LandUnitModifier1Type
                        : definition.LandUnitModifier2Type;
                    if (role == OrbatEquipmentSymbolRole.Modifier1
                        && Enum.TryParse(modifierType, out OrbatLandUnitModifier1 modifier1)
                        && modifier1 != OrbatLandUnitModifier1.Unspecified)
                    {
                        options.RemoveAll(option => option.LandUnitModifier1 == modifier1);
                        options.Add(new EquipmentModifierOption(
                            modifier1.GetDisplayName(), file, LandUnitModifier1: modifier1));
                        continue;
                    }

                    if (role == OrbatEquipmentSymbolRole.Modifier2
                        && Enum.TryParse(modifierType, out OrbatLandUnitModifier2 modifier2)
                        && modifier2 != OrbatLandUnitModifier2.Unspecified)
                    {
                        options.RemoveAll(option => option.LandUnitModifier2 == modifier2);
                        options.Add(new EquipmentModifierOption(
                            modifier2.GetDisplayName(), file, LandUnitModifier2: modifier2));
                        continue;
                    }

                    options.Add(new EquipmentModifierOption(
                        GetModifierLabel(modifierType, definition, file), file));
                    continue;
                }

                var equipmentModifierType = role == OrbatEquipmentSymbolRole.Modifier1
                    ? definition.Modifier1Type
                    : definition.Modifier2Type;
                if (role == OrbatEquipmentSymbolRole.Modifier1
                    && Enum.TryParse(equipmentModifierType, out OrbatEquipmentModifier1 equipmentModifier1)
                    && equipmentModifier1 != OrbatEquipmentModifier1.Unspecified)
                {
                    options.RemoveAll(option => option.EquipmentModifier1 == equipmentModifier1);
                    options.Add(new EquipmentModifierOption(
                        equipmentModifier1.GetDisplayName(), file, EquipmentModifier1: equipmentModifier1));
                    continue;
                }

                if (role == OrbatEquipmentSymbolRole.Modifier2
                    && Enum.TryParse(equipmentModifierType, out OrbatEquipmentModifier2 equipmentModifier2)
                    && equipmentModifier2 != OrbatEquipmentModifier2.Unspecified)
                {
                    options.RemoveAll(option => option.EquipmentModifier2 == equipmentModifier2);
                    options.Add(new EquipmentModifierOption(
                        equipmentModifier2.GetDisplayName(), file, EquipmentModifier2: equipmentModifier2));
                    continue;
                }

                options.Add(new EquipmentModifierOption(
                    GetModifierLabel(equipmentModifierType, definition, file), file));
            }
            catch
            {
                // Ignore unreadable modifier definitions.
            }
        }

        comboBox.BeginUpdate();
        try
        {
            comboBox.Items.Clear();
            comboBox.Items.Add(options[0]);
            comboBox.Items.AddRange(options.Skip(1)
                .OrderBy(option => option.Label, StringComparer.CurrentCultureIgnoreCase)
                .Cast<object>()
                .ToArray());
            comboBox.SelectedItem = options.FirstOrDefault(option =>
                selected != null && option.Matches(selected)) ?? options[0];
        }
        finally
        {
            comboBox.EndUpdate();
        }
    }

    private static string GetModifierLabel(
        string modifierType,
        SymbolLibraryDefinition definition,
        string file) =>
        !string.IsNullOrWhiteSpace(modifierType)
            && !modifierType.Equals("Unspecified", StringComparison.OrdinalIgnoreCase)
                ? SplitPascalCase(modifierType)
                : string.IsNullOrWhiteSpace(definition.Name)
                    ? Path.GetFileNameWithoutExtension(file)
                    : definition.Name;

    private static IReadOnlyList<SymbolDrawCommand> LoadSelectedModifierCommands(ComboBox comboBox)
    {
        if (comboBox.SelectedItem is not EquipmentModifierOption option)
            return Array.Empty<SymbolDrawCommand>();

        if (!string.IsNullOrWhiteSpace(option.FileName))
        {
            try
            {
                var commands = JsonSerializer.Deserialize<SymbolLibraryDefinition>(File.ReadAllText(option.FileName, Encoding.UTF8), LibraryJsonOptions)?.Commands;
                if (commands is { Count: > 0 })
                    return commands;
            }
            catch
            {
                // Fall back to the built-in modifier when the library file cannot be read.
            }
        }

        if (option.EquipmentModifier1 is { } equipmentModifier1)
            return BuiltInSymbolLibrary.Create(equipmentModifier1);
        if (option.EquipmentModifier2 is { } equipmentModifier2)
            return BuiltInSymbolLibrary.Create(equipmentModifier2);
        if (option.LandUnitModifier1 is { } landUnitModifier1)
            return BuiltInSymbolLibrary.Create(landUnitModifier1);
        if (option.LandUnitModifier2 is { } landUnitModifier2)
            return BuiltInSymbolLibrary.Create(landUnitModifier2);
        return Array.Empty<SymbolDrawCommand>();
    }

    internal static IReadOnlyList<string> GetRecentLibraryFiles()
    {
        var settings = LoadLibraryViewerSettings();
        if (settings == null)
        {
            var defaultFolder = SymbolLibraryLocator.FindDefaultFolder();
            return !string.IsNullOrWhiteSpace(defaultFolder) && Directory.Exists(defaultFolder)
                ? Directory.EnumerateFiles(defaultFolder, "*.orbatsymbol.json", SearchOption.TopDirectoryOnly).ToArray()
                : Array.Empty<string>();
        }

        if (settings.Mode == 1)
            return settings.Files.Where(File.Exists).ToArray();

        return Directory.Exists(settings.Folder)
            ? Directory.EnumerateFiles(settings.Folder, "*.orbatsymbol.json", SearchOption.TopDirectoryOnly).ToArray()
            : Array.Empty<string>();
    }

    private static OverlayLibrarySettings? LoadLibraryViewerSettings()
    {
        var fileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OrgHierarchy.Demo",
            "symbol-library-viewer.json");
        try
        {
            return File.Exists(fileName)
                ? JsonSerializer.Deserialize<OverlayLibrarySettings>(File.ReadAllText(fileName, Encoding.UTF8), LibraryJsonOptions)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static JsonSerializerOptions CreateLibraryJsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static Dictionary<string, string> CreateSampleValues(OrbatSymbolDomain domain, OrbatEquipmentFunction equipmentFunction, string equipmentVariant)
    {
        return domain == OrbatSymbolDomain.Equipment
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = GetEquipmentFunctionLabel(equipmentFunction, equipmentVariant),
                ["AO"] = "FriendlyParticipating",
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
                ["AL"] = "FullyOperational",
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

    private static string GetEquipmentFunctionLabel(OrbatEquipmentFunction equipmentFunction, string variant)
    {
        var functionLabel = equipmentFunction == OrbatEquipmentFunction.Unspecified
            ? "A"
            : SplitPascalCase(equipmentFunction.ToString());
        return string.IsNullOrWhiteSpace(variant)
            ? functionLabel
            : $"{functionLabel} / {SplitPascalCase(variant.Trim())}";
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0 && char.IsUpper(current) && !char.IsWhiteSpace(value[index - 1]))
                builder.Append(' ');
            builder.Append(current);
        }

        return builder.ToString();
    }
}

internal sealed class OverlaySymbolModel
{
    public SymbolPhysicalDomain Domain { get; set; } = SymbolPhysicalDomain.LandUnit;
    public SymbolAffiliation Affiliation { get; set; } = SymbolAffiliation.Friendly;
    public SymbolFrameStatus Status { get; set; } = SymbolFrameStatus.Present;
    public OrbatEquipmentOperatingState OperatingState { get; set; } = OrbatEquipmentOperatingState.Ground;
    public OrbatUnitType UnitType { get; set; } = OrbatUnitType.Armor;
    public OrbatUnitMainFunction UnitMainFunction { get; set; } = OrbatUnitMainFunction.ArmorTracked;
    public OrbatEquipmentFunction EquipmentFunction { get; set; } = OrbatEquipmentFunction.Mortar;
    public string EquipmentVariant { get; set; } = string.Empty;
    public IReadOnlyList<SymbolDrawCommand> EquipmentCommands { get; set; } = Array.Empty<SymbolDrawCommand>();
    public OrbatEquipmentSymbolRole EquipmentSymbolRole { get; set; } = OrbatEquipmentSymbolRole.MainFunction;
    public OrbatEquipmentCompositionMode EquipmentCompositionMode { get; set; } = OrbatEquipmentCompositionMode.Composable;
    public OrbatEquipmentSymbolLayout EquipmentLayout { get; set; } = OrbatEquipmentSymbolLayout.CreateDefault();
    public IReadOnlyList<SymbolDrawCommand> Modifier1Commands { get; set; } = Array.Empty<SymbolDrawCommand>();
    public IReadOnlyList<SymbolDrawCommand> Modifier2Commands { get; set; } = Array.Empty<SymbolDrawCommand>();
    public Dictionary<string, string> Amplifiers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed record LoadedEquipmentSymbol(
    IReadOnlyList<SymbolDrawCommand> Commands,
    OrbatEquipmentSymbolRole Role,
    OrbatEquipmentCompositionMode CompositionMode,
    OrbatEquipmentSymbolLayout Layout)
{
    public static LoadedEquipmentSymbol Empty { get; } = new(
        Array.Empty<SymbolDrawCommand>(),
        OrbatEquipmentSymbolRole.MainFunction,
        OrbatEquipmentCompositionMode.Composable,
        OrbatEquipmentSymbolLayout.CreateDefault());
}

internal sealed record EquipmentModifierOption(
    string Label,
    string FileName,
    OrbatEquipmentModifier1? EquipmentModifier1 = null,
    OrbatEquipmentModifier2? EquipmentModifier2 = null,
    OrbatLandUnitModifier1? LandUnitModifier1 = null,
    OrbatLandUnitModifier2? LandUnitModifier2 = null)
{
    public bool Matches(EquipmentModifierOption other) =>
        !string.IsNullOrWhiteSpace(other.FileName)
            ? string.Equals(FileName, other.FileName, StringComparison.OrdinalIgnoreCase)
            : EquipmentModifier1 == other.EquipmentModifier1
                && EquipmentModifier2 == other.EquipmentModifier2
                && LandUnitModifier1 == other.LandUnitModifier1
                && LandUnitModifier2 == other.LandUnitModifier2;

    public override string ToString() => Label;
}

internal sealed class OverlayLibrarySettings
{
    public int Mode { get; set; }
    public string Folder { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
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
        var frameShape = SymbolFrameMapping.GetFrameShape(Model.Affiliation, Model.Domain, Model.OperatingState);
        DrawSymbol(e.Graphics, symbolBounds, frameShape);
        DrawAmplifierBoxes(e.Graphics, symbolBounds, frameShape);
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
            ? $"Chapter 4 equipment amplifier overlay demo - {FormatEquipmentTitle(Model.EquipmentFunction, Model.EquipmentVariant)}"
            : "Chapter 2 unit amplifier overlay demo";
        graphics.DrawString(title, _smallFont, labelBrush, page.Left + 12, page.Top + 10);
    }

    private static string FormatEquipmentTitle(OrbatEquipmentFunction equipmentFunction, string variant)
    {
        var functionLabel = equipmentFunction == OrbatEquipmentFunction.Unspecified
            ? "Equipment"
            : SplitPascalCase(equipmentFunction.ToString());
        return string.IsNullOrWhiteSpace(variant)
            ? functionLabel
            : $"{functionLabel} / {SplitPascalCase(variant.Trim())}";
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0 && char.IsUpper(current) && !char.IsWhiteSpace(value[index - 1]))
                builder.Append(' ');
            builder.Append(current);
        }

        return builder.ToString();
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
        var fillUpperFrameCap = Model.Domain == SymbolPhysicalDomain.Equipment
    && Model.EquipmentFunction == OrbatEquipmentFunction.CommunicationsSatellite
    && Model.OperatingState == OrbatEquipmentOperatingState.InFlight;
        SymbolFrameRenderer.DrawFrame(graphics, symbolBounds, frameShape, Model.Status, fillFrame: true, IconGuideShape.FlatTopBottom, fillUpperCap: fillUpperFrameCap);

        var commands = Model.EquipmentCommands.Count > 0
            ? Model.EquipmentCommands
            : Model.Domain == SymbolPhysicalDomain.Equipment
                ? BuiltInSymbolLibrary.Create(Model.EquipmentFunction)
                : BuiltInSymbolLibrary.Create(Model.UnitType);
        if (commands.Count == 0)
        {
            commands = Model.Domain == SymbolPhysicalDomain.Equipment
                ? BuiltInSymbolLibrary.Create(OrbatEquipmentFunction.Mortar)
                : BuiltInSymbolLibrary.Create(OrbatUnitType.Armor);
        }

        var interiorFrame = SymbolFrameRenderer.GetInteriorFrame(symbolBounds, frameShape, IconGuideShape.FlatTopBottom);
        var hasModifier1 = Model.Modifier1Commands.Count > 0;
        var hasModifier2 = Model.Modifier2Commands.Count > 0;
        if (Model.Domain == SymbolPhysicalDomain.LandUnit && !hasModifier1 && !hasModifier2)
        {
            DrawCommands(graphics, symbolBounds, interiorFrame, frameShape, commands, pen);
            return;
        }

        var hasSelectedModifier = hasModifier1 || hasModifier2;
        var compositionMode = Model.EquipmentCommands.Count == 0 || hasSelectedModifier
            ? OrbatEquipmentCompositionMode.Composable
            : Model.EquipmentCompositionMode;
        var role = compositionMode == OrbatEquipmentCompositionMode.Composite
            ? OrbatEquipmentSymbolRole.Composite
            : Model.EquipmentSymbolRole == OrbatEquipmentSymbolRole.Composite
                ? OrbatEquipmentSymbolRole.MainFunction
                : Model.EquipmentSymbolRole;
        var mainFrame = SymbolFrameRenderer.GetEquipmentComponentFrame(
            interiorFrame,
            role,
            Model.EquipmentLayout,
            hasModifier1,
            hasModifier2);
        DrawCommands(graphics, symbolBounds, mainFrame, frameShape, commands, pen);

        if (compositionMode != OrbatEquipmentCompositionMode.Composable)
            return;

        if (hasModifier1)
        {
            var modifierFrame = SymbolFrameRenderer.GetEquipmentComponentFrame(
                interiorFrame, OrbatEquipmentSymbolRole.Modifier1, Model.EquipmentLayout, hasModifier1, hasModifier2);
            var commandFrame = SymbolFrameRenderer.FitCommandsPreservingAspect(modifierFrame, Model.Modifier1Commands);
            commandFrame = SymbolFrameRenderer.ContainComponentFrame(
                symbolBounds,
                frameShape,
                commandFrame,
                Model.Modifier1Commands,
                OrbatEquipmentSymbolRole.Modifier1,
                IconGuideShape.FlatTopBottom);
            DrawCommands(graphics, symbolBounds, commandFrame, frameShape, Model.Modifier1Commands, pen);
        }

        if (hasModifier2)
        {
            var modifierFrame = SymbolFrameRenderer.GetEquipmentComponentFrame(
                interiorFrame, OrbatEquipmentSymbolRole.Modifier2, Model.EquipmentLayout, hasModifier1, hasModifier2);
            var commandFrame = SymbolFrameRenderer.FitCommandsPreservingAspect(modifierFrame, Model.Modifier2Commands);
            commandFrame = SymbolFrameRenderer.ContainComponentFrame(
                symbolBounds,
                frameShape,
                commandFrame,
                Model.Modifier2Commands,
                OrbatEquipmentSymbolRole.Modifier2,
                IconGuideShape.FlatTopBottom);
            DrawCommands(graphics, symbolBounds, commandFrame, frameShape, Model.Modifier2Commands, pen);
        }
    }

    private static void DrawCommands(
        Graphics graphics,
        RectangleF symbolBounds,
        RectangleF drawingFrame,
        SymbolFrameShape frameShape,
        IReadOnlyList<SymbolDrawCommand> commands,
        Pen pen)
    {
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

    private void DrawAmplifierBoxes(Graphics graphics, RectangleF symbolBounds, SymbolFrameShape frameShape)
    {
        if (Model.Domain == SymbolPhysicalDomain.Equipment)
        {
            DrawEquipmentAmplifierBoxes(graphics, symbolBounds, frameShape);
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

    private void DrawEquipmentAmplifierBoxes(Graphics graphics, RectangleF symbolBounds, SymbolFrameShape frameShape)
    {
        var centerX = symbolBounds.Left + symbolBounds.Width / 2f;
        var rowHeight = 42f;
        var sideWidth = 126f;
        var sideTop = symbolBounds.Top + 2;

        DrawStack(graphics, new[] { "W/R", "Y/Y", "V/AD/AE", "T", "Z" }, new RectangleF(symbolBounds.Left - sideWidth - 42, sideTop, sideWidth, rowHeight), StringAlignment.Far);
        DrawStack(graphics, new[] { "G/AQ", "H/AF", "J/L/N/P" }, new RectangleF(symbolBounds.Right + 42, sideTop + rowHeight, sideWidth, rowHeight), StringAlignment.Near);

        var colorBar = GetEquipmentColorBarBounds(centerX, symbolBounds);
        var aoBar = new RectangleF(colorBar.Left, symbolBounds.Top - 30, colorBar.Width, colorBar.Height);
        DrawColorStatusBar(graphics, "AO", aoBar, GetEngagementBarColor);
        DrawFieldBox(graphics, "C", new RectangleF(centerX - 64, symbolBounds.Top - 92, 128, 42), StringAlignment.Center);
        var ragBox = new RectangleF(centerX - symbolBounds.Width / 2f, symbolBounds.Bottom + 10, symbolBounds.Width, rowHeight);
        DrawFieldBox(graphics, "R/AG", ragBox, StringAlignment.Center);
        var alBar = new RectangleF(colorBar.Left, ragBox.Bottom, colorBar.Width, colorBar.Height);
        DrawColorStatusBar(graphics, "AL", alBar, GetOperationalConditionColor);
        DrawEquipmentConnectors(graphics, symbolBounds, alBar, frameShape);
    }

    private static RectangleF GetEquipmentColorBarBounds(float centerX, RectangleF symbolBounds)
    {
        var width = Math.Min(144f, symbolBounds.Width * 0.76f);
        return new RectangleF(centerX - width / 2f, 0, width, 12f);
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

    private void DrawEquipmentConnectors(
        Graphics graphics,
        RectangleF symbolBounds,
        RectangleF alBox,
        SymbolFrameShape frameShape)
    {
        var centerX = symbolBounds.Left + symbolBounds.Width / 2f;
        var start = SymbolFrameRenderer.GetEquipmentS2Anchor(
            symbolBounds,
            frameShape,
            IconGuideShape.FlatTopBottom);
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

        if (IsMobilityField(key) && DrawMobilityBox(graphics, box, value))
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

    private void DrawColorStatusBar(Graphics graphics, string key, RectangleF box, Func<string, Color> colorResolver)
    {
        if (!TryGetValue(key, out var value))
            return;

        using var fill = new SolidBrush(colorResolver(value));
        using var border = new Pen(Color.FromArgb(64, 64, 64), 1.3f);
        graphics.FillRectangle(fill, box);
        graphics.DrawRectangle(border, Rectangle.Round(box));
    }

    private static Color GetEngagementBarColor(string value)
    {
        return NormalizeOption(value) switch
        {
            "hostiletarget" => Color.FromArgb(230, 62, 54),
            "hostilenontarget" => Color.White,
            "hostileexpiredtarget" => Color.FromArgb(245, 159, 41),
            "friendlyparticipating" => Color.FromArgb(71, 151, 236),
            _ => Color.White
        };
    }

    private static Color GetOperationalConditionColor(string value)
    {
        return NormalizeOption(value) switch
        {
            "fullyoperational" => Color.FromArgb(64, 190, 92),
            "damagedbutoperational" => Color.FromArgb(245, 206, 71),
            "destroyed" => Color.FromArgb(230, 62, 54),
            "fulltocapacity" => Color.FromArgb(71, 151, 236),
            _ => Color.White
        };
    }

    private static string NormalizeOption(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static bool IsMobilityField(string key) =>
        key.Equals("W/R", StringComparison.OrdinalIgnoreCase)
        || key.Equals("R/AG", StringComparison.OrdinalIgnoreCase);

    private bool DrawMobilityBox(Graphics graphics, RectangleF box, string value)
    {
        var normalized = NormalizeOption(value);
        if (!Enum.TryParse<OrbatEquipmentMobilityMode>(normalized, ignoreCase: true, out var mobility)
            || mobility == OrbatEquipmentMobilityMode.Unspecified)
            return false;

        var commands = LoadMobilityCommands(mobility);
        if (commands.Count == 0)
            return false;

        DrawEmptyBox(graphics, box);
        var iconBox = GetCenteredIconBox(box, 0.92f, 0.84f);
        var mobilityFrame = SymbolFrameRenderer.GetMobilityFrame(iconBox);
        using var pen = new Pen(Color.Black, 1.7f);
        foreach (var command in commands)
            command.Draw(graphics, mobilityFrame, pen, Brushes.Black);

        return true;
    }

    private static IReadOnlyList<SymbolDrawCommand> LoadMobilityCommands(OrbatEquipmentMobilityMode mobility)
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

                selected = definition.Commands;
            }
            catch
            {
                // Ignore unreadable overrides and retain the built-in fallback.
            }
        }

        return selected ?? BuiltInSymbolLibrary.Create(mobility);
    }

    private static RectangleF GetCenteredIconBox(RectangleF box, float widthRatio, float heightRatio)
    {
        var width = box.Width * widthRatio;
        var height = box.Height * heightRatio;
        return new RectangleF(
            box.Left + (box.Width - width) / 2f,
            box.Top + (box.Height - height) / 2f,
            width,
            height);
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
        DrawEmptyBox(graphics, box);
        using var brush = new SolidBrush(Color.Black);
        using var format = new StringFormat(StringFormatFlags.NoWrap) { Alignment = alignment, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        var textBox = RectangleF.Inflate(box, -7, 0);
        graphics.DrawString(value, _fieldFont, brush, textBox, format);
    }

    private static void DrawEmptyBox(Graphics graphics, RectangleF box)
    {
        using var fill = new SolidBrush(Color.White);
        using var border = new Pen(Color.FromArgb(148, 148, 148), 1.4f);
        graphics.FillRectangle(fill, box);
        graphics.DrawRectangle(border, Rectangle.Round(box));
    }
}
