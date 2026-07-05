using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private readonly Button _editInDesignerButton = new() { Text = "Edit in designer", AutoSize = true };
    private readonly Label _statusLabel = new() { Dock = DockStyle.Fill, ForeColor = SystemColors.GrayText, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ListView _symbolListView = new();
    private readonly ImageList _thumbnailImages = new();
    private readonly SymbolPreviewControl _preview = new();
    private readonly Label _nameLabel = CreateValueLabel();
    private readonly Label _unitTypeLabel = CreateValueLabel();
    private readonly Label _frameLabel = CreateValueLabel();
    private readonly Label _statusValueLabel = CreateValueLabel();
    private readonly Label _commandCountLabel = CreateValueLabel();
    private readonly TextBox _fileTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly ListBox _commandListBox = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();
    private string[] _loadedFiles = Array.Empty<string>();

    public SymbolLibraryViewerForm()
    {
        Text = "ORBAT Symbol Library";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(980, 680);
        Size = new Size(1120, 760);

        _thumbnailImages.ImageSize = new Size(180, 120);
        _thumbnailImages.ColorDepth = ColorDepth.Depth32Bit;

        _symbolListView.Dock = DockStyle.Fill;
        _symbolListView.View = View.LargeIcon;
        _symbolListView.MultiSelect = false;
        _symbolListView.HideSelection = false;
        _symbolListView.LargeImageList = _thumbnailImages;
        _symbolListView.SelectedIndexChanged += (_, _) => ShowSelectedSymbol();
        _symbolListView.DoubleClick += (_, _) => EditSelectedInDesigner();

        _preview.Dock = DockStyle.Fill;
        _preview.BackColor = Color.White;

        _browseFolderButton.Click += (_, _) => BrowseFolder();
        _openFilesButton.Click += (_, _) => OpenFiles();
        _reloadButton.Click += (_, _) => ReloadCurrentLibrary();
        _editInDesignerButton.Click += (_, _) => EditSelectedInDesigner();

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
            SplitterDistance = 620
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
            ColumnCount = 6
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));

        toolbar.Controls.Add(new Label { Text = "Library", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 7, 8, 0) }, 0, 0);
        toolbar.Controls.Add(_libraryPathTextBox, 1, 0);
        toolbar.Controls.Add(_browseFolderButton, 2, 0);
        toolbar.Controls.Add(_openFilesButton, 3, 0);
        toolbar.Controls.Add(_reloadButton, 4, 0);
        toolbar.Controls.Add(_editInDesignerButton, 5, 0);
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
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
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
            RowCount = 6,
            Padding = new Padding(0, 8, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddMetadataRow(panel, 0, "Name", _nameLabel);
        AddMetadataRow(panel, 1, "Unit type", _unitTypeLabel);
        AddMetadataRow(panel, 2, "Frame", _frameLabel);
        AddMetadataRow(panel, 3, "Status", _statusValueLabel);
        AddMetadataRow(panel, 4, "Commands", _commandCountLabel);
        AddMetadataRow(panel, 5, "File", _fileTextBox);
        return panel;
    }

    private static void AddMetadataRow(TableLayoutPanel panel, int row, string label, Control value)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 5 ? 56 : 30));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 8, 0) }, 0, row);
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

        using var form = new SymbolDesignerForm(item.FileName);
        form.ShowDialog(this);
        ReloadCurrentLibrary();
    }

    private void LoadRecentLibrary()
    {
        var settings = LoadSettings();
        if (settings == null)
        {
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

        _statusLabel.Text = "Recent library was not found. Choose a folder or open symbol files.";
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
        _symbolListView.BeginUpdate();
        try
        {
            _symbolListView.Items.Clear();
            _thumbnailImages.Images.Clear();
            _commandListBox.Items.Clear();
            ClearDetails();

            var skipped = 0;
            foreach (var file in files)
            {
                if (!TryLoadItem(file, out var item))
                {
                    skipped++;
                    continue;
                }

                var imageKey = file;
                _thumbnailImages.Images.Add(imageKey, RenderThumbnail(item.Definition));
                var listItem = new ListViewItem(item.DisplayName, imageKey)
                {
                    Tag = item,
                    ToolTipText = file
                };
                listItem.SubItems.Add(item.Definition.UnitType);
                _symbolListView.Items.Add(listItem);
            }

            if (_symbolListView.Items.Count > 0)
                _symbolListView.Items[0].Selected = true;

            var loaded = _symbolListView.Items.Count;
            _statusLabel.Text = skipped == 0
                ? $"Loaded {loaded} symbol(s)."
                : $"Loaded {loaded} symbol(s). Skipped {skipped} invalid file(s).";
        }
        finally
        {
            _symbolListView.EndUpdate();
        }
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
        _preview.SetFrame(definition.FrameShape, definition.FrameStatus);
        _preview.SetCommands(definition.Commands);
        _nameLabel.Text = item.DisplayName;
        _unitTypeLabel.Text = definition.UnitType;
        _frameLabel.Text = definition.FrameShape.ToString();
        _statusValueLabel.Text = definition.FrameStatus.ToString();
        _commandCountLabel.Text = definition.Commands.Count.ToString();
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
        _nameLabel.Text = string.Empty;
        _unitTypeLabel.Text = string.Empty;
        _frameLabel.Text = string.Empty;
        _statusValueLabel.Text = string.Empty;
        _commandCountLabel.Text = string.Empty;
        _fileTextBox.Text = string.Empty;
    }

    private static Bitmap RenderThumbnail(SymbolLibraryDefinition definition)
    {
        var bitmap = new Bitmap(180, 120);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.White);

        var contentBounds = new RectangleF(12, 8, 156, 104);
        var frame = SymbolFrameRenderer.GetFittedFrame(contentBounds, definition.FrameShape, definition.Commands, IconGuideShape.FlatTopBottom);
        using var pen = new Pen(Color.Black, 2f);
        SymbolFrameRenderer.DrawFrame(graphics, frame, definition.FrameShape, definition.FrameStatus, fillFrame: true, IconGuideShape.FlatTopBottom);
        foreach (var command in definition.Commands)
            command.Draw(graphics, frame, pen, Brushes.Black);

        return bitmap;
    }

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
        public string DisplayName =>
            ShouldPreferFileName()
                ? GetLibraryNameFromFileName()
                : !string.IsNullOrWhiteSpace(Definition.Name)
                    ? Definition.Name
                    : !string.IsNullOrWhiteSpace(Definition.UnitType)
                        ? Definition.UnitType
                        : GetLibraryNameFromFileName();

        private bool ShouldPreferFileName()
        {
            var fileName = GetLibraryNameFromFileName();
            return !string.IsNullOrWhiteSpace(fileName)
                && !fileName.Equals(Definition.Name, StringComparison.OrdinalIgnoreCase)
                && Definition.Name.Equals(Definition.UnitType, StringComparison.OrdinalIgnoreCase);
        }

        private string GetLibraryNameFromFileName()
        {
            var name = Path.GetFileName(FileName);
            if (name.EndsWith(".orbatsymbol.json", StringComparison.OrdinalIgnoreCase))
                name = name[..^".orbatsymbol.json".Length];
            else
                name = Path.GetFileNameWithoutExtension(name);

            return string.IsNullOrWhiteSpace(name) ? Definition.UnitType : name;
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
