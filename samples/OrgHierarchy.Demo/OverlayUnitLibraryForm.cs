using System.Text;
using System.Text.Json;
using OrgHierarchy.Components;

namespace OrgHierarchy.Demo;

public sealed class OverlayUnitLibraryForm : Form
{
    private static readonly string SettingsFileName = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OrgHierarchy.Demo",
        "overlay-unit-library.json");

    private readonly TextBox _folderTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly TextBox _searchTextBox = new() { Width = 190, PlaceholderText = "Search overlays" };
    private readonly ComboBox _domainComboBox = new() { Width = 112, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _browseButton = new() { Text = "Browse", AutoSize = true };
    private readonly Button _reloadButton = new() { Text = "Reload", AutoSize = true };
    private readonly Button _openButton = new() { Text = "Open / edit", AutoSize = true };
    private readonly Button _deleteButton = new() { Text = "Delete", AutoSize = true };
    private readonly ListView _unitListView = new();
    private readonly OverlayCanvas _preview = new();
    private readonly Label _designationValue = CreateValueLabel();
    private readonly Label _echelonValue = CreateValueLabel();
    private readonly Label _domainValue = CreateValueLabel();
    private readonly Label _functionValue = CreateValueLabel();
    private readonly Label _affiliationValue = CreateValueLabel();
    private readonly Label _modifier1Value = CreateValueLabel();
    private readonly Label _modifier2Value = CreateValueLabel();
    private readonly Label _instanceIdValue = CreateValueLabel();
    private readonly TextBox _fileValue = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly ListView _amplifierList = new();
    private readonly Label _statusLabel = new() { Dock = DockStyle.Fill, ForeColor = SystemColors.GrayText, TextAlign = ContentAlignment.MiddleLeft };
    private readonly List<OverlayUnitLibraryItem> _items = new();
    private readonly JsonSerializerOptions _jsonOptions = SymbolOverlayDemoForm.LibraryJsonOptions;

    public OverlayUnitLibraryForm()
    {
        Text = "ORBAT Overlay Library";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1420, 860);

        _domainComboBox.Items.AddRange(new object[] { "All", "LandUnit", "Equipment" });
        _domainComboBox.SelectedIndex = 0;
        _domainComboBox.SelectedIndexChanged += (_, _) => ApplyFilter();
        _searchTextBox.TextChanged += (_, _) => ApplyFilter();

        ConfigureUnitList();
        ConfigureAmplifierList();
        _browseButton.Click += (_, _) => BrowseFolder();
        _reloadButton.Click += (_, _) => LoadFolder(_folderTextBox.Text);
        _openButton.Click += (_, _) => OpenSelectedUnit();
        _deleteButton.Click += (_, _) => DeleteSelectedUnit();

        Controls.Add(CreateLayout());
        Load += (_, _) => LoadFolder(GetRecentFolder());
    }

    internal static string GetRecentFolder()
    {
        try
        {
            if (File.Exists(SettingsFileName))
            {
                var settings = JsonSerializer.Deserialize<OverlayUnitLibrarySettings>(
                    File.ReadAllText(SettingsFileName, Encoding.UTF8));
                if (!string.IsNullOrWhiteSpace(settings?.Folder) && Directory.Exists(settings.Folder))
                    return settings.Folder;
            }
        }
        catch
        {
            // Fall back to the active symbol-library location.
        }

        var libraryFolder = SymbolOverlayDemoForm.GetRecentLibraryFiles()
            .Select(Path.GetDirectoryName)
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path));
        return libraryFolder ?? Environment.CurrentDirectory;
    }

    internal static void RememberFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        try
        {
            var directory = Path.GetDirectoryName(SettingsFileName);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            var settings = new OverlayUnitLibrarySettings { Folder = folder };
            File.WriteAllText(
                SettingsFileName,
                JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }),
                Encoding.UTF8);
        }
        catch
        {
            // A read-only settings location should not block saving an overlay unit.
        }
    }

    private Control CreateLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.Controls.Add(CreateToolbar(), 0, 0);

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 560 };
        split.Panel1.Controls.Add(_unitListView);
        split.Panel2.Controls.Add(CreateDetailPanel());
        root.Controls.Add(split, 0, 1);
        root.Controls.Add(_statusLabel, 0, 2);
        return root;
    }

    private Control CreateToolbar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Padding = Padding.Empty
        };
        panel.Controls.Add(new Label { Text = "Folder", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
        _folderTextBox.Width = 360;
        panel.Controls.Add(_folderTextBox);
        panel.Controls.Add(_browseButton);
        panel.Controls.Add(_reloadButton);
        panel.Controls.Add(new Label { Text = "Domain", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
        panel.Controls.Add(_domainComboBox);
        panel.Controls.Add(_searchTextBox);
        panel.Controls.Add(_openButton);
        panel.Controls.Add(_deleteButton);
        return panel;
    }

    private void ConfigureUnitList()
    {
        _unitListView.Dock = DockStyle.Fill;
        _unitListView.View = View.Details;
        _unitListView.FullRowSelect = true;
        _unitListView.GridLines = true;
        _unitListView.HideSelection = false;
        _unitListView.MultiSelect = false;
        _unitListView.ShowGroups = true;
        _unitListView.Columns.Add("Unit", 220);
        _unitListView.Columns.Add("Echelon", 160);
        _unitListView.Columns.Add("Affiliation", 82);
        _unitListView.Columns.Add("Variant", 96);
        _unitListView.Columns.Add("Modifier 1", 116);
        _unitListView.Columns.Add("Modifier 2", 116);
        _unitListView.Columns.Add("Instance", 82);
        _unitListView.SelectedIndexChanged += (_, _) => ShowSelectedUnit();
        _unitListView.DoubleClick += (_, _) => OpenSelectedUnit();
    }

    private void ConfigureAmplifierList()
    {
        _amplifierList.Dock = DockStyle.Fill;
        _amplifierList.View = View.Details;
        _amplifierList.FullRowSelect = true;
        _amplifierList.GridLines = true;
        _amplifierList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _amplifierList.Columns.Add("Field", 80);
        _amplifierList.Columns.Add("Value", 260);
    }

    private Control CreateDetailPanel()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var previewTab = new TabPage("Preview") { Padding = new Padding(8) };
        var amplifierTab = new TabPage("Amplifiers") { Padding = new Padding(8) };

        var previewLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 68));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
        _preview.Dock = DockStyle.Fill;
        previewLayout.Controls.Add(_preview, 0, 0);
        previewLayout.Controls.Add(CreateMetadataPanel(), 0, 1);

        previewTab.Controls.Add(previewLayout);
        amplifierTab.Controls.Add(_amplifierList);
        tabs.Controls.Add(previewTab);
        tabs.Controls.Add(amplifierTab);
        return tabs;
    }

    private Control CreateMetadataPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(0, 6, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddMetadataRow(panel, 0, "Designation", _designationValue);
        AddMetadataRow(panel, 1, "Echelon", _echelonValue);
        AddMetadataRow(panel, 2, "Domain", _domainValue);
        AddMetadataRow(panel, 3, "Function", _functionValue);
        AddMetadataRow(panel, 4, "Affiliation", _affiliationValue);
        AddMetadataRow(panel, 5, "Modifier 1", _modifier1Value);
        AddMetadataRow(panel, 6, "Modifier 2", _modifier2Value);
        AddMetadataRow(panel, 7, "Instance ID", _instanceIdValue);
        AddMetadataRow(panel, 8, "File", _fileValue, 50);
        return panel;
    }

    private static void AddMetadataRow(
        TableLayoutPanel panel,
        int row,
        string label,
        Control value,
        int height = 28)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 5, 8, 0) }, 0, row);
        panel.Controls.Add(value, 1, row);
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a folder containing ORBAT overlay unit files.",
            SelectedPath = Directory.Exists(_folderTextBox.Text) ? _folderTextBox.Text : GetRecentFolder()
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            LoadFolder(dialog.SelectedPath);
    }

    private void LoadFolder(string? folder)
    {
        _items.Clear();
        _unitListView.Items.Clear();
        ClearDetails();

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            _statusLabel.Text = "Select a valid overlay library folder.";
            return;
        }

        _folderTextBox.Text = folder;
        RememberFolder(folder);
        var skipped = 0;
        foreach (var file in Directory.EnumerateFiles(folder, "*.orbatoverlay.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var instance = JsonSerializer.Deserialize<OrbatSymbolInstance>(
                    File.ReadAllText(file, Encoding.UTF8),
                    _jsonOptions);
                if (instance == null || instance.Version > OrbatSymbolInstance.CurrentSchemaVersion)
                {
                    skipped++;
                    continue;
                }

                instance.EnsureValidIdentity();
                _items.Add(new OverlayUnitLibraryItem(file, instance));
            }
            catch
            {
                skipped++;
            }
        }

        ApplyFilter();
        _statusLabel.Text = $"{_items.Count} overlay item(s) loaded"
            + (skipped > 0 ? $"; {skipped} unreadable file(s) skipped." : ".");
    }

    private void ApplyFilter()
    {
        var selectedPath = GetSelectedItem()?.FileName;
        var domain = Convert.ToString(_domainComboBox.SelectedItem) ?? "All";
        var search = _searchTextBox.Text.Trim();
        var filteredItems = _items
            .Where(item => domain == "All" || item.Instance.Domain.ToString().Equals(domain, StringComparison.OrdinalIgnoreCase))
            .Where(item => search.Length == 0 || item.SearchText.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var landCount = filteredItems.Count(item => item.Instance.Domain == OrbatSymbolDomain.LandUnit);
        var equipmentCount = filteredItems.Count(item => item.Instance.Domain == OrbatSymbolDomain.Equipment);

        _unitListView.BeginUpdate();
        try
        {
            _unitListView.Items.Clear();
            _unitListView.Groups.Clear();
            var landGroup = new ListViewGroup($"LandUnit ({landCount})", HorizontalAlignment.Left);
            var equipmentGroup = new ListViewGroup($"Equipment ({equipmentCount})", HorizontalAlignment.Left);
            _unitListView.Groups.Add(landGroup);
            _unitListView.Groups.Add(equipmentGroup);

            foreach (var item in filteredItems
                         .OrderBy(item => item.Instance.Domain)
                         .ThenBy(item => item.Designation, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(item => item.Instance.Function, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(item => item.Instance.Variant, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(item => item.Instance.InstanceId, StringComparer.OrdinalIgnoreCase))
            {
                var row = new ListViewItem(item.DisplayName)
                {
                    Tag = item,
                    Group = item.Instance.Domain == OrbatSymbolDomain.Equipment ? equipmentGroup : landGroup
                };
                row.SubItems.Add(item.Echelon);
                row.SubItems.Add(item.Instance.Affiliation.ToString());
                row.SubItems.Add(item.Instance.Variant);
                row.SubItems.Add(item.Instance.Modifier1);
                row.SubItems.Add(item.Instance.Modifier2);
                row.SubItems.Add(ShortId(item.Instance.InstanceId));
                _unitListView.Items.Add(row);
                if (item.FileName.Equals(selectedPath, StringComparison.OrdinalIgnoreCase))
                    row.Selected = true;
            }
        }
        finally
        {
            _unitListView.EndUpdate();
        }

        if (_unitListView.SelectedItems.Count == 0 && _unitListView.Items.Count > 0)
            _unitListView.Items[0].Selected = true;
        _statusLabel.Text = $"{_unitListView.Items.Count} of {_items.Count} overlay item(s) shown; "
            + $"LandUnit {landCount}, Equipment {equipmentCount}.";
    }

    private void ShowSelectedUnit()
    {
        var item = GetSelectedItem();
        if (item == null)
        {
            ClearDetails();
            return;
        }

        _preview.Model = SymbolOverlayDemoForm.ResolveInstanceModel(item.Instance);
        _preview.Invalidate();
        _designationValue.Text = item.Designation;
        _echelonValue.Text = item.Echelon;
        _domainValue.Text = item.Instance.Domain.ToString();
        _functionValue.Text = item.FunctionLabel;
        _affiliationValue.Text = item.Instance.Affiliation.ToString();
        _modifier1Value.Text = EmptyAsNone(item.Instance.Modifier1);
        _modifier2Value.Text = EmptyAsNone(item.Instance.Modifier2);
        _instanceIdValue.Text = item.Instance.InstanceId;
        _fileValue.Text = item.FileName;

        _amplifierList.BeginUpdate();
        try
        {
            _amplifierList.Items.Clear();
            foreach (var pair in (item.Instance.Amplifiers ?? new Dictionary<string, string>())
                         .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                var row = new ListViewItem(pair.Key);
                row.SubItems.Add(pair.Value);
                _amplifierList.Items.Add(row);
            }
        }
        finally
        {
            _amplifierList.EndUpdate();
        }
    }

    private void OpenSelectedUnit()
    {
        var item = GetSelectedItem();
        if (item == null)
        {
            MessageBox.Show(this, "Select an overlay item first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new SymbolOverlayDemoForm();
        if (!form.LoadInstanceFile(item.FileName))
            return;
        form.ShowDialog(this);
        LoadFolder(_folderTextBox.Text);
    }

    private void DeleteSelectedUnit()
    {
        var item = GetSelectedItem();
        if (item == null)
        {
            MessageBox.Show(this, "Select an overlay item first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(
            this,
            $"Delete overlay item '{item.DisplayName}'?{Environment.NewLine}{item.FileName}",
            "Delete overlay item",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes)
            return;

        try
        {
            File.Delete(item.FileName);
            LoadFolder(_folderTextBox.Text);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"Could not delete the overlay item.{Environment.NewLine}{exception.Message}",
                "Delete overlay item",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private OverlayUnitLibraryItem? GetSelectedItem() =>
        _unitListView.SelectedItems.Count == 0
            ? null
            : _unitListView.SelectedItems[0].Tag as OverlayUnitLibraryItem;

    private void ClearDetails()
    {
        _preview.Model = new OverlaySymbolModel();
        _preview.Invalidate();
        _designationValue.Text = string.Empty;
        _echelonValue.Text = string.Empty;
        _domainValue.Text = string.Empty;
        _functionValue.Text = string.Empty;
        _affiliationValue.Text = string.Empty;
        _modifier1Value.Text = string.Empty;
        _modifier2Value.Text = string.Empty;
        _instanceIdValue.Text = string.Empty;
        _fileValue.Text = string.Empty;
        _amplifierList.Items.Clear();
    }

    private static Label CreateValueLabel() => new()
    {
        AutoEllipsis = true,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static string ShortId(string value) => value.Length <= 8 ? value : value[..8];
    private static string EmptyAsNone(string value) => string.IsNullOrWhiteSpace(value) ? "None" : value;

    private sealed class OverlayUnitLibrarySettings
    {
        public string Folder { get; set; } = string.Empty;
    }

    private sealed record OverlayUnitLibraryItem(string FileName, OrbatSymbolInstance Instance)
    {
        public string Designation => Instance.Domain == OrbatSymbolDomain.LandUnit
            ? GetAmplifier("H/AF") ?? string.Empty
            : GetAmplifier("A") ?? string.Empty;

        public string Echelon => Instance.Domain == OrbatSymbolDomain.LandUnit
            ? GetAmplifier("B/C/D") ?? string.Empty
            : string.Empty;

        public string FunctionLabel =>
            string.IsNullOrWhiteSpace(Instance.Variant)
                ? Instance.Function
                : $"{Instance.Function} / {Instance.Variant}";

        public string DisplayName =>
            string.IsNullOrWhiteSpace(Designation)
                ? $"{FunctionLabel} [{ShortId(Instance.InstanceId)}]"
                : $"{Designation} - {FunctionLabel}";

        public string SearchText => string.Join(
            " ",
            DisplayName,
            Instance.Domain,
            Instance.Affiliation,
            Instance.Modifier1,
            Instance.Modifier2,
            Instance.InstanceId,
            Path.GetFileName(FileName),
            string.Join(" ", (Instance.Amplifiers ?? new Dictionary<string, string>()).Values));

        private string? GetAmplifier(string key)
        {
            if (Instance.Amplifiers != null
                && Instance.Amplifiers.TryGetValue(key, out var value)
                && !string.IsNullOrWhiteSpace(value)
                && !value.Equals(key, StringComparison.OrdinalIgnoreCase))
                return value.Trim();
            return null;
        }
    }
}
