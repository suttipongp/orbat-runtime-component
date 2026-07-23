using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrgHierarchy.Components;

namespace OrgHierarchy.Demo;

public sealed class SymbolLibraryViewerForm : Form
{
    private static readonly string SettingsFileName = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OrgHierarchy.Demo",
        "symbol-library-viewer.json");

    private readonly TextBox _libraryPathTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly Button _browseFolderButton = new() { Text = "Browse folder", AutoSize = true };
    private readonly Button _openFilesButton = new() { Text = "Open files", AutoSize = true };
    private readonly Button _reloadButton = new() { Text = "Reload", AutoSize = true };
    private readonly Button _validateButton = new() { Text = "Validate", AutoSize = true };
    private readonly Button _editInDesignerButton = new() { Text = "Edit in designer", AutoSize = true };
    private readonly Button _deleteFileButton = new() { Text = "Delete file", AutoSize = true };
    private readonly Label _statusLabel = new() { Dock = DockStyle.Fill, ForeColor = SystemColors.GrayText, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ListView _symbolListView = new();
    private readonly ImageList _thumbnailImages = new();
    private readonly SymbolPreviewControl _preview = new();
    private readonly Label _nameLabel = CreateValueLabel();
    private readonly Label _symbolKindLabel = new() { Text = "Main function", AutoSize = true, Margin = new Padding(0, 6, 8, 0) };
    private readonly Label _unitTypeLabel = CreateValueLabel();
    private readonly Label _frameLabel = CreateValueLabel();
    private readonly Label _statusValueLabel = CreateValueLabel();
    private readonly Label _commandCountLabel = CreateValueLabel();
    private readonly Label _libraryIdLabel = CreateValueLabel();
    private readonly Label _libraryVersionLabel = CreateValueLabel();
    private readonly TextBox _fileTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly ListBox _commandListBox = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();
    private string[] _loadedFiles = Array.Empty<string>();
    private SymbolLibraryValidationReport _lastValidationReport = SymbolLibraryValidationReport.Empty;

    public SymbolLibraryViewerForm()
    {
        Text = "ORBAT Symbol Library";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(980, 680);
        Size = new Size(1280, 820);

        _thumbnailImages.ImageSize = new Size(180, 120);
        _thumbnailImages.ColorDepth = ColorDepth.Depth32Bit;

        _symbolListView.Dock = DockStyle.Fill;
        _symbolListView.View = View.LargeIcon;
        _symbolListView.MultiSelect = false;
        _symbolListView.HideSelection = false;
        _symbolListView.ShowGroups = true;
        _symbolListView.LargeImageList = _thumbnailImages;
        _symbolListView.SelectedIndexChanged += (_, _) => ShowSelectedSymbol();
        _symbolListView.DoubleClick += (_, _) => EditSelectedInDesigner();

        _preview.Dock = DockStyle.Fill;
        _preview.BackColor = Color.White;
        _preview.PreviewScale = 1.5f;

        _browseFolderButton.Click += (_, _) => BrowseFolder();
        _openFilesButton.Click += (_, _) => OpenFiles();
        _reloadButton.Click += (_, _) => ReloadCurrentLibrary();
        _validateButton.Click += (_, _) => ShowValidationReport();
        _editInDesignerButton.Click += (_, _) => EditSelectedInDesigner();
        _deleteFileButton.Click += (_, _) => DeleteSelectedFile();

        Controls.Add(CreateMainLayout());
        Load += (_, _) => LoadRecentLibrary();
    }

    private Control CreateMainLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        root.Controls.Add(CreateToolbar(), 0, 0);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 560
        };
        split.Panel1.Controls.Add(_symbolListView);
        split.Panel2.Controls.Add(CreateDetailPanel());
        root.Controls.Add(split, 0, 1);
        root.Controls.Add(_statusLabel, 0, 2);
        return root;
    }

    private Control CreateToolbar()
    {
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102));

        toolbar.Controls.Add(new Label { Text = "Library", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 7, 8, 0) }, 0, 0);
        toolbar.Controls.Add(_libraryPathTextBox, 1, 0);
        toolbar.Controls.Add(_browseFolderButton, 2, 0);
        toolbar.Controls.Add(_openFilesButton, 3, 0);
        toolbar.Controls.Add(_reloadButton, 4, 0);
        toolbar.Controls.Add(_validateButton, 5, 0);
        toolbar.Controls.Add(_editInDesignerButton, 6, 0);
        toolbar.Controls.Add(_deleteFileButton, 7, 0);
        return toolbar;
    }

    private Control CreateDetailPanel()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var previewTab = new TabPage("Preview") { Padding = new Padding(8) };
        var commandsTab = new TabPage("Commands") { Padding = new Padding(8) };

        previewTab.Controls.Add(CreatePreviewPanel());
        commandsTab.Controls.Add(_commandListBox);
        tabs.Controls.Add(previewTab);
        tabs.Controls.Add(commandsTab);
        return tabs;
    }

    private Control CreatePreviewPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        panel.Controls.Add(_preview, 0, 0);
        panel.Controls.Add(CreateMetadataPanel(), 0, 1);
        return panel;
    }

    private Control CreateMetadataPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(0, 8, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddMetadataRow(panel, 0, "Name", _nameLabel);
        AddMetadataRow(panel, 1, _symbolKindLabel, _unitTypeLabel);
        AddMetadataRow(panel, 2, "Frame", _frameLabel);
        AddMetadataRow(panel, 3, "Status", _statusValueLabel);
        AddMetadataRow(panel, 4, "Commands", _commandCountLabel);
        AddMetadataRow(panel, 5, "Library ID", _libraryIdLabel);
        AddMetadataRow(panel, 6, "Revision", _libraryVersionLabel);
        AddMetadataRow(panel, 7, "File", _fileTextBox);
        return panel;
    }

    private static void AddMetadataRow(TableLayoutPanel panel, int row, string label, Control value)
    {
        AddMetadataRow(panel, row, new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 8, 0) }, value);
    }

    private static void AddMetadataRow(TableLayoutPanel panel, int row, Control label, Control value)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 7 ? 56 : 30));
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(value, 1, row);
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a folder containing ORBAT symbol library files."
        };

        if (Directory.Exists(_libraryPathTextBox.Text))
            dialog.SelectedPath = _libraryPathTextBox.Text;

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        LoadFolder(dialog.SelectedPath);
        SaveRecentLibrary(SymbolLibraryLoadMode.Folder, dialog.SelectedPath, Array.Empty<string>());
    }

    private void OpenFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open ORBAT symbol library files",
            Filter = "ORBAT symbol library|*.orbatsymbol.json;*.json|All files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        LoadFiles(dialog.FileNames);
        _libraryPathTextBox.Text = Path.GetDirectoryName(dialog.FileNames[0]) ?? string.Empty;
        SaveRecentLibrary(SymbolLibraryLoadMode.Files, _libraryPathTextBox.Text, dialog.FileNames);
    }

    private void ShowValidationReport()
    {
        if (_loadedFiles.Length == 0)
        {
            MessageBox.Show(this, "Open a symbol library folder or files first.", "Symbol Library Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _lastValidationReport = SymbolLibraryValidator.ValidateFiles(_loadedFiles, _jsonOptions);
        using var form = new SymbolLibraryValidationForm(_lastValidationReport);
        form.ShowDialog(this);
        _statusLabel.Text = $"Validation: {_lastValidationReport.ErrorCount} error(s), {_lastValidationReport.WarningCount} warning(s), {_lastValidationReport.InfoCount} info.";
    }

    private void ReloadCurrentLibrary()
    {
        if (_loadedFiles.Length > 0)
        {
            LoadFiles(_loadedFiles);
            return;
        }

        if (Directory.Exists(_libraryPathTextBox.Text))
            LoadFolder(_libraryPathTextBox.Text);
    }

    private void EditSelectedInDesigner()
    {
        if (_symbolListView.SelectedItems.Count == 0 || _symbolListView.SelectedItems[0].Tag is not SymbolLibraryItem item)
        {
            MessageBox.Show(this, "Please select a symbol to edit.", "Symbol Library", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (Owner is SymbolDesignerForm ownerDesigner)
        {
            ownerDesigner.LoadLibraryFile(item.FileName);
            Close();
            return;
        }

        using var form = new SymbolDesignerForm(item.FileName);
        form.ShowDialog(this);
        ReloadCurrentLibrary();
    }

    private void DeleteSelectedFile()
    {
        if (_symbolListView.SelectedItems.Count == 0 || _symbolListView.SelectedItems[0].Tag is not SymbolLibraryItem item)
        {
            MessageBox.Show(this, "Please select a symbol file to delete.", "Symbol Library", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!File.Exists(item.FileName))
        {
            MessageBox.Show(this, "The selected file no longer exists.", "Symbol Library", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadCurrentLibrary();
            return;
        }

        var message = $"Delete this symbol library file?\n\n{item.DisplayName}\n{item.FileName}";
        var result = MessageBox.Show(this, message, "Delete symbol library file", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (result != DialogResult.Yes)
            return;

        try
        {
            File.Delete(item.FileName);
            _loadedFiles = _loadedFiles.Where(file => !file.Equals(item.FileName, StringComparison.OrdinalIgnoreCase)).ToArray();
            _statusLabel.Text = $"Deleted {Path.GetFileName(item.FileName)}.";
            ReloadCurrentLibrary();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not delete the selected file.\n\n{ex.Message}", "Symbol Library", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadRecentLibrary()
    {
        var settings = LoadSettings();
        if (settings == null)
        {
            if (TryLoadDefaultLibrary())
                return;

            _statusLabel.Text = "Choose a symbol library folder or open symbol files.";
            return;
        }

        if (settings.Mode == SymbolLibraryLoadMode.Files && settings.Files.Count > 0)
        {
            var files = settings.Files.Where(File.Exists).ToArray();
            if (files.Length > 0)
            {
                LoadFiles(files);
                _libraryPathTextBox.Text = Directory.Exists(settings.Folder)
                    ? settings.Folder
                    : Path.GetDirectoryName(files[0]) ?? string.Empty;
                return;
            }
        }

        if (Directory.Exists(settings.Folder))
        {
            LoadFolder(settings.Folder);
            return;
        }

        if (!TryLoadDefaultLibrary())
            _statusLabel.Text = "Recent library was not found. Choose a folder or open symbol files.";
    }

    private bool TryLoadDefaultLibrary()
    {
        var folder = SymbolLibraryLocator.FindDefaultFolder();
        if (string.IsNullOrWhiteSpace(folder))
            return false;

        LoadFolder(folder);
        SaveRecentLibrary(SymbolLibraryLoadMode.Folder, folder, Array.Empty<string>());
        return true;
    }

    private void LoadFolder(string folder)
    {
        _libraryPathTextBox.Text = folder;
        var files = Directory
            .EnumerateFiles(folder, "*.orbatsymbol.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName)
            .ToArray();
        LoadFiles(files);
    }

    private static SymbolLibraryViewerSettings? LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFileName))
                return null;

            return JsonSerializer.Deserialize<SymbolLibraryViewerSettings>(File.ReadAllText(SettingsFileName, Encoding.UTF8));
        }
        catch
        {
            return null;
        }
    }

    private static void SaveRecentLibrary(SymbolLibraryLoadMode mode, string folder, IReadOnlyList<string> files)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFileName);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var settings = new SymbolLibraryViewerSettings
            {
                Mode = mode,
                Folder = folder,
                Files = files.ToList()
            };
            File.WriteAllText(SettingsFileName, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }
        catch
        {
            // Recent-library persistence is a convenience; keep the viewer usable if settings cannot be written.
        }
    }

    private void LoadFiles(IReadOnlyList<string> files)
    {
        _loadedFiles = files.ToArray();
        _lastValidationReport = SymbolLibraryValidator.ValidateFiles(_loadedFiles, _jsonOptions);
        _symbolListView.BeginUpdate();
        try
        {
            _symbolListView.Items.Clear();
            _symbolListView.Groups.Clear();
            _thumbnailImages.Images.Clear();
            _commandListBox.Items.Clear();
            ClearDetails();

            var groups = CreateSymbolGroups();
            var skipped = 0;
            var items = new List<SymbolLibraryItem>();
            foreach (var file in files)
            {
                if (!TryLoadItem(file, out var item))
                {
                    skipped++;
                    continue;
                }

                items.Add(item);
            }

            foreach (var item in items
            .OrderBy(item => item.DomainSortOrder)
            .ThenBy(item => item.PrimarySortText, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.SecondarySortText, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => Path.GetFileName(item.FileName), StringComparer.CurrentCultureIgnoreCase))
            {
                var imageKey = item.FileName;
                _thumbnailImages.Images.Add(imageKey, RenderThumbnail(item.Definition));
                var listItem = new ListViewItem(item.DisplayName, imageKey)
                {
                    Tag = item,
                    ToolTipText = item.FileName
                };
                listItem.Group = groups[GetGroupKey(item.Definition)];
                listItem.SubItems.Add(item.PrimarySortText);
                _symbolListView.Items.Add(listItem);
            }

            if (_symbolListView.Items.Count > 0)
                _symbolListView.Items[0].Selected = true;

            var loaded = _symbolListView.Items.Count;
            var loadSummary = skipped == 0
                ? $"Loaded {loaded} symbol(s)."
                : $"Loaded {loaded} symbol(s). Skipped {skipped} invalid file(s).";
            _statusLabel.Text = $"{loadSummary} Validation: {_lastValidationReport.ErrorCount} error(s), {_lastValidationReport.WarningCount} warning(s).";
        }
        finally
        {
            _symbolListView.EndUpdate();
        }
    }

    private Dictionary<string, ListViewGroup> CreateSymbolGroups()
    {
        var groups = new Dictionary<string, ListViewGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in Enum.GetValues<OrbatUnitMainFunctionCategory>()
            .Where(value => value != OrbatUnitMainFunctionCategory.All))
        {
            AddSymbolGroup(
                groups,
                $"LandUnitMain.{category}",
                $"Land Unit - {OrbatUnitMainFunctionCatalog.GetCategoryDisplayName(category)}");
        }
        AddSymbolGroup(groups, "LandUnitModifier1", "Land Unit - Sector 1 (Modifier 1)");
        AddSymbolGroup(groups, "LandUnitModifier2", "Land Unit - Sector 2 (Modifier 2)");
        AddSymbolGroup(groups, "LandUnitEchelon", "Land Unit - Amplifier B (Echelon)");
        AddSymbolGroup(groups, "EquipmentMain", "Equipment - Main symbols");
        AddSymbolGroup(groups, "EquipmentComposite", "Equipment - Composite symbols");
        AddSymbolGroup(groups, "EquipmentModifier1", "Equipment - Modifier 1");
        AddSymbolGroup(groups, "EquipmentModifier2", "Equipment - Modifier 2");
        AddSymbolGroup(groups, "EquipmentMobility", "Equipment - Amplifier R (Mobility)");
        AddSymbolGroup(groups, "Other", "Other");
        return groups;
    }

    private void AddSymbolGroup(Dictionary<string, ListViewGroup> groups, string key, string header)
    {
        var group = new ListViewGroup(key, header);
        groups.Add(key, group);
        _symbolListView.Groups.Add(group);
    }

    private static string GetGroupKey(SymbolLibraryDefinition definition)
    {
        return definition.GetEffectivePhysicalDomain() switch
        {
            SymbolPhysicalDomain.LandUnit => definition.SymbolRole switch
            {
                OrbatEquipmentSymbolRole.Modifier1 => "LandUnitModifier1",
                OrbatEquipmentSymbolRole.Modifier2 => "LandUnitModifier2",
                OrbatEquipmentSymbolRole.EchelonIndicator => "LandUnitEchelon",
                _ => $"LandUnitMain.{definition.GetEffectiveUnitCategory()}"
            },
            SymbolPhysicalDomain.Equipment => definition.SymbolRole switch
            {
                OrbatEquipmentSymbolRole.Modifier1 => "EquipmentModifier1",
                OrbatEquipmentSymbolRole.Modifier2 => "EquipmentModifier2",
                OrbatEquipmentSymbolRole.MobilityIndicator => "EquipmentMobility",
                OrbatEquipmentSymbolRole.Composite => "EquipmentComposite",
                _ => "EquipmentMain"
            },
            _ => "Other"
        };
    }

    private bool TryLoadItem(string file, out SymbolLibraryItem item)
    {
        item = default!;
        try
        {
            var definition = JsonSerializer.Deserialize<SymbolLibraryDefinition>(File.ReadAllText(file, Encoding.UTF8), _jsonOptions);
            if (definition == null)
                return false;

            item = new SymbolLibraryItem(file, definition);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ShowSelectedSymbol()
    {
        if (_symbolListView.SelectedItems.Count == 0 || _symbolListView.SelectedItems[0].Tag is not SymbolLibraryItem item)
            return;

        var definition = item.Definition;
        _preview.SetFrame(definition.GetEffectiveFrameShape(), definition.FrameStatus);
        _preview.PhysicalDomain = definition.GetEffectivePhysicalDomain();
        _preview.SymbolRole = definition.SymbolRole;
        _preview.CompositionMode = definition.CompositionMode;
        _preview.SymbolLayout = definition.Layout ?? OrbatEquipmentSymbolLayout.CreateDefault();
        _preview.ComponentOnly = IsStandaloneComponent(definition);
        _preview.FillUpperFrameCap = definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
            && Enum.TryParse(definition.EquipmentFunction, out OrbatEquipmentFunction equipmentFunction)
            && equipmentFunction == OrbatEquipmentFunction.CommunicationsSatellite
            && definition.GetEffectiveOperatingState() == OrbatEquipmentOperatingState.InFlight;
        _preview.SetCommands(definition.Commands);
        _nameLabel.Text = item.DisplayName;
        if (definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment)
        {
            _symbolKindLabel.Text = definition.SymbolRole switch
            {
                OrbatEquipmentSymbolRole.Modifier1 => "Modifier 1",
                OrbatEquipmentSymbolRole.Modifier2 => "Modifier 2",
                OrbatEquipmentSymbolRole.MobilityIndicator => "Amplifier R (Mobility)",
                OrbatEquipmentSymbolRole.Composite => "Equipment composite",
                _ => "Equipment main"
            };
            _unitTypeLabel.Text = item.PrimarySortText;
        }
        else
        {
            _symbolKindLabel.Text = definition.SymbolRole switch
            {
                OrbatEquipmentSymbolRole.Modifier1 => "LandUnit Modifier 1",
                OrbatEquipmentSymbolRole.Modifier2 => "LandUnit Modifier 2",
                OrbatEquipmentSymbolRole.EchelonIndicator => "Amplifier B (Echelon)",
                _ => "Main function"
            };
            _unitTypeLabel.Text = definition.SymbolRole switch
            {
                OrbatEquipmentSymbolRole.Modifier1 => definition.LandUnitModifier1Type,
                OrbatEquipmentSymbolRole.Modifier2 => definition.LandUnitModifier2Type,
                OrbatEquipmentSymbolRole.EchelonIndicator => definition.EchelonType,
                _ => OrbatUnitMainFunctionCatalog.GetDisplayName(definition.GetEffectiveUnitMainFunction())
            };
        }
        _frameLabel.Text = definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
            ? $"{definition.GetEffectiveAffiliation()} / Equipment / {definition.GetEffectiveOperatingState()}"
            : $"{definition.GetEffectiveAffiliation()} / LandUnit";
        _statusValueLabel.Text = definition.FrameStatus.ToString();
        _commandCountLabel.Text = definition.Commands.Count.ToString();
        _libraryIdLabel.Text = definition.HasStoredLibraryId
            ? definition.LibraryId
            : $"{definition.GetEffectiveLibraryId()} (derived)";
        _libraryVersionLabel.Text = Math.Max(1, definition.LibraryVersion).ToString();
        _fileTextBox.Text = item.FileName;

        _commandListBox.BeginUpdate();
        try
        {
            _commandListBox.Items.Clear();
            foreach (var command in definition.Commands)
                _commandListBox.Items.Add(command.GetSummary());
        }
        finally
        {
            _commandListBox.EndUpdate();
        }
    }

    private void ClearDetails()
    {
        _preview.SetCommands(Array.Empty<SymbolDrawCommand>());
        _preview.ComponentOnly = false;
        _nameLabel.Text = string.Empty;
        _unitTypeLabel.Text = string.Empty;
        _frameLabel.Text = string.Empty;
        _statusValueLabel.Text = string.Empty;
        _commandCountLabel.Text = string.Empty;
        _libraryIdLabel.Text = string.Empty;
        _libraryVersionLabel.Text = string.Empty;
        _fileTextBox.Text = string.Empty;
    }

    private static Bitmap RenderThumbnail(SymbolLibraryDefinition definition)
    {
        var bitmap = new Bitmap(180, 120);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.White);

        var contentBounds = new RectangleF(12, 8, 156, 104);
        if (IsStandaloneComponent(definition))
        {
            using var componentPen = new Pen(Color.Black, 2f);
            if (definition.SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator)
            {
                var mobilityFrame = SymbolFrameRenderer.GetMobilityFrame(contentBounds);
                var thumbnailBounds = RectangleF.Inflate(mobilityFrame, -6f, -4f);
                var commandFrame = SymbolFrameRenderer.FitMobilityThumbnailCommands(thumbnailBounds, definition.Commands);
                foreach (var command in definition.Commands)
                    command.Draw(graphics, commandFrame, componentPen, Brushes.Black);
            }
            else if (definition.SymbolRole == OrbatEquipmentSymbolRole.EchelonIndicator)
            {
                var echelonFrame = SymbolFrameRenderer.GetEchelonFrame(contentBounds);
                foreach (var command in definition.Commands)
                    command.Draw(graphics, echelonFrame, componentPen, Brushes.Black);
            }
            else
            {
                SymbolFrameRenderer.DrawComponentCommands(graphics, contentBounds, definition.Commands, componentPen, Brushes.Black, strokeScale: 1f);
            }

            return bitmap;
        }

        var frameShape = definition.GetEffectiveFrameShape();
        var frame = SymbolFrameRenderer.GetFittedFrame(contentBounds, frameShape, definition.Commands, IconGuideShape.FlatTopBottom);
        var interiorFrame = SymbolFrameRenderer.GetInteriorFrame(frame, frameShape, IconGuideShape.FlatTopBottom);
        var drawingFrame = definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
            ? SymbolFrameRenderer.GetEquipmentComponentFrame(
                interiorFrame,
                definition.CompositionMode == OrbatEquipmentCompositionMode.Composite ? OrbatEquipmentSymbolRole.Composite : definition.SymbolRole,
                definition.Layout,
                hasModifier1: false,
                hasModifier2: false)
            : interiorFrame;
        using var pen = new Pen(Color.Black, 2f);
        var fillUpperFrameCap = definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
    && Enum.TryParse(definition.EquipmentFunction, out OrbatEquipmentFunction equipmentFunction)
    && equipmentFunction == OrbatEquipmentFunction.CommunicationsSatellite
    && definition.GetEffectiveOperatingState() == OrbatEquipmentOperatingState.InFlight;
        SymbolFrameRenderer.DrawFrame(graphics, frame, frameShape, definition.FrameStatus, fillFrame: true, IconGuideShape.FlatTopBottom, strokeScale: 1f, fillUpperCap: fillUpperFrameCap);
        foreach (var command in definition.Commands)
            SymbolFrameRenderer.DrawCommand(graphics, frame, SymbolFrameRenderer.GetCommandFrame(drawingFrame, frameShape, command), frameShape, command, pen, Brushes.Black, IconGuideShape.FlatTopBottom, strokeScale: 1f);

        return bitmap;
    }

    private static bool IsStandaloneComponent(SymbolLibraryDefinition definition) =>
        definition.SymbolRole is OrbatEquipmentSymbolRole.Modifier1
            or OrbatEquipmentSymbolRole.Modifier2
            or OrbatEquipmentSymbolRole.EchelonIndicator
        || definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
            && definition.SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator;

    private static Label CreateValueLabel() =>
        new()
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record SymbolLibraryItem(string FileName, SymbolLibraryDefinition Definition)
    {
        public int DomainSortOrder =>
            Definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment ? 1 : 0;

        public string PrimarySortText =>
            Definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
                ? Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier1
                    ? Definition.Modifier1Type
                    : Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier2
                        ? Definition.Modifier2Type
                        : Definition.SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator
                            ? Definition.MobilityType
                            : !string.IsNullOrWhiteSpace(Definition.EquipmentFunction)
                            ? Definition.EquipmentFunction
                            : GetLibraryNameFromFileName()
                : Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier1
                    ? Definition.LandUnitModifier1Type
                    : Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier2
                        ? Definition.LandUnitModifier2Type
                        : Definition.SymbolRole == OrbatEquipmentSymbolRole.EchelonIndicator
                            ? Definition.EchelonType
                            : Definition.GetEffectiveUnitMainFunction() != OrbatUnitMainFunction.Unspecified
                            ? OrbatUnitMainFunctionCatalog.GetDisplayName(Definition.GetEffectiveUnitMainFunction())
                            : !string.IsNullOrWhiteSpace(Definition.UnitType)
                                ? Definition.UnitType
                                : GetLibraryNameFromFileName();

        public string SecondarySortText =>
            Definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
                ? Definition.Variant
                : string.Empty;

        public string DisplayName => GetDisplayName();

        private string GetDisplayName()
        {
            var domain = Definition.GetEffectivePhysicalDomain();
            if (domain == SymbolPhysicalDomain.LandUnit
                && Definition.SymbolRole == OrbatEquipmentSymbolRole.EchelonIndicator
                && Enum.TryParse(Definition.EchelonType, out OrbatEchelon echelon))
                return echelon.ToString();

            if (domain == SymbolPhysicalDomain.LandUnit
                && Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier1
                && Enum.TryParse(Definition.LandUnitModifier1Type, out OrbatLandUnitModifier1 landModifier1))
                return landModifier1.GetDisplayName();

            if (domain == SymbolPhysicalDomain.LandUnit
                && Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier2
                && Enum.TryParse(Definition.LandUnitModifier2Type, out OrbatLandUnitModifier2 landModifier2))
                return landModifier2.GetDisplayName();

             if (domain == SymbolPhysicalDomain.LandUnit
                && Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier1
                && !string.IsNullOrWhiteSpace(Definition.LandUnitModifier1Type)
                && !Definition.LandUnitModifier1Type.Equals(OrbatLandUnitModifier1.Unspecified.ToString(), StringComparison.OrdinalIgnoreCase))
                return Definition.LandUnitModifier1Type;

            if (domain == SymbolPhysicalDomain.LandUnit
                && Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier2
                && !string.IsNullOrWhiteSpace(Definition.LandUnitModifier2Type)
                && !Definition.LandUnitModifier2Type.Equals(OrbatLandUnitModifier2.Unspecified.ToString(), StringComparison.OrdinalIgnoreCase))
                return Definition.LandUnitModifier2Type;

            if (domain == SymbolPhysicalDomain.Equipment
                && Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier1
                && Enum.TryParse(Definition.Modifier1Type, out OrbatEquipmentModifier1 modifier1))
                return modifier1.GetDisplayName();

            if (domain == SymbolPhysicalDomain.Equipment
                && Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier2
                && Enum.TryParse(Definition.Modifier2Type, out OrbatEquipmentModifier2 modifier2))
                return modifier2.GetDisplayName();

            if (domain == SymbolPhysicalDomain.Equipment
                && Definition.SymbolRole == OrbatEquipmentSymbolRole.MobilityIndicator
                && Enum.TryParse(Definition.MobilityType, out OrbatEquipmentMobilityMode mobility))
                return mobility.GetDisplayName();

            return ShouldPreferFileName()
                ? GetLibraryNameFromFileName()
                : !string.IsNullOrWhiteSpace(Definition.Name)
                    ? Definition.Name
                    : Definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
                        ? GetDefaultEquipmentDisplayName()
                    : Definition.GetEffectiveUnitMainFunction() != OrbatUnitMainFunction.Unspecified
                        ? OrbatUnitMainFunctionCatalog.GetDisplayName(Definition.GetEffectiveUnitMainFunction())
                        : !string.IsNullOrWhiteSpace(Definition.UnitType)
                            ? Definition.UnitType
                            : GetLibraryNameFromFileName();
        }

        private string GetDefaultEquipmentDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(Definition.Variant))
                return Definition.Variant;

            return !string.IsNullOrWhiteSpace(Definition.EquipmentFunction)
                ? Definition.EquipmentFunction
                : GetLibraryNameFromFileName();
        }

        private bool ShouldPreferFileName()
        {
            var fileName = GetLibraryNameFromFileName();
            var defaultKindName = Definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
                ? Definition.EquipmentFunction
                : Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier1
                    ? Definition.LandUnitModifier1Type
                    : Definition.SymbolRole == OrbatEquipmentSymbolRole.Modifier2
                        ? Definition.LandUnitModifier2Type
                        : Definition.SymbolRole == OrbatEquipmentSymbolRole.EchelonIndicator
                            ? Definition.EchelonType
                            : Definition.GetEffectiveUnitMainFunction() != OrbatUnitMainFunction.Unspecified
                            ? Definition.GetEffectiveUnitMainFunction().ToString()
                            : Definition.UnitType;
            return !string.IsNullOrWhiteSpace(fileName)
                && !fileName.Equals(Definition.Name, StringComparison.OrdinalIgnoreCase)
                && Definition.Name.Equals(defaultKindName, StringComparison.OrdinalIgnoreCase);
        }

        private string GetLibraryNameFromFileName()
        {
            var name = SymbolLibraryFileNaming.GetLogicalName(FileName);

            if (!string.IsNullOrWhiteSpace(name))
                return name;

            return Definition.GetEffectivePhysicalDomain() == SymbolPhysicalDomain.Equipment
                ? Definition.EquipmentFunction
                : Definition.GetEffectiveUnitMainFunction() != OrbatUnitMainFunction.Unspecified
                    ? OrbatUnitMainFunctionCatalog.GetDisplayName(Definition.GetEffectiveUnitMainFunction())
                    : Definition.UnitType;
        }
    }

    private sealed class SymbolLibraryViewerSettings
    {
        public SymbolLibraryLoadMode Mode { get; set; } = SymbolLibraryLoadMode.Folder;
        public string Folder { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new();
    }

    private enum SymbolLibraryLoadMode
    {
        Folder,
        Files
    }
}
