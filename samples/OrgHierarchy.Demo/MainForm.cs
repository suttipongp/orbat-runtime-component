using System.Data;
using System.Text.Json;
using System.Linq;
using OrgHierarchy.Components;

namespace OrgHierarchy.Demo;

public sealed partial class MainForm : Form
{
    private const string OrbatClipboardFormat = "OrgHierarchy.Demo.OrbatUnitFormat.v1";
    private const string OrbatStructureClipboardFormat = "OrgHierarchy.Demo.OrbatSubtree.v1";

    private DataTable? _orbatTable;
    private string? _orbatViewRootId;

    public MainForm()
    {
        InitializeComponent();
        KeyPreview = true;

        _showUniqueDesignationCheckBox.CheckedChanged += (_, _) =>
        {
            _orbatChartView.ShowUniqueDesignation = _showUniqueDesignationCheckBox.Checked;
            _orbatChartView.Invalidate(true);
        };

        _addUnitButton.Click += (_, _) => AddChildUnit();
        _editUnitButton.Click += (_, _) => EditSelectedUnit();
        _deleteUnitButton.Click += (_, _) => DeleteSelectedUnit();
        _showBranchButton.Click += (_, _) => ShowSelectedBranch();
        _showParentButton.Click += (_, _) => ShowParentBranch();
        _showAllButton.Click += (_, _) => ShowAllUnits();
        _copyFormatButton.Click += (_, _) => CopySelectedUnit();
        _pasteFormatButton.Click += (_, _) => PasteCopiedUnitToSelectedUnit();
        _copyStructureButton.Click += (_, _) => CopySelectedUnitStructure();
        _pasteStructureButton.Click += (_, _) => PasteUnitStructureToSelectedUnit();
        _exportOrbatButton.Click += (_, _) => ExportOrbatData();
        _importOrbatButton.Click += (_, _) => ImportOrbatData();
        _resetOrbatButton.Click += (_, _) => ResetOrbatData();
        _openSymbolDesignerButton.Click += (_, _) => OpenSymbolDesigner();
        _viewSymbolLibraryButton.Click += (_, _) => OpenSymbolLibraryViewer();
        KeyDown += MainForm_KeyDown;

        _orbatContextMenu.Items.Add("Add unit under this unit", null, (_, _) => AddChildUnit());
        _orbatContextMenu.Items.Add("Edit unit", null, (_, _) => EditSelectedUnit());
        _orbatContextMenu.Items.Add("Delete unit", null, (_, _) => DeleteSelectedUnit());
        _orbatContextMenu.Items.Add(new ToolStripSeparator());
        _orbatContextMenu.Items.Add("Copy unit", null, (_, _) => CopySelectedUnit());
        _orbatContextMenu.Items.Add("Paste unit", null, (_, _) => PasteCopiedUnitToSelectedUnit());
        _orbatContextMenu.Items.Add("Copy structure", null, (_, _) => CopySelectedUnitStructure());
        _orbatContextMenu.Items.Add("Paste structure", null, (_, _) => PasteUnitStructureToSelectedUnit());
        _orbatContextMenu.Items.Add(new ToolStripSeparator());
        _orbatContextMenu.Items.Add("Move left", null, (_, _) => MoveSelectedUnitLeft());
        _orbatContextMenu.Items.Add("Move right", null, (_, _) => MoveSelectedUnitRight());
        _orbatContextMenu.Items.Add(new ToolStripSeparator());
        _orbatContextMenu.Items.Add("Show this branch", null, (_, _) => ShowSelectedBranch());
        _orbatContextMenu.Items.Add("Show parent branch", null, (_, _) => ShowParentBranch());
        _orbatContextMenu.Items.Add("Show all units", null, (_, _) => ShowAllUnits());

        _hierarchyTreeView.NodeActivated += (_, args) => _propertyGrid.SelectedObject = args.Record;
        _organizationChartView.NodeActivated += (_, args) => _propertyGrid.SelectedObject = args.Record;
        _orbatChartView.UnitActivated += (_, args) => _propertyGrid.SelectedObject = args.Unit;
        _symbolGalleryChartView.UnitActivated += (_, args) => _propertyGrid.SelectedObject = args.Unit;
        _orbatChartView.UnitContextRequested += (_, args) =>
        {
            _propertyGrid.SelectedObject = args.Unit;
            _orbatContextMenu.Show(Cursor.Position);
        };
        _hierarchyTreeView.SetDataLoader(_ => Task.FromResult(CreateSampleHierarchyTable()));
        _organizationChartView.SetDataLoader(_ => Task.FromResult(CreateSampleHierarchyTable()));
        _orbatChartView.SetDataLoader(_ => Task.FromResult(GetCurrentOrbatViewTable()));
        _symbolGalleryChartView.SetDataLoader(_ => Task.FromResult(CreateSymbolGalleryTable()));

        Load += async (_, _) =>
        {
            await _hierarchyTreeView.ReloadAsync();
            await _organizationChartView.ReloadAsync();
            await _orbatChartView.ReloadAsync();
            await _symbolGalleryChartView.ReloadAsync();
            _organizationChartView.FitToView();
            _orbatChartView.FitToView();
            _symbolGalleryChartView.FitToView();
        };
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs args)
    {
        if (!args.Control || !args.Shift)
            return;

        if (args.KeyCode == Keys.C)
        {
            CopySelectedUnit();
            args.Handled = true;
        }
        else if (args.KeyCode == Keys.V)
        {
            PasteCopiedUnitToSelectedUnit();
            args.Handled = true;
        }
    }

    private void OpenSymbolDesigner()
    {
        using var form = new SymbolDesignerForm();
        form.ShowDialog(this);
    }

    private void OpenSymbolLibraryViewer()
    {
        using var form = new SymbolLibraryViewerForm();
        form.ShowDialog(this);
    }

    private DataTable GetOrbatTable()
    {
        _orbatTable ??= LoadOrbatTable();
        return _orbatTable;
    }

    private DataTable GetCurrentOrbatViewTable()
    {
        var table = GetOrbatTable();
        if (string.IsNullOrWhiteSpace(_orbatViewRootId))
            return table;

        return CreateOrbatSubtreeTable(_orbatViewRootId);
    }

    private void AddChildUnit()
    {
        var parent = _orbatChartView.SelectedUnit;
        if (parent == null)
        {
            MessageBox.Show(this, "Please select a parent unit first.", "Add Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var childEchelon = GetChildEchelon(parent.Echelon);
        var draftId = CreateNewUnitId(childEchelon, OrbatUnitType.Infantry);
        var draft = new OrbatUnitDraft
        {
            Id = draftId,
            ParentId = parent.Id,
            Name = draftId,
            ShortName = draftId,
            UniqueDesignation = string.Empty,
            Affiliation = parent.Affiliation,
            Echelon = childEchelon,
            UnitType = OrbatUnitType.Infantry,
            Sidc = OrbatSidcParser.Compose(parent.Affiliation, childEchelon, OrbatUnitType.Infantry, false, false, false),
            SymbolText = string.Empty,
            Headquarters = false,
            TaskForce = false,
            PlannedAnticipated = false,
            StackCount = 1,
            ReinforcedReduced = OrbatReinforcedReduced.NotApplicable,
            SortOrder = GetNextSortOrder(parent.Id)
        };

        using var form = new OrbatUnitEditForm(draft, true, CreateParentOptions(draft.Id, draft.ParentId));
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        if (!ValidateParentChange(form.Unit))
            return;

        var savedId = CreateNewUnitId(form.Unit.Echelon, form.Unit.UnitType);
        if (ShouldUseGeneratedUnitName(form.Unit.Name, form.Unit.Id))
            form.Unit.Name = savedId;
        if (ShouldUseGeneratedUnitName(form.Unit.ShortName, form.Unit.Id))
            form.Unit.ShortName = savedId;

        form.Unit.Id = savedId;
        if (!TryAddOrbatRow(GetOrbatTable(), form.Unit, "Add Unit"))
            return;

        SaveOrbatTable();
        ReloadOrbatTable();
    }

    private void EditSelectedUnit()
    {
        var selected = _orbatChartView.SelectedUnit;
        if (selected == null)
        {
            MessageBox.Show(this, "Please select a unit to edit.", "Edit Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var row = FindOrbatRow(selected.Id);
        if (row == null)
            return;

        var draft = CreateDraft(row);
        using var form = new OrbatUnitEditForm(draft, false, CreateParentOptions(selected.Id, draft.ParentId));
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        if (!ValidateParentChange(form.Unit))
            return;

        UpdateOrbatRow(row, form.Unit);
        SaveOrbatTable();
        ReloadOrbatTable();
    }

    private void DeleteSelectedUnit()
    {
        var selected = _orbatChartView.SelectedUnit;
        if (selected == null)
        {
            MessageBox.Show(this, "Please select a unit to delete.", "Delete Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Delete {selected.Name} and all subordinate units?",
            "Delete Unit",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
            return;

        if (IsCurrentViewWithin(selected.Id))
            _orbatViewRootId = GetNullableString(FindOrbatRow(selected.Id)!, "ParentId");

        DeleteOrbatRows(selected.Id);
        SaveOrbatTable();
        _propertyGrid.SelectedObject = null;
        ReloadOrbatTable();
    }

    private void ShowSelectedBranch()
    {
        var selected = _orbatChartView.SelectedUnit;
        if (selected == null)
        {
            MessageBox.Show(this, "Please select a unit first.", "Show Branch", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _orbatViewRootId = selected.Id;
        ReloadOrbatTable();
    }

    private void ShowParentBranch()
    {
        if (string.IsNullOrWhiteSpace(_orbatViewRootId))
        {
            MessageBox.Show(this, "The chart is already showing all units.", "Show Parent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var rootRow = FindOrbatRow(_orbatViewRootId);
        if (rootRow == null)
        {
            _orbatViewRootId = null;
            ReloadOrbatTable();
            return;
        }

        _orbatViewRootId = GetNullableString(rootRow, "ParentId");
        ReloadOrbatTable();
    }

    private void ShowAllUnits()
    {
        _orbatViewRootId = null;
        ReloadOrbatTable();
    }

    private void MoveSelectedUnitLeft()
    {
        MoveSelectedUnit(-1);
    }

    private void MoveSelectedUnitRight()
    {
        MoveSelectedUnit(1);
    }

    private void MoveSelectedUnit(int direction)
    {
        var selected = _orbatChartView.SelectedUnit;
        if (selected == null)
        {
            MessageBox.Show(this, "Please select a unit to move.", "Move Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var siblings = GetSiblingRows(selected.ParentId);
        var index = siblings.FindIndex(row => string.Equals(Convert.ToString(row["Id"]), selected.Id, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return;

        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= siblings.Count)
        {
            MessageBox.Show(this, direction < 0 ? "This unit is already at the left edge." : "This unit is already at the right edge.", "Move Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        NormalizeSortOrders(siblings);
        var currentSortOrder = Convert.ToInt32(siblings[index]["SortOrder"]);
        siblings[index]["SortOrder"] = Convert.ToInt32(siblings[targetIndex]["SortOrder"]);
        siblings[targetIndex]["SortOrder"] = currentSortOrder;
        SaveOrbatTable();
        ReloadOrbatTable();
    }

    private void CopySelectedUnit()
    {
        var selected = _orbatChartView.SelectedUnit;
        if (selected == null)
        {
            MessageBox.Show(this, "Please select a unit to copy.", "Copy Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        CopyUnitsToClipboard(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { selected.Id }, selected.Id);
    }

    private void PasteCopiedUnitToSelectedUnit()
    {
        var selected = _orbatChartView.SelectedUnit;
        if (selected == null)
        {
            MessageBox.Show(this, "Please select a target parent unit first.", "Paste Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!TryReadClipboardStructure(out var structure))
        {
            MessageBox.Show(this, "Clipboard does not contain an ORBAT unit.", "Paste Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (structure.Units.Count != 1)
        {
            MessageBox.Show(this, "Clipboard contains a structure. Use Paste structure instead.", "Paste Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        PasteStructureUnderSelectedUnit(selected, structure, "Paste Unit");
    }

    private void CopySelectedUnitStructure()
    {
        var selected = _orbatChartView.SelectedUnit;
        if (selected == null)
        {
            MessageBox.Show(this, "Please select a unit to copy its structure.", "Copy Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        CopyUnitsToClipboard(GetSubtreeIds(selected.Id), selected.Id);
    }

    private void CopyUnitsToClipboard(HashSet<string> unitIds, string sourceRootId)
    {
        var source = GetOrbatTable();
        var units = source.Rows
            .Cast<DataRow>()
            .Where(row => unitIds.Contains(Convert.ToString(row["Id"]) ?? string.Empty))
            .OrderBy(GetRowDepth)
            .ThenBy(row => Convert.ToInt32(row["SortOrder"]))
            .ThenBy(row => Convert.ToString(row["Name"]), StringComparer.CurrentCultureIgnoreCase)
            .Select(OrbatUnitTemplate.FromRow)
            .ToList();

        var structure = new OrbatSubtreeClipboard
        {
            SourceRootId = sourceRootId,
            Units = units
        };

        var json = JsonSerializer.Serialize(structure, new JsonSerializerOptions { WriteIndented = true });
        var data = new DataObject();
        data.SetData(OrbatStructureClipboardFormat, json);
        data.SetText(json);
        Clipboard.SetDataObject(data, true);
    }

    private void PasteUnitStructureToSelectedUnit()
    {
        var selected = _orbatChartView.SelectedUnit;
        if (selected == null)
        {
            MessageBox.Show(this, "Please select a target parent unit first.", "Paste Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!TryReadClipboardStructure(out var structure))
        {
            MessageBox.Show(this, "Clipboard does not contain an ORBAT structure.", "Paste Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        PasteStructureUnderSelectedUnit(selected, structure, "Paste Structure");
    }

    private void PasteStructureUnderSelectedUnit(OrbatUnitRecord selected, OrbatSubtreeClipboard structure, string title)
    {
        var targetParentRow = FindOrbatRow(selected.Id);
        if (targetParentRow == null)
            return;

        var table = GetOrbatTable();
        var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var insertedCount = 0;

        foreach (var unit in structure.Units)
        {
            var newId = CreateNewUnitId(ParseEnum(unit.Echelon, OrbatEchelon.Unspecified), ParseUnitType(unit.UnitType));
            idMap[unit.Id] = newId;

            var parentId = !string.IsNullOrWhiteSpace(unit.ParentId) && idMap.TryGetValue(unit.ParentId, out var mappedParentId)
                ? mappedParentId
                : selected.Id;

            var sortOrder = parentId == selected.Id || !idMap.ContainsValue(parentId)
                ? GetNextSortOrder(parentId)
                : unit.SortOrder;

            if (!TryAddOrbatRow(table, unit.ToDraft(newId, parentId, sortOrder), title))
                return;

            insertedCount++;
        }

        SaveOrbatTable();
        ReloadOrbatTable();
        MessageBox.Show(this, $"Pasted {insertedCount} unit(s) under {selected.Name}.", title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static bool TryReadClipboardFormat(out OrbatUnitFormat format)
    {
        format = new OrbatUnitFormat();
        string? text = null;

        var data = Clipboard.GetDataObject();
        if (data != null && data.GetDataPresent(OrbatClipboardFormat))
            text = Convert.ToString(data.GetData(OrbatClipboardFormat));

        if (string.IsNullOrWhiteSpace(text) && Clipboard.ContainsText())
            text = Clipboard.GetText();

        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<OrbatUnitFormat>(text);
            if (parsed != null && parsed.IsUsable)
            {
                format = parsed;
                return true;
            }
        }
        catch (JsonException)
        {
        }

        if (OrbatSidcParser.TryParse(text, out var sidc))
        {
            format = OrbatUnitFormat.FromSidc(sidc.Sidc);
            return true;
        }

        return false;
    }

    private static bool TryReadClipboardStructure(out OrbatSubtreeClipboard structure)
    {
        structure = new OrbatSubtreeClipboard();
        string? text = null;

        var data = Clipboard.GetDataObject();
        if (data != null && data.GetDataPresent(OrbatStructureClipboardFormat))
            text = Convert.ToString(data.GetData(OrbatStructureClipboardFormat));

        if (string.IsNullOrWhiteSpace(text) && Clipboard.ContainsText())
            text = Clipboard.GetText();

        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<OrbatSubtreeClipboard>(text);
            if (parsed != null && parsed.IsUsable)
            {
                structure = parsed;
                return true;
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }

    private static void ApplyFormat(DataRow row, OrbatUnitFormat format)
    {
        row["Affiliation"] = format.Affiliation;
        row["Echelon"] = format.Echelon;
        row["UnitType"] = format.UnitType;
        row["Sidc"] = format.Sidc;
        row["SymbolText"] = format.SymbolText;
        row["Headquarters"] = format.Headquarters;
        row["TaskForce"] = format.TaskForce;
        row["PlannedAnticipated"] = format.PlannedAnticipated;
        row["StackCount"] = Math.Max(1, Math.Min(6, format.StackCount));
        row["ReinforcedReduced"] = format.ReinforcedReduced;
        row["Reinforced"] = format.ReinforcedReduced is nameof(OrbatReinforcedReduced.Reinforced) or nameof(OrbatReinforcedReduced.ReinforcedAndReduced);
        row["Reduced"] = format.ReinforcedReduced is nameof(OrbatReinforcedReduced.Reduced) or nameof(OrbatReinforcedReduced.ReinforcedAndReduced);
    }

    private void ExportOrbatData()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export ORBAT Data",
            Filter = "ORBAT XML (*.xml)|*.xml|All files (*.*)|*.*",
            FileName = "orbat.xml",
            AddExtension = true,
            DefaultExt = "xml"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        GetOrbatTable().WriteXml(dialog.FileName, XmlWriteMode.WriteSchema);
    }

    private void ImportOrbatData()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import ORBAT Data",
            Filter = "ORBAT XML (*.xml)|*.xml|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var table = new DataTable("Orbat");
            table.ReadXml(dialog.FileName);
            EnsureOrbatColumns(table);
            if (TryFindDuplicateUnitId(table, out var duplicateId))
            {
                MessageBox.Show(this, $"Import contains duplicate ORBAT unit Id '{duplicateId}'. Please fix the file before importing.", "Import ORBAT Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _orbatTable = table;
            _orbatViewRootId = null;
            SaveOrbatTable();
            ReloadOrbatTable();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not import ORBAT data.\r\n\r\n{ex.Message}", "Import ORBAT Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ResetOrbatData()
    {
        var confirm = MessageBox.Show(
            this,
            "Reset ORBAT data to the built-in sample data?",
            "Reset ORBAT Data",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
            return;

        _orbatTable = CreateSampleOrbatTable();
        _orbatViewRootId = null;
        SaveOrbatTable();
        ReloadOrbatTable();
    }

    private List<DataRow> GetSiblingRows(string? parentId)
    {
        return GetOrbatTable()
            .Rows
            .Cast<DataRow>()
            .Where(row => string.Equals(GetNullableString(row, "ParentId"), parentId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => Convert.ToInt32(row["SortOrder"]))
            .ThenBy(row => Convert.ToString(row["Name"]), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void NormalizeSortOrders(IReadOnlyList<DataRow> rows)
    {
        for (var index = 0; index < rows.Count; index++)
            rows[index]["SortOrder"] = (index + 1) * 10;
    }

    private void ReloadOrbatTable()
    {
        _orbatChartView.LoadFromDataTable(GetCurrentOrbatViewTable());
        _orbatChartView.FitToView();
    }

    private DataTable CreateOrbatSubtreeTable(string rootId)
    {
        var source = GetOrbatTable();
        var view = source.Clone();
        var idsToShow = GetSubtreeIds(rootId);

        foreach (var row in source.Rows.Cast<DataRow>().Where(row => idsToShow.Contains(Convert.ToString(row["Id"]) ?? string.Empty)))
            view.ImportRow(row);

        return view;
    }

    private HashSet<string> GetSubtreeIds(string rootId)
    {
        var table = GetOrbatTable();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootId };
        var changed = true;

        while (changed)
        {
            changed = false;
            foreach (var row in table.Rows.Cast<DataRow>())
            {
                var id = Convert.ToString(row["Id"]);
                var parentId = GetNullableString(row, "ParentId");
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(parentId) && ids.Contains(parentId) && ids.Add(id))
                    changed = true;
            }
        }

        return ids;
    }

    private int GetRowDepth(DataRow row)
    {
        var depth = 0;
        var parentId = GetNullableString(row, "ParentId");
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (!string.IsNullOrWhiteSpace(parentId) && visited.Add(parentId) && depth < 100)
        {
            depth++;
            var parent = FindOrbatRow(parentId);
            parentId = parent == null ? null : GetNullableString(parent, "ParentId");
        }

        return depth;
    }

    private IReadOnlyList<OrbatParentOption> CreateParentOptions(string currentUnitId, string? currentParentId)
    {
        var excludedIds = string.IsNullOrWhiteSpace(currentUnitId)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : GetSubtreeIds(currentUnitId);
        var visibleIds = GetCurrentVisibleOrbatIds();

        var options = new List<OrbatParentOption>
        {
            new(null, "(Root / no parent)")
        };

        foreach (var row in GetOrbatTable().Rows.Cast<DataRow>()
            .Where(row => visibleIds.Contains(Convert.ToString(row["Id"]) ?? string.Empty))
            .Where(row => !excludedIds.Contains(Convert.ToString(row["Id"]) ?? string.Empty))
            .OrderByDescending(row => GetEchelonRank(ParseEnum(row["Echelon"], OrbatEchelon.Unspecified)))
            .ThenBy(row => Convert.ToInt32(row["SortOrder"]))
            .ThenBy(row => Convert.ToString(row["Name"]), StringComparer.CurrentCultureIgnoreCase))
        {
            var id = Convert.ToString(row["Id"]) ?? string.Empty;
            var name = Convert.ToString(row["Name"]) ?? id;
            var echelon = ParseEnum(row["Echelon"], OrbatEchelon.Unspecified);
            options.Add(new OrbatParentOption(id, $"{echelon} - {name} ({id})", echelon));
        }

        if (!string.IsNullOrWhiteSpace(currentParentId)
            && !options.Any(option => string.Equals(option.Id, currentParentId, StringComparison.OrdinalIgnoreCase)))
        {
            var currentParent = FindOrbatRow(currentParentId);
            if (currentParent != null)
            {
                var parentEchelon = ParseEnum(currentParent["Echelon"], OrbatEchelon.Unspecified);
                var parentName = Convert.ToString(currentParent["Name"]) ?? currentParentId;
                options.Insert(1, new OrbatParentOption(currentParentId, $"{parentEchelon} - {parentName} ({currentParentId}) [outside current view]", parentEchelon));
            }
        }

        return options;
    }

    private HashSet<string> GetCurrentVisibleOrbatIds()
    {
        if (!string.IsNullOrWhiteSpace(_orbatViewRootId))
            return GetSubtreeIds(_orbatViewRootId);

        return GetOrbatTable()
            .Rows
            .Cast<DataRow>()
            .Select(row => Convert.ToString(row["Id"]) ?? string.Empty)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private bool ValidateParentChange(OrbatUnitDraft unit)
    {
        var unitId = unit.Id;
        var parentId = unit.ParentId;

        if (string.IsNullOrWhiteSpace(parentId))
            return true;

        if (string.Equals(unitId, parentId, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "A unit cannot be its own parent.", "ORBAT Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        if (FindOrbatRow(parentId) == null)
        {
            MessageBox.Show(this, "Parent Id was not found.", "ORBAT Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        if (GetSubtreeIds(unitId).Contains(parentId))
        {
            MessageBox.Show(this, "A unit cannot be moved under one of its subordinate units.", "ORBAT Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        var parent = FindOrbatRow(parentId);
        var parentEchelon = parent == null ? OrbatEchelon.Unspecified : ParseEnum(parent["Echelon"], OrbatEchelon.Unspecified);
        if (!IsHigherEchelon(parentEchelon, unit.Echelon))
        {
            MessageBox.Show(this, "Parent unit must have a higher echelon than the current unit.", "ORBAT Unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        return true;
    }

    private static bool IsHigherEchelon(OrbatEchelon parentEchelon, OrbatEchelon childEchelon)
    {
        var parentRank = GetEchelonRank(parentEchelon);
        var childRank = GetEchelonRank(childEchelon);
        if (childRank == 0)
            return parentRank > 0;

        return parentRank > childRank;
    }

    private static int GetEchelonRank(OrbatEchelon echelon)
    {
        return echelon switch
        {
            OrbatEchelon.Team => 1,
            OrbatEchelon.Squad => 2,
            OrbatEchelon.Section => 3,
            OrbatEchelon.Platoon => 4,
            OrbatEchelon.Company => 5,
            OrbatEchelon.Battalion => 6,
            OrbatEchelon.Regiment => 7,
            OrbatEchelon.Brigade => 8,
            OrbatEchelon.Division => 9,
            OrbatEchelon.Corps => 10,
            OrbatEchelon.Army => 11,
            OrbatEchelon.ArmyGroup => 12,
            OrbatEchelon.Region => 13,
            OrbatEchelon.Command => 14,
            _ => 0
        };
    }

    private bool IsCurrentViewWithin(string rootId)
    {
        return !string.IsNullOrWhiteSpace(_orbatViewRootId) && GetSubtreeIds(rootId).Contains(_orbatViewRootId);
    }

    private string CreateNewUnitId(OrbatEchelon echelon, OrbatUnitType unitType)
    {
        var table = GetOrbatTable();
        var prefix = $"{GetEchelonCode(echelon)}-{GetUnitTypeCode(unitType)}";
        var serial = 1;
        string id;
        do
        {
            id = $"{prefix}-{serial:000}";
            serial++;
        }
        while (ContainsUnitId(table, id));

        return id;
    }

    private static string GetEchelonCode(OrbatEchelon echelon)
    {
        return echelon switch
        {
            OrbatEchelon.Team => "TM",
            OrbatEchelon.Squad => "SQD",
            OrbatEchelon.Section => "SEC",
            OrbatEchelon.Platoon => "PLT",
            OrbatEchelon.Company => "CO",
            OrbatEchelon.Battalion => "BN",
            OrbatEchelon.Regiment => "REG",
            OrbatEchelon.Brigade => "BDE",
            OrbatEchelon.Division => "DIV",
            OrbatEchelon.Corps => "CORPS",
            OrbatEchelon.Army => "ARMY",
            OrbatEchelon.ArmyGroup => "AG",
            OrbatEchelon.Region => "RGN",
            OrbatEchelon.Command => "CMD",
            _ => "UNIT"
        };
    }

    private static string GetUnitTypeCode(OrbatUnitType unitType)
    {
        return unitType switch
        {
            OrbatUnitType.Headquarters => "HQ",
            OrbatUnitType.Infantry => "INF",
            OrbatUnitType.Armor => "ARM",
            OrbatUnitType.MechanizedInfantry => "MECH",
            OrbatUnitType.Artillery => "ARTY",
            OrbatUnitType.AirDefense => "AD",
            OrbatUnitType.Aviation => "AVN",
            OrbatUnitType.Engineer => "ENG",
            OrbatUnitType.Reconnaissance => "RECON",
            OrbatUnitType.Signal => "SIG",
            OrbatUnitType.MilitaryPolice => "MP",
            OrbatUnitType.Medical => "MED",
            OrbatUnitType.CBRN => "CBRN",
            OrbatUnitType.Logistics => "LOG",
            OrbatUnitType.Ordnance => "ORD",
            OrbatUnitType.Quartermaster => "QM",
            OrbatUnitType.Maintenance => "MAINT",
            OrbatUnitType.Transportation => "TRANS",
            OrbatUnitType.SpecialOperations => "SOF",
            OrbatUnitType.Naval => "NAV",
            OrbatUnitType.Air => "AIR",
            OrbatUnitType.Cyber => "CYB",
            OrbatUnitType.Intelligence => "INT",
            OrbatUnitType.PsychologicalOperations => "PSYOP",
            _ => "GEN"
        };
    }

    private static bool ShouldUseGeneratedUnitName(string value, string currentId)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.Equals("New Unit", StringComparison.OrdinalIgnoreCase)
            || value.Equals(currentId, StringComparison.OrdinalIgnoreCase);
    }

    private int GetNextSortOrder(string parentId)
    {
        var siblingOrders = GetOrbatTable()
            .Rows
            .Cast<DataRow>()
            .Where(row => string.Equals(GetNullableString(row, "ParentId"), parentId, StringComparison.OrdinalIgnoreCase))
            .Select(row => Convert.ToInt32(row["SortOrder"]));

        return siblingOrders.DefaultIfEmpty(0).Max() + 10;
    }

    private static OrbatEchelon GetChildEchelon(OrbatEchelon parentEchelon)
    {
        return parentEchelon switch
        {
            OrbatEchelon.Region => OrbatEchelon.ArmyGroup,
            OrbatEchelon.ArmyGroup => OrbatEchelon.Army,
            OrbatEchelon.Army => OrbatEchelon.Corps,
            OrbatEchelon.Corps => OrbatEchelon.Division,
            OrbatEchelon.Division => OrbatEchelon.Brigade,
            OrbatEchelon.Brigade => OrbatEchelon.Battalion,
            OrbatEchelon.Regiment => OrbatEchelon.Battalion,
            OrbatEchelon.Battalion => OrbatEchelon.Company,
            OrbatEchelon.Company => OrbatEchelon.Platoon,
            OrbatEchelon.Platoon => OrbatEchelon.Section,
            OrbatEchelon.Section => OrbatEchelon.Squad,
            OrbatEchelon.Squad => OrbatEchelon.Team,
            _ => OrbatEchelon.Team
        };
    }

    private DataRow? FindOrbatRow(string id)
    {
        return GetOrbatTable()
            .Rows
            .Cast<DataRow>()
            .FirstOrDefault(row => string.Equals(Convert.ToString(row["Id"]), id, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryAddOrbatRow(DataTable table, OrbatUnitDraft unit, string title)
    {
        if (ContainsUnitId(table, unit.Id))
        {
            MessageBox.Show(this, $"ORBAT unit Id '{unit.Id}' already exists. Please try again or choose a different unit type/echelon.", title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        AddOrbatRow(table, unit);
        return true;
    }

    private static bool ContainsUnitId(DataTable table, string id)
    {
        return table.Rows
            .Cast<DataRow>()
            .Any(row => string.Equals(Convert.ToString(row["Id"]), id, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryFindDuplicateUnitId(DataTable table, out string duplicateId)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in table.Rows.Cast<DataRow>().Select(row => Convert.ToString(row["Id"]) ?? string.Empty))
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (!seen.Add(id))
            {
                duplicateId = id;
                return true;
            }
        }

        duplicateId = string.Empty;
        return false;
    }

    private void DeleteOrbatRows(string rootId)
    {
        var table = GetOrbatTable();
        var idsToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootId };
        var changed = true;

        while (changed)
        {
            changed = false;
            foreach (var row in table.Rows.Cast<DataRow>())
            {
                var id = Convert.ToString(row["Id"]);
                var parentId = GetNullableString(row, "ParentId");
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(parentId) && idsToDelete.Contains(parentId) && idsToDelete.Add(id))
                    changed = true;
            }
        }

        foreach (var row in table.Rows.Cast<DataRow>().Where(row => idsToDelete.Contains(Convert.ToString(row["Id"]) ?? string.Empty)).ToList())
            table.Rows.Remove(row);
    }

    private static OrbatUnitDraft CreateDraft(DataRow row)
    {
        var draft = new OrbatUnitDraft
        {
            Id = Convert.ToString(row["Id"]) ?? string.Empty,
            ParentId = GetNullableString(row, "ParentId"),
            Name = Convert.ToString(row["Name"]) ?? string.Empty,
            ShortName = Convert.ToString(row["ShortName"]) ?? string.Empty,
            UniqueDesignation = Convert.ToString(row["UniqueDesignation"]) ?? string.Empty,
            Affiliation = ParseEnum(row["Affiliation"], OrbatAffiliation.Friend),
            Echelon = ParseEnum(row["Echelon"], OrbatEchelon.Unspecified),
            UnitType = ParseUnitType(row["UnitType"]),
            Sidc = ReadString(row, "Sidc"),
            SymbolText = ReadString(row, "SymbolText"),
            Headquarters = Convert.ToBoolean(row["Headquarters"]),
            TaskForce = Convert.ToBoolean(row["TaskForce"]),
            PlannedAnticipated = ReadBoolean(row, "PlannedAnticipated"),
            StackCount = Math.Max(1, Math.Min(6, ReadInteger(row, "StackCount", 1))),
            ReinforcedReduced = ParseReinforcedReduced(row),
            SortOrder = Convert.ToInt32(row["SortOrder"])
        };

        if (string.IsNullOrWhiteSpace(draft.Sidc))
            draft.Sidc = OrbatSidcParser.Compose(draft.Affiliation, draft.Echelon, draft.UnitType, draft.Headquarters, draft.TaskForce, draft.PlannedAnticipated);

        return draft;
    }

    private static void AddOrbatRow(DataTable table, OrbatUnitDraft unit)
    {
        table.Rows.Add(
            unit.Id,
            string.IsNullOrWhiteSpace(unit.ParentId) ? DBNull.Value : unit.ParentId,
            unit.Name,
            unit.ShortName,
            unit.UniqueDesignation,
            unit.Affiliation.ToString(),
            unit.Echelon.ToString(),
            unit.UnitType.ToString(),
            unit.Sidc,
            unit.SymbolText,
            unit.Headquarters,
            unit.TaskForce,
            unit.PlannedAnticipated,
            unit.StackCount,
            unit.ReinforcedReduced.ToString(),
            unit.ReinforcedReduced is OrbatReinforcedReduced.Reinforced or OrbatReinforcedReduced.ReinforcedAndReduced,
            unit.ReinforcedReduced is OrbatReinforcedReduced.Reduced or OrbatReinforcedReduced.ReinforcedAndReduced,
            unit.SortOrder);
    }

    private static void UpdateOrbatRow(DataRow row, OrbatUnitDraft unit)
    {
        row["ParentId"] = string.IsNullOrWhiteSpace(unit.ParentId) ? DBNull.Value : unit.ParentId;
        row["Name"] = unit.Name;
        row["ShortName"] = unit.ShortName;
        row["UniqueDesignation"] = unit.UniqueDesignation;
        row["Affiliation"] = unit.Affiliation.ToString();
        row["Echelon"] = unit.Echelon.ToString();
        row["UnitType"] = unit.UnitType.ToString();
        row["Sidc"] = unit.Sidc;
        row["SymbolText"] = unit.SymbolText;
        row["Headquarters"] = unit.Headquarters;
        row["TaskForce"] = unit.TaskForce;
        row["PlannedAnticipated"] = unit.PlannedAnticipated;
        row["StackCount"] = unit.StackCount;
        row["ReinforcedReduced"] = unit.ReinforcedReduced.ToString();
        row["Reinforced"] = unit.ReinforcedReduced is OrbatReinforcedReduced.Reinforced or OrbatReinforcedReduced.ReinforcedAndReduced;
        row["Reduced"] = unit.ReinforcedReduced is OrbatReinforcedReduced.Reduced or OrbatReinforcedReduced.ReinforcedAndReduced;
        row["SortOrder"] = unit.SortOrder;
    }

    private static string? GetNullableString(DataRow row, string columnName)
    {
        return row[columnName] == DBNull.Value ? null : Convert.ToString(row[columnName]);
    }

    private static string ReadString(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            return string.Empty;

        return Convert.ToString(row[columnName]) ?? string.Empty;
    }

    private static bool ReadBoolean(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            return false;

        return Convert.ToBoolean(row[columnName]);
    }

    private static int ReadInteger(DataRow row, string columnName, int fallback)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            return fallback;

        return int.TryParse(Convert.ToString(row[columnName]), out var value) ? value : fallback;
    }

    private static TEnum ParseEnum<TEnum>(object value, TEnum fallback)
        where TEnum : struct
    {
        var text = Convert.ToString(value);
        if (Enum.TryParse(text, true, out TEnum parsed))
            return parsed;

        var normalized = (text ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
        return Enum.TryParse(normalized, true, out parsed) ? parsed : fallback;
    }

    private static OrbatUnitType ParseUnitType(object value)
    {
        var text = Convert.ToString(value);
        if (text != null && text.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return OrbatUnitType.Unspecified;

        return ParseEnum(value, OrbatUnitType.Unspecified);
    }

    private static OrbatReinforcedReduced ParseReinforcedReduced(DataRow row)
    {
        var value = Convert.ToString(row["ReinforcedReduced"])?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(value))
        {
            var normalized = value.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
            if (Enum.TryParse(normalized, true, out OrbatReinforcedReduced parsed))
                return parsed;

            if (value == "+")
                return OrbatReinforcedReduced.Reinforced;
            if (value == "-")
                return OrbatReinforcedReduced.Reduced;
            if (value == "±" || value == "+/-" || value.Equals("both", StringComparison.OrdinalIgnoreCase))
                return OrbatReinforcedReduced.ReinforcedAndReduced;
        }

        var reinforced = row.Table.Columns.Contains("Reinforced") && Convert.ToBoolean(row["Reinforced"]);
        var reduced = row.Table.Columns.Contains("Reduced") && Convert.ToBoolean(row["Reduced"]);
        if (reinforced && reduced)
            return OrbatReinforcedReduced.ReinforcedAndReduced;
        if (reinforced)
            return OrbatReinforcedReduced.Reinforced;
        return reduced ? OrbatReinforcedReduced.Reduced : OrbatReinforcedReduced.NotApplicable;
    }

    private static DataTable LoadOrbatTable()
    {
        var path = GetOrbatDataPath();
        if (!File.Exists(path))
            return CreateSampleOrbatTable();

        try
        {
            var table = new DataTable("Orbat");
            table.ReadXml(path);
            EnsureOrbatColumns(table);
            return EnsureCurrentDemoUnits(table);
        }
        catch (Exception)
        {
            return CreateSampleOrbatTable();
        }
    }

    private void SaveOrbatTable()
    {
        if (_orbatTable == null)
            return;

        var path = GetOrbatDataPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _orbatTable.WriteXml(path, XmlWriteMode.WriteSchema);
    }

    private static string GetOrbatDataPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OrgHierarchy.Demo",
            "orbat.xml");
    }

    private static void EnsureOrbatColumns(DataTable table)
    {
        if (!table.Columns.Contains("Sidc"))
            table.Columns.Add("Sidc", typeof(string));
        if (!table.Columns.Contains("SymbolText"))
            table.Columns.Add("SymbolText", typeof(string));
        if (!table.Columns.Contains("PlannedAnticipated"))
            table.Columns.Add("PlannedAnticipated", typeof(bool));
        if (!table.Columns.Contains("StackCount"))
            table.Columns.Add("StackCount", typeof(int));
        if (!table.Columns.Contains("ReinforcedReduced"))
            table.Columns.Add("ReinforcedReduced", typeof(string));

        foreach (DataRow row in table.Rows)
        {
            if (row["StackCount"] == DBNull.Value)
                row["StackCount"] = 1;
            if (row["ReinforcedReduced"] == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(row["ReinforcedReduced"])))
                row["ReinforcedReduced"] = GetReinforcedReducedValue(ReadBoolean(row, "Reinforced"), ReadBoolean(row, "Reduced"));
        }
    }

    private static DataTable CreateSampleHierarchyTable()
    {
        var table = new DataTable("OrgHierarchy");
        table.Columns.Add("Id", typeof(string));
        table.Columns.Add("ParentId", typeof(string));
        table.Columns.Add("DisplayName", typeof(string));
        table.Columns.Add("Subtitle", typeof(string));
        table.Columns.Add("Kind", typeof(string));
        table.Columns.Add("SortOrder", typeof(int));

        AddNode(table, "ORG-001", null, "กรรมการผู้จัดการ", "สำนักงานใหญ่", "Organization", 10);

        AddNode(table, "DEP-100", "ORG-001", "การผลิต", "ฝ่ายผลิตและควบคุมคุณภาพ", "Department", 10);
        AddNode(table, "DEP-200", "ORG-001", "การตลาด", "ฝ่ายขายและการตลาด", "Department", 20);
        AddNode(table, "DEP-300", "ORG-001", "การเงิน", "บัญชี งบประมาณ และจัดซื้อ", "Department", 30);
        AddNode(table, "DEP-400", "ORG-001", "บุคคล", "ทรัพยากรบุคคลและธุรการ", "Department", 40);
        AddNode(table, "DEP-500", "ORG-001", "เทคโนโลยีสารสนเทศ", "ระบบงานและโครงสร้างพื้นฐาน", "Department", 50);
        AddNode(table, "DEP-600", "ORG-001", "คลังสินค้า", "คลังสินค้าและจัดส่ง", "Department", 60);

        AddNode(table, "POS-101", "DEP-100", "ผู้จัดการฝ่ายผลิต", "Production Manager", "Position", 10);
        AddNode(table, "TEAM-110", "DEP-100", "วางแผนการผลิต", "Production Planning", "Position", 20);
        AddNode(table, "TEAM-120", "DEP-100", "ควบคุมคุณภาพ", "Quality Control", "Position", 30);
        AddNode(table, "TEAM-130", "DEP-100", "ซ่อมบำรุง", "Maintenance", "Position", 40);
        AddNode(table, "EMP-101", "POS-101", "สมชาย ประเสริฐ", "หัวหน้าฝ่ายผลิต", "Person", 10);
        AddNode(table, "EMP-111", "TEAM-110", "อรทัย ใจดี", "เจ้าหน้าที่วางแผน", "Person", 10);
        AddNode(table, "EMP-112", "TEAM-110", "วิชัย พงศ์สุข", "เจ้าหน้าที่วางแผน", "Person", 20);
        AddNode(table, "EMP-121", "TEAM-120", "กมลชนก ศรีทอง", "เจ้าหน้าที่ QC", "Person", 10);
        AddNode(table, "EMP-131", "TEAM-130", "เดชา แสงชัย", "ช่างซ่อมบำรุง", "Person", 10);

        AddNode(table, "POS-201", "DEP-200", "ผู้จัดการฝ่ายขาย", "Sales Manager", "Position", 10);
        AddNode(table, "TEAM-210", "DEP-200", "ขายในประเทศ", "Domestic Sales", "Position", 20);
        AddNode(table, "TEAM-220", "DEP-200", "บริการลูกค้า", "Customer Service", "Position", 30);
        AddNode(table, "TEAM-230", "DEP-200", "วิจัยตลาด", "Market Research", "Position", 40);
        AddNode(table, "TEAM-240", "DEP-200", "สื่อสารการตลาด", "Marketing Communications", "Position", 50);
        AddNode(table, "EMP-201", "POS-201", "นฤมล แซ่ลี", "หัวหน้าฝ่ายขาย", "Person", 10);
        AddNode(table, "EMP-211", "TEAM-210", "ปกรณ์ วัฒนะ", "พนักงานขาย", "Person", 10);
        AddNode(table, "EMP-212", "TEAM-210", "มาลินี อินทร์แก้ว", "พนักงานขาย", "Person", 20);
        AddNode(table, "EMP-221", "TEAM-220", "ธันวา ชูเกียรติ", "เจ้าหน้าที่บริการลูกค้า", "Person", 10);
        AddNode(table, "EMP-231", "TEAM-230", "ศิริพร ตั้งใจ", "นักวิเคราะห์ตลาด", "Person", 10);
        AddNode(table, "EMP-241", "TEAM-240", "วราภรณ์ มีสุข", "เจ้าหน้าที่การตลาด", "Person", 10);

        AddNode(table, "POS-301", "DEP-300", "ผู้จัดการการเงิน", "Finance Manager", "Position", 10);
        AddNode(table, "TEAM-310", "DEP-300", "บัญชี", "Accounting", "Position", 20);
        AddNode(table, "TEAM-320", "DEP-300", "จัดซื้อ", "Purchasing", "Position", 30);
        AddNode(table, "TEAM-330", "DEP-300", "งบประมาณ", "Budget Control", "Position", 40);
        AddNode(table, "EMP-301", "POS-301", "กฤต วงศ์ชัย", "หัวหน้าฝ่ายการเงิน", "Person", 10);
        AddNode(table, "EMP-311", "TEAM-310", "พิมพ์ชนก แก้วใส", "เจ้าหน้าที่บัญชี", "Person", 10);
        AddNode(table, "EMP-321", "TEAM-320", "อาทิตย์ รุ่งเรือง", "เจ้าหน้าที่จัดซื้อ", "Person", 10);
        AddNode(table, "EMP-331", "TEAM-330", "จิราพร สมบูรณ์", "เจ้าหน้าที่งบประมาณ", "Person", 10);

        AddNode(table, "POS-401", "DEP-400", "ผู้จัดการฝ่ายบุคคล", "HR Manager", "Position", 10);
        AddNode(table, "TEAM-410", "DEP-400", "สรรหาและว่าจ้าง", "Recruitment", "Position", 20);
        AddNode(table, "TEAM-420", "DEP-400", "เงินเดือนและสวัสดิการ", "Payroll and Benefits", "Position", 30);
        AddNode(table, "TEAM-430", "DEP-400", "ฝึกอบรม", "Training", "Position", 40);
        AddNode(table, "EMP-401", "POS-401", "สุดารัตน์ ทองดี", "หัวหน้าฝ่ายบุคคล", "Person", 10);
        AddNode(table, "EMP-411", "TEAM-410", "ชลธิชา มากมี", "เจ้าหน้าที่สรรหา", "Person", 10);
        AddNode(table, "EMP-421", "TEAM-420", "ภัทรพล เจริญ", "เจ้าหน้าที่เงินเดือน", "Person", 10);
        AddNode(table, "EMP-431", "TEAM-430", "พัชรินทร์ แสงทอง", "เจ้าหน้าที่ฝึกอบรม", "Person", 10);

        AddNode(table, "POS-501", "DEP-500", "ผู้จัดการไอที", "IT Manager", "Position", 10);
        AddNode(table, "TEAM-510", "DEP-500", "พัฒนาระบบ", "Application Development", "Position", 20);
        AddNode(table, "TEAM-520", "DEP-500", "โครงสร้างพื้นฐาน", "Infrastructure", "Position", 30);
        AddNode(table, "TEAM-530", "DEP-500", "สนับสนุนผู้ใช้", "Helpdesk", "Position", 40);
        AddNode(table, "EMP-501", "POS-501", "ณัฐพล เที่ยงธรรม", "หัวหน้าฝ่ายไอที", "Person", 10);
        AddNode(table, "EMP-511", "TEAM-510", "รัตนา วงษ์ดี", "นักพัฒนาระบบ", "Person", 10);
        AddNode(table, "EMP-512", "TEAM-510", "ธนากร สายชล", "นักพัฒนาระบบ", "Person", 20);
        AddNode(table, "EMP-521", "TEAM-520", "เมธา กล้าหาญ", "ผู้ดูแลระบบ", "Person", 10);
        AddNode(table, "EMP-531", "TEAM-530", "ปรียา นิ่มนวล", "เจ้าหน้าที่ Helpdesk", "Person", 10);

        AddNode(table, "POS-601", "DEP-600", "ผู้จัดการคลังสินค้า", "Warehouse Manager", "Position", 10);
        AddNode(table, "TEAM-610", "DEP-600", "รับสินค้า", "Receiving", "Position", 20);
        AddNode(table, "TEAM-620", "DEP-600", "จัดเก็บสินค้า", "Storage", "Position", 30);
        AddNode(table, "TEAM-630", "DEP-600", "จัดส่ง", "Delivery", "Position", 40);
        AddNode(table, "EMP-601", "POS-601", "มนตรี เกษมสุข", "หัวหน้าคลังสินค้า", "Person", 10);
        AddNode(table, "EMP-611", "TEAM-610", "สมนึก ชาญชัย", "เจ้าหน้าที่รับสินค้า", "Person", 10);
        AddNode(table, "EMP-621", "TEAM-620", "ลัดดา พร้อมพงษ์", "เจ้าหน้าที่คลังสินค้า", "Person", 10);
        AddNode(table, "EMP-631", "TEAM-630", "เอกชัย ทรัพย์มาก", "เจ้าหน้าที่จัดส่ง", "Person", 10);

        return table;
    }

    private static void AddNode(
        DataTable table,
        string id,
        string? parentId,
        string displayName,
        string subtitle,
        string kind,
        int sortOrder)
    {
        table.Rows.Add(id, parentId == null ? DBNull.Value : parentId, displayName, subtitle, kind, sortOrder);
    }

    private static DataTable CreateSampleOrbatTable()
    {
        var table = CreateOrbatSchemaTable();

        AddUnit(table, "III-CORPS", null, "III Corps", "III Corps", "", "Friend", "Corps", "Headquarters", true, false, false, false, 10);
        AddUnit(table, "CJTF-HQ", "III-CORPS", "Combined Joint Task Force HQ", "CJTF HQ", "CJTF HQ", "Hostile", "Corps", "Headquarters", true, true, false, false, 10);
        AddUnit(table, "MAR-HQ", "III-CORPS", "Maritime Component HQ", "MAR HQ", "", "Friend", "Brigade", "Naval", true, false, false, false, 20);
        AddUnit(table, "AIR-HQ", "III-CORPS", "Air Component HQ", "AIR HQ", "", "Friend", "Brigade", "Air", true, false, false, false, 30);
        AddUnit(table, "SOF-TF", "III-CORPS", "Joint Special Operations Task Force", "SOF TF", "", "Hostile", "Brigade", "SpecialOperations", true, true, false, false, 40);

        AddUnit(table, "3-BDE", "III-CORPS", "3 Brigade Combat Team", "3 BCT", "3", "Friend", "Brigade", "Infantry", false, false, false, false, 100);
        AddUnit(table, "4-MECH", "III-CORPS", "4 Mechanized Brigade", "4 Mech", "4", "Friend", "Brigade", "MechanizedInfantry", false, false, true, false, 110);
        AddUnit(table, "UK-ARMOR", "III-CORPS", "UK Armoured Brigade", "UK Armor", "UK", "Hostile", "Brigade", "Armor", false, false, false, false, 120);
        AddUnit(table, "420-INF", "III-CORPS", "420 Infantry Battalion", "420 Inf", "420", "Friend", "Battalion", "Infantry", false, false, false, false, 130);
        AddUnit(table, "504-MED", "III-CORPS", "504 Medical Battalion", "504 Med", "504", "Friend", "Battalion", "Medical", false, false, false, false, 140);
        AddUnit(table, "31-ENG", "III-CORPS", "31 Engineer Battalion", "31 Eng", "31", "Friend", "Battalion", "Engineer", false, false, false, false, 150);
        AddUnit(table, "2-AVN", "III-CORPS", "2 Aviation Battalion", "2 Avn", "2", "Friend", "Battalion", "Aviation", false, false, false, false, 160);

        AddUnit(table, "3-SIG", "3-BDE", "3 Signal Company", "Signal", "", "Friend", "Company", "Signal", false, false, false, false, 10);
        AddUnit(table, "SRT", "3-SIG", "Signal Relay Team", "SRT", "", "Friend", "Team", "Signal", false, false, false, false, 10);
        AddUnit(table, "ARE", "3-SIG", "Area Relay Element", "ARE", "", "Friend", "Team", "Signal", false, false, false, false, 20);
        AddUnit(table, "89-MP", "III-CORPS", "89 Military Police Company", "89 MP", "89", "Friend", "Company", "MilitaryPolice", false, false, false, false, 200);
        AddUnit(table, "460-ARTY", "III-CORPS", "460 Field Artillery Battalion", "460 FA", "460", "Friend", "Battalion", "Artillery", false, false, false, false, 210);
        AddUnit(table, "3-PSYOP", "III-CORPS", "3 Psychological Operations Company", "3 PSYOP", "3", "Friend", "Company", "PsychologicalOperations", false, false, false, false, 220);
        AddUnit(table, "13-LOG", "III-CORPS", "13 Combat Service Support", "13 CSS", "13", "Friend", "Battalion", "Logistics", false, false, false, false, 230);
        AddUnit(table, "13-CBRN", "III-CORPS", "13 CBRN Company", "13 CBRN", "13", "Friend", "Company", "CBRN", false, false, false, false, 240);
        AddUnit(table, "13-ORD", "III-CORPS", "13 Ordnance Company", "13 Ord", "13", "Friend", "Company", "Ordnance", false, false, false, false, 250);
        AddUnit(table, "13-QM", "III-CORPS", "13 Quartermaster Company", "13 QM", "13", "Friend", "Company", "Quartermaster", false, false, false, false, 260);

        return table;
    }

    private static DataTable EnsureCurrentDemoUnits(DataTable table)
    {
        if (HasOrbatRow(table, "III-CORPS") && !HasOrbatRow(table, "13-CBRN"))
            AddUnit(table, "13-CBRN", "III-CORPS", "13 CBRN Company", "13 CBRN", "13", "Friend", "Company", "CBRN", false, false, false, false, 240);

        if (HasOrbatRow(table, "III-CORPS") && !HasOrbatRow(table, "2-AVN"))
            AddUnit(table, "2-AVN", "III-CORPS", "2 Aviation Battalion", "2 Avn", "2", "Friend", "Battalion", "Aviation", false, false, false, false, 160);

        if (HasOrbatRow(table, "III-CORPS") && !HasOrbatRow(table, "13-ORD"))
            AddUnit(table, "13-ORD", "III-CORPS", "13 Ordnance Company", "13 Ord", "13", "Friend", "Company", "Ordnance", false, false, false, false, 250);

        if (HasOrbatRow(table, "III-CORPS") && !HasOrbatRow(table, "13-QM"))
            AddUnit(table, "13-QM", "III-CORPS", "13 Quartermaster Company", "13 QM", "13", "Friend", "Company", "Quartermaster", false, false, false, false, 260);

        return table;
    }

    private static bool HasOrbatRow(DataTable table, string id)
    {
        return table.Rows
            .Cast<DataRow>()
            .Any(row => string.Equals(Convert.ToString(row["Id"]), id, StringComparison.OrdinalIgnoreCase));
    }

    private static DataTable CreateOrbatSchemaTable()
    {
        var table = new DataTable("Orbat");
        table.Columns.Add("Id", typeof(string));
        table.Columns.Add("ParentId", typeof(string));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("ShortName", typeof(string));
        table.Columns.Add("UniqueDesignation", typeof(string));
        table.Columns.Add("Affiliation", typeof(string));
        table.Columns.Add("Echelon", typeof(string));
        table.Columns.Add("UnitType", typeof(string));
        table.Columns.Add("Sidc", typeof(string));
        table.Columns.Add("SymbolText", typeof(string));
        table.Columns.Add("Headquarters", typeof(bool));
        table.Columns.Add("TaskForce", typeof(bool));
        table.Columns.Add("PlannedAnticipated", typeof(bool));
        table.Columns.Add("StackCount", typeof(int));
        table.Columns.Add("ReinforcedReduced", typeof(string));
        table.Columns.Add("Reinforced", typeof(bool));
        table.Columns.Add("Reduced", typeof(bool));
        table.Columns.Add("SortOrder", typeof(int));

        return table;
    }

    private static DataTable CreateSymbolGalleryTable()
    {
        var table = CreateOrbatSchemaTable();
        AddUnit(table, "SYM-ROOT", null, "Military Symbols", "Symbols", "", "Friend", "Command", "Headquarters", true, false, false, false, 10);
        AddGalleryGroup(table, "SYM-COMBAT", "SYM-ROOT", "Combat", 10);
        AddGalleryGroup(table, "SYM-SUPPORT", "SYM-ROOT", "Support", 20);
        AddGalleryGroup(table, "SYM-DOMAIN", "SYM-ROOT", "Domain/Special", 30);

        AddSymbolCatalogUnit(table, "SYM-HQ", "SYM-COMBAT", OrbatUnitType.Headquarters, 10);
        AddSymbolCatalogUnit(table, "SYM-INF", "SYM-COMBAT", OrbatUnitType.Infantry, 20);
        AddSymbolCatalogUnit(table, "SYM-ARM", "SYM-COMBAT", OrbatUnitType.Armor, 30);
        AddSymbolCatalogUnit(table, "SYM-MECH", "SYM-COMBAT", OrbatUnitType.MechanizedInfantry, 40);
        AddSymbolCatalogUnit(table, "SYM-ARTY", "SYM-COMBAT", OrbatUnitType.Artillery, 50);
        AddSymbolCatalogUnit(table, "SYM-AD", "SYM-COMBAT", OrbatUnitType.AirDefense, 60);
        AddSymbolCatalogUnit(table, "SYM-AVN", "SYM-COMBAT", OrbatUnitType.Aviation, 70);
        AddSymbolCatalogUnit(table, "SYM-ENG", "SYM-COMBAT", OrbatUnitType.Engineer, 80);
        AddSymbolCatalogUnit(table, "SYM-RECON", "SYM-COMBAT", OrbatUnitType.Reconnaissance, 90);
        AddSymbolCatalogUnit(table, "SYM-SIG", "SYM-COMBAT", OrbatUnitType.Signal, 100);

        AddSymbolCatalogUnit(table, "SYM-MP", "SYM-SUPPORT", OrbatUnitType.MilitaryPolice, 10);
        AddSymbolCatalogUnit(table, "SYM-MED", "SYM-SUPPORT", OrbatUnitType.Medical, 20);
        AddSymbolCatalogUnit(table, "SYM-CBRN", "SYM-SUPPORT", OrbatUnitType.CBRN, 30);
        AddSymbolCatalogUnit(table, "SYM-LOG", "SYM-SUPPORT", OrbatUnitType.Logistics, 40);
        AddSymbolCatalogUnit(table, "SYM-ORD", "SYM-SUPPORT", OrbatUnitType.Ordnance, 50);
        AddSymbolCatalogUnit(table, "SYM-QM", "SYM-SUPPORT", OrbatUnitType.Quartermaster, 60);
        AddSymbolCatalogUnit(table, "SYM-MAINT", "SYM-SUPPORT", OrbatUnitType.Maintenance, 70);
        AddSymbolCatalogUnit(table, "SYM-TRANS", "SYM-SUPPORT", OrbatUnitType.Transportation, 80);
        AddSymbolCatalogUnit(table, "SYM-INT", "SYM-SUPPORT", OrbatUnitType.Intelligence, 90);
        AddSymbolCatalogUnit(table, "SYM-PSYOP", "SYM-SUPPORT", OrbatUnitType.PsychologicalOperations, 100);

        AddSymbolCatalogUnit(table, "SYM-SOF", "SYM-DOMAIN", OrbatUnitType.SpecialOperations, 10);
        AddSymbolCatalogUnit(table, "SYM-NAV", "SYM-DOMAIN", OrbatUnitType.Naval, 20);
        AddSymbolCatalogUnit(table, "SYM-AIR", "SYM-DOMAIN", OrbatUnitType.Air, 30);
        AddSymbolCatalogUnit(table, "SYM-CYB", "SYM-DOMAIN", OrbatUnitType.Cyber, 40);
        AddSymbolCatalogUnit(table, "SYM-UNSPEC", "SYM-DOMAIN", OrbatUnitType.Unspecified, 50);

        return table;
    }

    private static void AddGalleryGroup(DataTable table, string id, string parentId, string name, int sortOrder)
    {
        AddUnit(table, id, parentId, name, name, "", "Unspecified", "Unspecified", "Unspecified", false, false, false, false, sortOrder);
    }

    private static void AddSymbolCatalogUnit(DataTable table, string id, string parentId, OrbatUnitType unitType, int sortOrder)
    {
        var echelon = unitType == OrbatUnitType.Headquarters ? OrbatEchelon.Brigade : OrbatEchelon.Company;
        var headquarters = unitType == OrbatUnitType.Headquarters;
        var unitTypeCode = GetUnitTypeCode(unitType);
        var name = unitType.ToString();

        AddUnit(
            table,
            id,
            parentId,
            name,
            unitTypeCode,
            unitTypeCode,
            "Friend",
            echelon.ToString(),
            unitType.ToString(),
            headquarters,
            false,
            false,
            false,
            sortOrder);

        var row = table.Rows[table.Rows.Count - 1];
        row["Sidc"] = OrbatSidcParser.Compose(OrbatAffiliation.Friend, echelon, unitType, headquarters, false, false);
    }

    private static void AddUnit(
        DataTable table,
        string id,
        string? parentId,
        string name,
        string shortName,
        string uniqueDesignation,
        string affiliation,
        string echelon,
        string unitType,
        bool headquarters,
        bool taskForce,
        bool reinforced,
        bool reduced,
        int sortOrder)
    {
        table.Rows.Add(
            id,
            parentId == null ? DBNull.Value : parentId,
            name,
            shortName,
            uniqueDesignation,
            affiliation,
            echelon,
            unitType,
            string.Empty,
            string.Empty,
            headquarters,
            taskForce,
            false,
            1,
            GetReinforcedReducedValue(reinforced, reduced),
            reinforced,
            reduced,
            sortOrder);
    }

    private static string GetReinforcedReducedValue(bool reinforced, bool reduced)
    {
        if (reinforced && reduced)
            return nameof(OrbatReinforcedReduced.ReinforcedAndReduced);
        if (reinforced)
            return nameof(OrbatReinforcedReduced.Reinforced);
        return reduced ? nameof(OrbatReinforcedReduced.Reduced) : nameof(OrbatReinforcedReduced.NotApplicable);
    }

}

internal sealed class OrbatUnitFormat
{
    public string Affiliation { get; set; } = nameof(OrbatAffiliation.Friend);
    public string Echelon { get; set; } = nameof(OrbatEchelon.Unspecified);
    public string UnitType { get; set; } = nameof(OrbatUnitType.Unspecified);
    public string Sidc { get; set; } = string.Empty;
    public string SymbolText { get; set; } = string.Empty;
    public bool Headquarters { get; set; }
    public bool TaskForce { get; set; }
    public bool PlannedAnticipated { get; set; }
    public int StackCount { get; set; } = 1;
    public string ReinforcedReduced { get; set; } = nameof(OrbatReinforcedReduced.NotApplicable);

    public bool IsUsable =>
        !string.IsNullOrWhiteSpace(Affiliation)
        && !string.IsNullOrWhiteSpace(Echelon)
        && !string.IsNullOrWhiteSpace(UnitType);

    public static OrbatUnitFormat FromRecord(OrbatUnitRecord unit)
    {
        return new OrbatUnitFormat
        {
            Affiliation = unit.Affiliation.ToString(),
            Echelon = unit.Echelon.ToString(),
            UnitType = unit.UnitType.ToString(),
            Sidc = unit.Sidc ?? string.Empty,
            SymbolText = unit.SymbolText ?? string.Empty,
            Headquarters = unit.Headquarters,
            TaskForce = unit.TaskForce,
            PlannedAnticipated = unit.PlannedAnticipated,
            StackCount = unit.StackCount,
            ReinforcedReduced = unit.ReinforcedReduced.ToString()
        };
    }

    public static OrbatUnitFormat FromSidc(string sidc)
    {
        var parsed = OrbatSidcParser.Parse(sidc);
        return new OrbatUnitFormat
        {
            Affiliation = (parsed.Affiliation ?? OrbatAffiliation.Friend).ToString(),
            Echelon = (parsed.Echelon ?? OrbatEchelon.Unspecified).ToString(),
            UnitType = (parsed.UnitType ?? OrbatUnitType.Unspecified).ToString(),
            Sidc = parsed.Sidc,
            Headquarters = parsed.Headquarters ?? false,
            TaskForce = parsed.TaskForce ?? false,
            PlannedAnticipated = parsed.PlannedAnticipated ?? false
        };
    }
}

internal sealed class OrbatSubtreeClipboard
{
    public string SourceRootId { get; set; } = string.Empty;
    public List<OrbatUnitTemplate> Units { get; set; } = new();

    public bool IsUsable => Units.Count > 0 && Units.All(unit => !string.IsNullOrWhiteSpace(unit.Id));
}

internal sealed class OrbatUnitTemplate
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string UniqueDesignation { get; set; } = string.Empty;
    public string Affiliation { get; set; } = nameof(OrbatAffiliation.Friend);
    public string Echelon { get; set; } = nameof(OrbatEchelon.Unspecified);
    public string UnitType { get; set; } = nameof(OrbatUnitType.Unspecified);
    public string Sidc { get; set; } = string.Empty;
    public string SymbolText { get; set; } = string.Empty;
    public bool Headquarters { get; set; }
    public bool TaskForce { get; set; }
    public bool PlannedAnticipated { get; set; }
    public int StackCount { get; set; } = 1;
    public string ReinforcedReduced { get; set; } = nameof(OrbatReinforcedReduced.NotApplicable);
    public int SortOrder { get; set; } = 10;

    public static OrbatUnitTemplate FromRow(DataRow row)
    {
        return new OrbatUnitTemplate
        {
            Id = ReadString(row, "Id"),
            ParentId = ReadNullableString(row, "ParentId"),
            Name = ReadString(row, "Name"),
            ShortName = ReadString(row, "ShortName"),
            UniqueDesignation = ReadString(row, "UniqueDesignation"),
            Affiliation = ReadString(row, "Affiliation", nameof(OrbatAffiliation.Friend)),
            Echelon = ReadString(row, "Echelon", nameof(OrbatEchelon.Unspecified)),
            UnitType = ReadString(row, "UnitType", nameof(OrbatUnitType.Unspecified)),
            Sidc = ReadString(row, "Sidc"),
            SymbolText = ReadString(row, "SymbolText"),
            Headquarters = ReadBoolean(row, "Headquarters"),
            TaskForce = ReadBoolean(row, "TaskForce"),
            PlannedAnticipated = ReadBoolean(row, "PlannedAnticipated"),
            StackCount = Math.Max(1, Math.Min(6, ReadInteger(row, "StackCount", 1))),
            ReinforcedReduced = ReadString(row, "ReinforcedReduced", nameof(OrbatReinforcedReduced.NotApplicable)),
            SortOrder = ReadInteger(row, "SortOrder", 10)
        };
    }

    public OrbatUnitDraft ToDraft(string newId, string parentId, int sortOrder)
    {
        var draft = new OrbatUnitDraft
        {
            Id = newId,
            ParentId = parentId,
            Name = ShouldUseGeneratedUnitName(Name, Id) ? newId : Name,
            ShortName = ShouldUseGeneratedUnitName(ShortName, Id) ? newId : ShortName,
            UniqueDesignation = UniqueDesignation,
            Affiliation = ParseEnum(Affiliation, OrbatAffiliation.Friend),
            Echelon = ParseEnum(Echelon, OrbatEchelon.Unspecified),
            UnitType = ParseUnitType(UnitType),
            Sidc = Sidc,
            SymbolText = SymbolText,
            Headquarters = Headquarters,
            TaskForce = TaskForce,
            PlannedAnticipated = PlannedAnticipated,
            StackCount = Math.Max(1, Math.Min(6, StackCount)),
            ReinforcedReduced = ParseEnum(ReinforcedReduced, OrbatReinforcedReduced.NotApplicable),
            SortOrder = sortOrder
        };

        if (string.IsNullOrWhiteSpace(draft.Sidc))
            draft.Sidc = OrbatSidcParser.Compose(draft.Affiliation, draft.Echelon, draft.UnitType, draft.Headquarters, draft.TaskForce, draft.PlannedAnticipated);

        return draft;
    }

    private static string? ReadNullableString(DataRow row, string columnName)
    {
        return !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value
            ? null
            : Convert.ToString(row[columnName]);
    }

    private static string ReadString(DataRow row, string columnName, string fallback = "")
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            return fallback;

        var text = Convert.ToString(row[columnName]);
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static bool ReadBoolean(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            return false;

        return Convert.ToBoolean(row[columnName]);
    }

    private static int ReadInteger(DataRow row, string columnName, int fallback)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            return fallback;

        return int.TryParse(Convert.ToString(row[columnName]), out var value) ? value : fallback;
    }

    private static OrbatUnitType ParseUnitType(string value)
    {
        return value.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? OrbatUnitType.Unspecified
            : ParseEnum(value, OrbatUnitType.Unspecified);
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct
    {
        if (Enum.TryParse(value, true, out TEnum parsed))
            return parsed;

        var normalized = value.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
        return Enum.TryParse(normalized, true, out parsed) ? parsed : fallback;
    }

    private static bool ShouldUseGeneratedUnitName(string value, string currentId)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.Equals("New Unit", StringComparison.OrdinalIgnoreCase)
            || value.Equals(currentId, StringComparison.OrdinalIgnoreCase);
    }
}
