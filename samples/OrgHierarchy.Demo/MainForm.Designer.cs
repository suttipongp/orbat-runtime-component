using OrgHierarchy.Components;

namespace OrgHierarchy.Demo;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private OrgHierarchyTreeView _hierarchyTreeView = null!;
    private OrganizationChartView _organizationChartView = null!;
    private OrbatChartView _orbatChartView = null!;
    private OrbatChartView _symbolGalleryChartView = null!;
    private CheckBox _showUniqueDesignationCheckBox = null!;
    private Button _addUnitButton = null!;
    private Button _editUnitButton = null!;
    private Button _deleteUnitButton = null!;
    private Button _showBranchButton = null!;
    private Button _showParentButton = null!;
    private Button _showAllButton = null!;
    private Button _copyFormatButton = null!;
    private Button _pasteFormatButton = null!;
    private Button _copyStructureButton = null!;
    private Button _pasteStructureButton = null!;
    private Button _exportOrbatButton = null!;
    private Button _importOrbatButton = null!;
    private Button _resetOrbatButton = null!;
    private Button _openSymbolDesignerButton = null!;
    private Button _viewSymbolLibraryButton = null!;
    private ContextMenuStrip _orbatContextMenu = null!;
    private Panel _orbatOptionsPanel = null!;
    private Panel _symbolsHeaderPanel = null!;
    private PropertyGrid _propertyGrid = null!;
    private SplitContainer _splitContainer = null!;
    private TabControl _viewTabs = null!;
    private TabPage _treeTab = null!;
    private TabPage _chartTab = null!;
    private TabPage _orbatTab = null!;
    private TabPage _symbolsTab = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _hierarchyTreeView = new OrgHierarchyTreeView();
        _organizationChartView = new OrganizationChartView();
        _orbatChartView = new OrbatChartView();
        _symbolGalleryChartView = new OrbatChartView();
        _showUniqueDesignationCheckBox = new CheckBox();
        _addUnitButton = new Button();
        _editUnitButton = new Button();
        _deleteUnitButton = new Button();
        _showBranchButton = new Button();
        _showParentButton = new Button();
        _showAllButton = new Button();
        _copyFormatButton = new Button();
        _pasteFormatButton = new Button();
        _copyStructureButton = new Button();
        _pasteStructureButton = new Button();
        _exportOrbatButton = new Button();
        _importOrbatButton = new Button();
        _resetOrbatButton = new Button();
        _openSymbolDesignerButton = new Button();
        _viewSymbolLibraryButton = new Button();
        _orbatContextMenu = new ContextMenuStrip(components);
        _orbatOptionsPanel = new Panel();
        _symbolsHeaderPanel = new Panel();
        _propertyGrid = new PropertyGrid();
        _splitContainer = new SplitContainer();
        _viewTabs = new TabControl();
        _treeTab = new TabPage();
        _chartTab = new TabPage();
        _orbatTab = new TabPage();
        _symbolsTab = new TabPage();
        ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
        _splitContainer.Panel1.SuspendLayout();
        _splitContainer.Panel2.SuspendLayout();
        _splitContainer.SuspendLayout();
        _viewTabs.SuspendLayout();
        _treeTab.SuspendLayout();
        _chartTab.SuspendLayout();
        _orbatTab.SuspendLayout();
        _symbolsTab.SuspendLayout();
        _orbatOptionsPanel.SuspendLayout();
        _symbolsHeaderPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _hierarchyTreeView
        // 
        _hierarchyTreeView.Dock = DockStyle.Fill;
        _hierarchyTreeView.Location = new Point(3, 3);
        _hierarchyTreeView.Name = "_hierarchyTreeView";
        _hierarchyTreeView.Size = new Size(766, 728);
        _hierarchyTreeView.TabIndex = 0;
        // 
        // _organizationChartView
        // 
        _organizationChartView.Dock = DockStyle.Fill;
        _organizationChartView.Location = new Point(3, 3);
        _organizationChartView.Name = "_organizationChartView";
        _organizationChartView.Size = new Size(766, 728);
        _organizationChartView.TabIndex = 0;
        // 
        // _orbatChartView
        // 
        _orbatChartView.Dock = DockStyle.Fill;
        _orbatChartView.Location = new Point(3, 113);
        _orbatChartView.Name = "_orbatChartView";
        _orbatChartView.Size = new Size(766, 618);
        _orbatChartView.TabIndex = 1;
        // 
        // _symbolGalleryChartView
        // 
        _symbolGalleryChartView.Dock = DockStyle.Fill;
        _symbolGalleryChartView.Location = new Point(3, 47);
        _symbolGalleryChartView.Name = "_symbolGalleryChartView";
        _symbolGalleryChartView.Size = new Size(766, 684);
        _symbolGalleryChartView.TabIndex = 1;
        // 
        // _showUniqueDesignationCheckBox
        // 
        _showUniqueDesignationCheckBox.AutoSize = true;
        _showUniqueDesignationCheckBox.Checked = true;
        _showUniqueDesignationCheckBox.CheckState = CheckState.Checked;
        _showUniqueDesignationCheckBox.Location = new Point(10, 11);
        _showUniqueDesignationCheckBox.Name = "_showUniqueDesignationCheckBox";
        _showUniqueDesignationCheckBox.Size = new Size(157, 19);
        _showUniqueDesignationCheckBox.TabIndex = 0;
        _showUniqueDesignationCheckBox.Text = "Show unique designation";
        _showUniqueDesignationCheckBox.UseVisualStyleBackColor = true;
        // 
        // _addUnitButton
        // 
        _addUnitButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _addUnitButton.Location = new Point(230, 6);
        _addUnitButton.Name = "_addUnitButton";
        _addUnitButton.Size = new Size(86, 28);
        _addUnitButton.TabIndex = 1;
        _addUnitButton.Text = "Add unit";
        _addUnitButton.UseVisualStyleBackColor = true;
        // 
        // _editUnitButton
        // 
        _editUnitButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _editUnitButton.Location = new Point(322, 6);
        _editUnitButton.Name = "_editUnitButton";
        _editUnitButton.Size = new Size(54, 28);
        _editUnitButton.TabIndex = 2;
        _editUnitButton.Text = "Edit";
        _editUnitButton.UseVisualStyleBackColor = true;
        // 
        // _deleteUnitButton
        // 
        _deleteUnitButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _deleteUnitButton.Location = new Point(382, 6);
        _deleteUnitButton.Name = "_deleteUnitButton";
        _deleteUnitButton.Size = new Size(54, 28);
        _deleteUnitButton.TabIndex = 3;
        _deleteUnitButton.Text = "Delete";
        _deleteUnitButton.UseVisualStyleBackColor = true;
        // 
        // _showBranchButton
        // 
        _showBranchButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _showBranchButton.Location = new Point(450, 6);
        _showBranchButton.Name = "_showBranchButton";
        _showBranchButton.Size = new Size(86, 28);
        _showBranchButton.TabIndex = 4;
        _showBranchButton.Text = "Show branch";
        _showBranchButton.UseVisualStyleBackColor = true;
        // 
        // _showParentButton
        // 
        _showParentButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _showParentButton.Location = new Point(560, 6);
        _showParentButton.Name = "_showParentButton";
        _showParentButton.Size = new Size(54, 28);
        _showParentButton.TabIndex = 5;
        _showParentButton.Text = "Up";
        _showParentButton.UseVisualStyleBackColor = true;
        // 
        // _showAllButton
        // 
        _showAllButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _showAllButton.Location = new Point(614, 6);
        _showAllButton.Name = "_showAllButton";
        _showAllButton.Size = new Size(86, 28);
        _showAllButton.TabIndex = 6;
        _showAllButton.Text = "Show all";
        _showAllButton.UseVisualStyleBackColor = true;
        // 
        // _copyFormatButton
        // 
        _copyFormatButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _copyFormatButton.Location = new Point(230, 40);
        _copyFormatButton.Name = "_copyFormatButton";
        _copyFormatButton.Size = new Size(86, 28);
        _copyFormatButton.TabIndex = 7;
        _copyFormatButton.Text = "Copy unit";
        _copyFormatButton.UseVisualStyleBackColor = true;
        // 
        // _pasteFormatButton
        // 
        _pasteFormatButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _pasteFormatButton.Location = new Point(322, 40);
        _pasteFormatButton.Name = "_pasteFormatButton";
        _pasteFormatButton.Size = new Size(86, 28);
        _pasteFormatButton.TabIndex = 8;
        _pasteFormatButton.Text = "Paste unit";
        _pasteFormatButton.UseVisualStyleBackColor = true;
        // 
        // _copyStructureButton
        // 
        _copyStructureButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _copyStructureButton.Location = new Point(428, 40);
        _copyStructureButton.Name = "_copyStructureButton";
        _copyStructureButton.Size = new Size(104, 28);
        _copyStructureButton.TabIndex = 9;
        _copyStructureButton.Text = "Copy structure";
        _copyStructureButton.UseVisualStyleBackColor = true;
        // 
        // _pasteStructureButton
        // 
        _pasteStructureButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _pasteStructureButton.Location = new Point(538, 40);
        _pasteStructureButton.Name = "_pasteStructureButton";
        _pasteStructureButton.Size = new Size(108, 28);
        _pasteStructureButton.TabIndex = 10;
        _pasteStructureButton.Text = "Paste structure";
        _pasteStructureButton.UseVisualStyleBackColor = true;
        // 
        // _exportOrbatButton
        // 
        _exportOrbatButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _exportOrbatButton.Location = new Point(230, 74);
        _exportOrbatButton.Name = "_exportOrbatButton";
        _exportOrbatButton.Size = new Size(76, 28);
        _exportOrbatButton.TabIndex = 11;
        _exportOrbatButton.Text = "Export";
        _exportOrbatButton.UseVisualStyleBackColor = true;
        // 
        // _importOrbatButton
        // 
        _importOrbatButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _importOrbatButton.Location = new Point(312, 74);
        _importOrbatButton.Name = "_importOrbatButton";
        _importOrbatButton.Size = new Size(76, 28);
        _importOrbatButton.TabIndex = 12;
        _importOrbatButton.Text = "Import";
        _importOrbatButton.UseVisualStyleBackColor = true;
        // 
        // _resetOrbatButton
        // 
        _resetOrbatButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _resetOrbatButton.Location = new Point(394, 74);
        _resetOrbatButton.Name = "_resetOrbatButton";
        _resetOrbatButton.Size = new Size(86, 28);
        _resetOrbatButton.TabIndex = 13;
        _resetOrbatButton.Text = "Reset data";
        _resetOrbatButton.UseVisualStyleBackColor = true;
        // 
        // _openSymbolDesignerButton
        // 
        _openSymbolDesignerButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _openSymbolDesignerButton.Location = new Point(10, 8);
        _openSymbolDesignerButton.Name = "_openSymbolDesignerButton";
        _openSymbolDesignerButton.Size = new Size(142, 28);
        _openSymbolDesignerButton.TabIndex = 0;
        _openSymbolDesignerButton.Text = "Open symbol designer";
        _openSymbolDesignerButton.UseVisualStyleBackColor = true;
        // 
        // _viewSymbolLibraryButton
        // 
        _viewSymbolLibraryButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _viewSymbolLibraryButton.Location = new Point(158, 8);
        _viewSymbolLibraryButton.Name = "_viewSymbolLibraryButton";
        _viewSymbolLibraryButton.Size = new Size(96, 28);
        _viewSymbolLibraryButton.TabIndex = 1;
        _viewSymbolLibraryButton.Text = "View library";
        _viewSymbolLibraryButton.UseVisualStyleBackColor = true;
        // 
        // _orbatOptionsPanel
        // 
        _orbatOptionsPanel.Controls.Add(_showUniqueDesignationCheckBox);
        _orbatOptionsPanel.Controls.Add(_addUnitButton);
        _orbatOptionsPanel.Controls.Add(_editUnitButton);
        _orbatOptionsPanel.Controls.Add(_deleteUnitButton);
        _orbatOptionsPanel.Controls.Add(_showBranchButton);
        _orbatOptionsPanel.Controls.Add(_showParentButton);
        _orbatOptionsPanel.Controls.Add(_showAllButton);
        _orbatOptionsPanel.Controls.Add(_copyFormatButton);
        _orbatOptionsPanel.Controls.Add(_pasteFormatButton);
        _orbatOptionsPanel.Controls.Add(_copyStructureButton);
        _orbatOptionsPanel.Controls.Add(_pasteStructureButton);
        _orbatOptionsPanel.Controls.Add(_exportOrbatButton);
        _orbatOptionsPanel.Controls.Add(_importOrbatButton);
        _orbatOptionsPanel.Controls.Add(_resetOrbatButton);
        _orbatOptionsPanel.Dock = DockStyle.Top;
        _orbatOptionsPanel.Location = new Point(3, 3);
        _orbatOptionsPanel.Name = "_orbatOptionsPanel";
        _orbatOptionsPanel.Padding = new Padding(10, 7, 10, 5);
        _orbatOptionsPanel.Size = new Size(766, 110);
        _orbatOptionsPanel.TabIndex = 0;
        // 
        // _symbolsHeaderPanel
        // 
        _symbolsHeaderPanel.Controls.Add(_viewSymbolLibraryButton);
        _symbolsHeaderPanel.Controls.Add(_openSymbolDesignerButton);
        _symbolsHeaderPanel.Dock = DockStyle.Top;
        _symbolsHeaderPanel.Location = new Point(3, 3);
        _symbolsHeaderPanel.Name = "_symbolsHeaderPanel";
        _symbolsHeaderPanel.Padding = new Padding(10, 7, 10, 5);
        _symbolsHeaderPanel.Size = new Size(766, 44);
        _symbolsHeaderPanel.TabIndex = 0;
        // 
        // _propertyGrid
        // 
        _propertyGrid.Dock = DockStyle.Fill;
        _propertyGrid.Location = new Point(0, 0);
        _propertyGrid.Name = "_propertyGrid";
        _propertyGrid.Size = new Size(384, 760);
        _propertyGrid.TabIndex = 0;
        // 
        // _splitContainer
        // 
        _splitContainer.Dock = DockStyle.Fill;
        _splitContainer.Location = new Point(0, 0);
        _splitContainer.Name = "_splitContainer";
        // 
        // _splitContainer.Panel1
        // 
        _splitContainer.Panel1.Controls.Add(_viewTabs);
        // 
        // _splitContainer.Panel2
        // 
        _splitContainer.Panel2.Controls.Add(_propertyGrid);
        _splitContainer.Size = new Size(1180, 760);
        _splitContainer.SplitterDistance = 780;
        _splitContainer.TabIndex = 0;
        // 
        // _viewTabs
        // 
        _viewTabs.Controls.Add(_treeTab);
        _viewTabs.Controls.Add(_chartTab);
        _viewTabs.Controls.Add(_orbatTab);
        _viewTabs.Controls.Add(_symbolsTab);
        _viewTabs.Dock = DockStyle.Fill;
        _viewTabs.Location = new Point(0, 0);
        _viewTabs.Name = "_viewTabs";
        _viewTabs.SelectedIndex = 0;
        _viewTabs.Size = new Size(792, 760);
        _viewTabs.TabIndex = 0;
        // 
        // _treeTab
        // 
        _treeTab.Controls.Add(_hierarchyTreeView);
        _treeTab.Location = new Point(4, 24);
        _treeTab.Name = "_treeTab";
        _treeTab.Padding = new Padding(3);
        _treeTab.Size = new Size(772, 734);
        _treeTab.TabIndex = 0;
        _treeTab.Text = "Tree";
        _treeTab.UseVisualStyleBackColor = true;
        // 
        // _chartTab
        // 
        _chartTab.Controls.Add(_organizationChartView);
        _chartTab.Location = new Point(4, 24);
        _chartTab.Name = "_chartTab";
        _chartTab.Padding = new Padding(3);
        _chartTab.Size = new Size(772, 734);
        _chartTab.TabIndex = 1;
        _chartTab.Text = "Chart";
        _chartTab.UseVisualStyleBackColor = true;
        // 
        // _orbatTab
        // 
        _orbatTab.Controls.Add(_orbatChartView);
        _orbatTab.Controls.Add(_orbatOptionsPanel);
        _orbatTab.Location = new Point(4, 24);
        _orbatTab.Name = "_orbatTab";
        _orbatTab.Padding = new Padding(3);
        _orbatTab.Size = new Size(772, 734);
        _orbatTab.TabIndex = 2;
        _orbatTab.Text = "ORBAT";
        _orbatTab.UseVisualStyleBackColor = true;
        // 
        // _symbolsTab
        // 
        _symbolsTab.Controls.Add(_symbolGalleryChartView);
        _symbolsTab.Controls.Add(_symbolsHeaderPanel);
        _symbolsTab.Location = new Point(4, 24);
        _symbolsTab.Name = "_symbolsTab";
        _symbolsTab.Padding = new Padding(3);
        _symbolsTab.Size = new Size(772, 734);
        _symbolsTab.TabIndex = 3;
        _symbolsTab.Text = "Symbols";
        _symbolsTab.UseVisualStyleBackColor = true;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1180, 760);
        Controls.Add(_splitContainer);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Organization Hierarchy Runtime Component";
        _splitContainer.Panel1.ResumeLayout(false);
        _splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
        _splitContainer.ResumeLayout(false);
        _viewTabs.ResumeLayout(false);
        _treeTab.ResumeLayout(false);
        _chartTab.ResumeLayout(false);
        _orbatTab.ResumeLayout(false);
        _symbolsTab.ResumeLayout(false);
        _orbatOptionsPanel.ResumeLayout(false);
        _orbatOptionsPanel.PerformLayout();
        _symbolsHeaderPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

}
