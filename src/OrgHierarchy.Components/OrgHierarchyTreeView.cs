using System.ComponentModel;
using System.Data;
using System.Drawing.Drawing2D;

namespace OrgHierarchy.Components;

[DefaultEvent(nameof(NodeActivated))]
[DefaultProperty(nameof(DisplayColumn))]
[ToolboxItem(true)]
[Description("Displays organization or personnel hierarchy data from a DataTable or custom records.")]
public sealed class OrgHierarchyTreeView : UserControl
{
    private readonly ToolStrip _toolStrip = new();
    private readonly ToolStripTextBox _searchTextBox = new();
    private readonly ToolStripButton _expandButton = new("Expand");
    private readonly ToolStripButton _collapseButton = new("Collapse");
    private readonly ToolStripButton _refreshButton = new("Refresh");
    private readonly TreeView _treeView = new();
    private readonly ImageList _images = new();

    private List<HierarchyNodeRecord> _records = new();
    private Func<CancellationToken, Task<DataTable>>? _dataLoader;

    public OrgHierarchyTreeView()
    {
        InitializeComponent();
    }

    public event EventHandler<HierarchyNodeEventArgs>? NodeActivated;
    public event EventHandler? RefreshRequested;

    [Category("Data")]
    [DefaultValue("Id")]
    public string IdColumn { get; set; } = "Id";

    [Category("Data")]
    [DefaultValue("ParentId")]
    public string ParentIdColumn { get; set; } = "ParentId";

    [Category("Data")]
    [DefaultValue("DisplayName")]
    public string DisplayColumn { get; set; } = "DisplayName";

    [Category("Data")]
    [DefaultValue("Subtitle")]
    public string SubtitleColumn { get; set; } = "Subtitle";

    [Category("Data")]
    [DefaultValue("Kind")]
    public string KindColumn { get; set; } = "Kind";

    [Category("Data")]
    [DefaultValue("SortOrder")]
    public string SortColumn { get; set; } = "SortOrder";

    [Browsable(false)]
    public TreeNode? SelectedHierarchyNode => _treeView.SelectedNode;

    public void SetDataLoader(Func<CancellationToken, Task<DataTable>> dataLoader)
    {
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (_dataLoader == null)
            return;

        UseWaitCursor = true;
        try
        {
            var table = await _dataLoader(cancellationToken).ConfigureAwait(true);
            LoadFromDataTable(table);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    public void LoadFromDataTable(DataTable table)
    {
        var records = DataTableHierarchyMapper.FromDataTable(
            table,
            IdColumn,
            ParentIdColumn,
            DisplayColumn,
            SubtitleColumn,
            KindColumn,
            SortColumn);

        LoadFromRecords(records);
    }

    public void LoadFromRecords(IEnumerable<HierarchyNodeRecord> records)
    {
        if (records == null)
            throw new ArgumentNullException(nameof(records));

        _records = records.ToList();
        BuildTree(_records);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _images.ColorDepth = ColorDepth.Depth32Bit;
        _images.ImageSize = new Size(16, 16);
        _images.Images.Add(HierarchyNodeKind.Unknown.ToString(), CreateNodeIcon(Color.Gray));
        _images.Images.Add(HierarchyNodeKind.Organization.ToString(), CreateNodeIcon(Color.FromArgb(44, 123, 229)));
        _images.Images.Add(HierarchyNodeKind.Department.ToString(), CreateNodeIcon(Color.FromArgb(38, 166, 154)));
        _images.Images.Add(HierarchyNodeKind.Position.ToString(), CreateNodeIcon(Color.FromArgb(251, 140, 0)));
        _images.Images.Add(HierarchyNodeKind.Person.ToString(), CreatePersonIcon());

        _searchTextBox.AutoSize = false;
        _searchTextBox.Width = 220;
        _searchTextBox.ToolTipText = "Search name or subtitle";
        _searchTextBox.TextChanged += (_, _) => ApplyFilter();

        _expandButton.Click += (_, _) => _treeView.ExpandAll();
        _collapseButton.Click += (_, _) => _treeView.CollapseAll();
        _refreshButton.Click += async (_, _) =>
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
            await ReloadAsync();
        };

        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.Items.Add(new ToolStripLabel("Search"));
        _toolStrip.Items.Add(_searchTextBox);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(_expandButton);
        _toolStrip.Items.Add(_collapseButton);
        _toolStrip.Items.Add(_refreshButton);

        _treeView.Dock = DockStyle.Fill;
        _treeView.HideSelection = false;
        _treeView.ImageList = _images;
        _treeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
        _treeView.NodeMouseDoubleClick += (_, args) => RaiseNodeActivated(args.Node);
        _treeView.AfterSelect += (_, args) => RaiseNodeActivated(args.Node);
        _treeView.DrawNode += DrawNode;

        Controls.Add(_treeView);
        Controls.Add(_toolStrip);
        Dock = DockStyle.Fill;
        MinimumSize = new Size(320, 280);
        Name = nameof(OrgHierarchyTreeView);
        Size = new Size(420, 520);

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildTree(IEnumerable<HierarchyNodeRecord> records)
    {
        _treeView.BeginUpdate();
        _treeView.Nodes.Clear();

        var orderedRecords = records
            .OrderBy(record => record.SortOrder)
            .ThenBy(record => record.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var nodesById = orderedRecords.ToDictionary(
            record => record.Id,
            CreateTreeNode,
            StringComparer.OrdinalIgnoreCase);

        foreach (var record in orderedRecords)
        {
            var node = nodesById[record.Id];
            if (!string.IsNullOrWhiteSpace(record.ParentId) && nodesById.TryGetValue(record.ParentId, out var parent))
                parent.Nodes.Add(node);
            else
                _treeView.Nodes.Add(node);
        }

        _treeView.ExpandAll();
        _treeView.EndUpdate();
    }

    private TreeNode CreateTreeNode(HierarchyNodeRecord record)
    {
        var imageKey = record.Kind.ToString();
        return new TreeNode(record.DisplayName)
        {
            Tag = record,
            ToolTipText = record.Subtitle ?? record.DisplayName,
            ImageKey = imageKey,
            SelectedImageKey = imageKey
        };
    }

    private void ApplyFilter()
    {
        var searchText = _searchTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            BuildTree(_records);
            return;
        }

        var matches = _records
            .Where(record => Contains(record.DisplayName, searchText) || Contains(record.Subtitle, searchText))
            .Select(record => record.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var parentLookup = _records.ToDictionary(record => record.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var record in _records)
        {
            if (!matches.Contains(record.Id))
                continue;

            var parentId = record.ParentId;
            while (!string.IsNullOrWhiteSpace(parentId) && parentLookup.TryGetValue(parentId, out var parent))
            {
                matches.Add(parent.Id);
                parentId = parent.ParentId;
            }
        }

        BuildTree(_records.Where(record => matches.Contains(record.Id)));
    }

    private static bool Contains(string? source, string searchText)
    {
        return source?.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }

    private void RaiseNodeActivated(TreeNode? node)
    {
        if (node?.Tag is HierarchyNodeRecord record)
            NodeActivated?.Invoke(this, new HierarchyNodeEventArgs(record));
    }

    private static void DrawNode(object? sender, DrawTreeNodeEventArgs e)
    {
        if (e.Node == null)
            return;

        e.DrawDefault = true;
    }

    private static Bitmap CreateNodeIcon(Color color)
    {
        var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        graphics.FillRoundedRectangle(brush, new Rectangle(2, 2, 12, 12), 3);
        return bitmap;
    }

    private static Bitmap CreatePersonIcon()
    {
        var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Color.FromArgb(94, 53, 177));
        graphics.FillEllipse(brush, 5, 2, 6, 6);
        graphics.FillRoundedRectangle(brush, new Rectangle(3, 9, 10, 5), 4);
        return bitmap;
    }
}
