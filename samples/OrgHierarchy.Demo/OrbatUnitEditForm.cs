using OrgHierarchy.Components;

namespace OrgHierarchy.Demo;

internal sealed class OrbatUnitDraft
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string UniqueDesignation { get; set; } = string.Empty;
    public OrbatAffiliation Affiliation { get; set; } = OrbatAffiliation.Friend;
    public OrbatEchelon Echelon { get; set; } = OrbatEchelon.Unspecified;
    public OrbatUnitType UnitType { get; set; } = OrbatUnitType.Unspecified;
    public string Sidc { get; set; } = string.Empty;
    public string SymbolText { get; set; } = string.Empty;
    public bool Headquarters { get; set; }
    public bool TaskForce { get; set; }
    public bool PlannedAnticipated { get; set; }
    public int StackCount { get; set; } = 1;
    public OrbatReinforcedReduced ReinforcedReduced { get; set; } = OrbatReinforcedReduced.NotApplicable;
    public int SortOrder { get; set; }
}

internal sealed class OrbatUnitEditForm : Form
{
    private readonly TextBox _idTextBox = new();
    private readonly TextBox _parentIdTextBox = new();
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _shortNameTextBox = new();
    private readonly TextBox _uniqueDesignationTextBox = new();
    private readonly ComboBox _affiliationComboBox = new();
    private readonly ComboBox _echelonComboBox = new();
    private readonly ComboBox _unitTypeComboBox = new();
    private readonly TextBox _sidcTextBox = new();
    private readonly Button _fromSidcButton = new();
    private readonly Button _toSidcButton = new();
    private readonly TextBox _symbolTextTextBox = new();
    private readonly ComboBox _reinforcedReducedComboBox = new();
    private readonly CheckBox _headquartersCheckBox = new();
    private readonly CheckBox _taskForceCheckBox = new();
    private readonly CheckBox _plannedAnticipatedCheckBox = new();
    private readonly NumericUpDown _stackCountInput = new();
    private readonly NumericUpDown _sortOrderInput = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();
    private bool _applyingValues;
    private bool _fieldsChangedAfterSidc;

    public OrbatUnitEditForm(OrbatUnitDraft unit, bool isNew)
    {
        Unit = unit;

        Text = isNew ? "Add ORBAT Unit" : "Edit ORBAT Unit";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 640);
        Padding = new Padding(12);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 16,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < layout.RowCount; row++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        AddTextRow(layout, "Id", _idTextBox, unit.Id, 0, true);
        AddTextRow(layout, "Parent Id", _parentIdTextBox, unit.ParentId ?? string.Empty, 1, true);
        AddTextRow(layout, "Name", _nameTextBox, unit.Name, 2, false);
        AddTextRow(layout, "Short name", _shortNameTextBox, unit.ShortName, 3, false);
        AddTextRow(layout, "Unique designation", _uniqueDesignationTextBox, unit.UniqueDesignation, 4, false);
        AddComboRow(layout, "Affiliation", _affiliationComboBox, unit.Affiliation, 5);
        AddComboRow(layout, "Echelon", _echelonComboBox, unit.Echelon, 6);
        AddComboRow(layout, "Unit type", _unitTypeComboBox, unit.UnitType, 7);
        AddSidcRow(layout, unit.Sidc, 8);
        AddTextRow(layout, "Symbol text", _symbolTextTextBox, unit.SymbolText, 9, false);
        AddComboRow(layout, "Reinforced/reduced", _reinforcedReducedComboBox, unit.ReinforcedReduced, 10);
        AddCheckRow(layout, _headquartersCheckBox, "Headquarters", unit.Headquarters, 11);
        AddCheckRow(layout, _taskForceCheckBox, "Task force", unit.TaskForce, 12);
        AddCheckRow(layout, _plannedAnticipatedCheckBox, "Planned/Anticipated", unit.PlannedAnticipated, 13);
        AddNumberRow(layout, "Stack count", _stackCountInput, unit.StackCount, 1, 6, 14);
        AddNumberRow(layout, "Sort order", _sortOrderInput, unit.SortOrder, 0, 100000, 15);
        AddButtonRow(root, 1);

        scrollPanel.Controls.Add(layout);
        root.Controls.Add(scrollPanel, 0, 0);
        Controls.Add(root);

        WireSourceTracking();
        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }

    public OrbatUnitDraft Unit { get; private set; }

    private static void AddTextRow(TableLayoutPanel layout, string label, TextBox textBox, string value, int row, bool readOnly)
    {
        AddLabel(layout, label, row);
        textBox.Dock = DockStyle.Fill;
        textBox.ReadOnly = readOnly;
        textBox.Text = value;
        layout.Controls.Add(textBox, 1, row);
    }

    private void AddSidcRow(TableLayoutPanel layout, string value, int row)
    {
        AddLabel(layout, "SIDC", row);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));

        _sidcTextBox.Dock = DockStyle.Fill;
        _sidcTextBox.Text = value;

        _fromSidcButton.Text = "From SIDC";
        _fromSidcButton.Dock = DockStyle.Fill;
        _fromSidcButton.Click += (_, _) => ApplySidc();

        _toSidcButton.Text = "To SIDC";
        _toSidcButton.Dock = DockStyle.Fill;
        _toSidcButton.Click += (_, _) => UpdateSidcFromFields();

        panel.Controls.Add(_sidcTextBox, 0, 0);
        panel.Controls.Add(_fromSidcButton, 1, 0);
        panel.Controls.Add(_toSidcButton, 2, 0);
        layout.Controls.Add(panel, 1, row);
    }

    private static void AddComboRow<TEnum>(TableLayoutPanel layout, string label, ComboBox comboBox, TEnum value, int row)
        where TEnum : struct, Enum
    {
        AddLabel(layout, label, row);
        comboBox.Dock = DockStyle.Fill;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Items.AddRange(Enum.GetNames(typeof(TEnum)));
        comboBox.SelectedItem = value.ToString();
        if (comboBox.SelectedIndex < 0 && comboBox.Items.Count > 0)
            comboBox.SelectedIndex = 0;
        layout.Controls.Add(comboBox, 1, row);
    }

    private static void AddCheckRow(TableLayoutPanel layout, CheckBox checkBox, string text, bool value, int row)
    {
        layout.Controls.Add(new Label(), 0, row);
        checkBox.AutoSize = true;
        checkBox.Text = text;
        checkBox.Checked = value;
        checkBox.Dock = DockStyle.Left;
        layout.Controls.Add(checkBox, 1, row);
    }

    private static void AddNumberRow(TableLayoutPanel layout, string label, NumericUpDown input, int value, int minimum, int maximum, int row)
    {
        AddLabel(layout, label, row);
        input.Dock = DockStyle.Left;
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Value = Math.Max(minimum, Math.Min(maximum, value));
        layout.Controls.Add(input, 1, row);
    }

    private void AddButtonRow(TableLayoutPanel layout, int row)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        _okButton.Text = "OK";
        _okButton.Width = 82;
        _okButton.DialogResult = DialogResult.OK;
        _okButton.Click += (_, args) =>
        {
            if (!SaveUnit())
            {
                args = EventArgs.Empty;
                DialogResult = DialogResult.None;
            }
        };

        _cancelButton.Text = "Cancel";
        _cancelButton.Width = 82;
        _cancelButton.DialogResult = DialogResult.Cancel;

        panel.Controls.Add(_okButton);
        panel.Controls.Add(_cancelButton);
        layout.Controls.Add(panel, 0, row);
        if (layout.ColumnCount > 1)
            layout.SetColumnSpan(panel, 2);
    }

    private static void AddLabel(TableLayoutPanel layout, string text, int row)
    {
        var label = new Label
        {
            AutoSize = true,
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };
        layout.Controls.Add(label, 0, row);
    }

    private bool SaveUnit()
    {
        if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            MessageBox.Show(this, "Name is required.", "ORBAT Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _nameTextBox.Focus();
            return false;
        }

        var affiliation = GetSelected(_affiliationComboBox, OrbatAffiliation.Friend);
        var echelon = GetSelected(_echelonComboBox, OrbatEchelon.Unspecified);
        var unitType = GetSelected(_unitTypeComboBox, OrbatUnitType.Unspecified);
        var headquarters = _headquartersCheckBox.Checked;
        var taskForce = _taskForceCheckBox.Checked;
        var plannedAnticipated = _plannedAnticipatedCheckBox.Checked;
        var sidc = GetSidcForSave(affiliation, echelon, unitType, headquarters, taskForce, plannedAnticipated);
        var sidcParts = OrbatSidcParser.Parse(sidc);

        if (sidcParts.IsValid && !_fieldsChangedAfterSidc)
        {
            affiliation = sidcParts.Affiliation ?? affiliation;
            echelon = sidcParts.Echelon ?? echelon;
            unitType = sidcParts.UnitType ?? unitType;
            headquarters = sidcParts.Headquarters ?? headquarters;
            taskForce = sidcParts.TaskForce ?? taskForce;
            plannedAnticipated = sidcParts.PlannedAnticipated ?? plannedAnticipated;
        }

        Unit = new OrbatUnitDraft
        {
            Id = _idTextBox.Text.Trim(),
            ParentId = string.IsNullOrWhiteSpace(_parentIdTextBox.Text) ? null : _parentIdTextBox.Text.Trim(),
            Name = _nameTextBox.Text.Trim(),
            ShortName = string.IsNullOrWhiteSpace(_shortNameTextBox.Text) ? _nameTextBox.Text.Trim() : _shortNameTextBox.Text.Trim(),
            UniqueDesignation = _uniqueDesignationTextBox.Text.Trim(),
            Affiliation = affiliation,
            Echelon = echelon,
            UnitType = unitType,
            Sidc = sidc,
            SymbolText = _symbolTextTextBox.Text.Trim(),
            Headquarters = headquarters,
            TaskForce = taskForce,
            PlannedAnticipated = plannedAnticipated,
            StackCount = (int)_stackCountInput.Value,
            ReinforcedReduced = GetSelected(_reinforcedReducedComboBox, OrbatReinforcedReduced.NotApplicable),
            SortOrder = (int)_sortOrderInput.Value
        };

        return true;
    }

    private string GetSidcForSave(
        OrbatAffiliation affiliation,
        OrbatEchelon echelon,
        OrbatUnitType unitType,
        bool headquarters,
        bool taskForce,
        bool plannedAnticipated)
    {
        var sidc = _sidcTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(sidc) && !_fieldsChangedAfterSidc)
            return sidc;

        return OrbatSidcParser.Compose(affiliation, echelon, unitType, headquarters, taskForce, plannedAnticipated);
    }

    private void ApplySidc()
    {
        if (!OrbatSidcParser.TryParse(_sidcTextBox.Text, out var parsed))
        {
            MessageBox.Show(this, "SIDC must contain at least 20 digits.", "ORBAT Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _sidcTextBox.Focus();
            return;
        }

        _applyingValues = true;
        try
        {
            if (parsed.Affiliation.HasValue)
                SelectComboValue(_affiliationComboBox, parsed.Affiliation.Value);
            if (parsed.Echelon.HasValue)
                SelectComboValue(_echelonComboBox, parsed.Echelon.Value);
            if (parsed.UnitType.HasValue)
                SelectComboValue(_unitTypeComboBox, parsed.UnitType.Value);
            if (parsed.Headquarters.HasValue)
                _headquartersCheckBox.Checked = parsed.Headquarters.Value;
            if (parsed.TaskForce.HasValue)
                _taskForceCheckBox.Checked = parsed.TaskForce.Value;
            if (parsed.PlannedAnticipated.HasValue)
                _plannedAnticipatedCheckBox.Checked = parsed.PlannedAnticipated.Value;
        }
        finally
        {
            _applyingValues = false;
        }

        _fieldsChangedAfterSidc = false;
    }

    private void UpdateSidcFromFields()
    {
        _applyingValues = true;
        try
        {
            _sidcTextBox.Text = OrbatSidcParser.Compose(
                GetSelected(_affiliationComboBox, OrbatAffiliation.Friend),
                GetSelected(_echelonComboBox, OrbatEchelon.Unspecified),
                GetSelected(_unitTypeComboBox, OrbatUnitType.Unspecified),
                _headquartersCheckBox.Checked,
                _taskForceCheckBox.Checked,
                _plannedAnticipatedCheckBox.Checked);
        }
        finally
        {
            _applyingValues = false;
        }

        _fieldsChangedAfterSidc = true;
    }

    private void WireSourceTracking()
    {
        _sidcTextBox.TextChanged += (_, _) =>
        {
            if (_applyingValues)
                return;

            _fieldsChangedAfterSidc = false;
        };

        _affiliationComboBox.SelectedIndexChanged += (_, _) => MarkFieldsChanged();
        _echelonComboBox.SelectedIndexChanged += (_, _) => MarkFieldsChanged();
        _unitTypeComboBox.SelectedIndexChanged += (_, _) => MarkFieldsChanged();
        _headquartersCheckBox.CheckedChanged += (_, _) => MarkFieldsChanged();
        _taskForceCheckBox.CheckedChanged += (_, _) => MarkFieldsChanged();
        _plannedAnticipatedCheckBox.CheckedChanged += (_, _) => MarkFieldsChanged();
    }

    private void MarkFieldsChanged()
    {
        if (_applyingValues)
            return;

        _fieldsChangedAfterSidc = true;
    }

    private static void SelectComboValue<TEnum>(ComboBox comboBox, TEnum value)
        where TEnum : struct, Enum
    {
        comboBox.SelectedItem = value.ToString();
    }

    private static TEnum GetSelected<TEnum>(ComboBox comboBox, TEnum fallback)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(Convert.ToString(comboBox.SelectedItem), true, out TEnum selected) ? selected : fallback;
    }
}
