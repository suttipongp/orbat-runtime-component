namespace OrgHierarchy.Components;

/// <summary>
/// Flat source record used by <see cref="OrgHierarchyTreeView"/> to build a hierarchy.
/// </summary>
public sealed class HierarchyNodeRecord
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public HierarchyNodeKind Kind { get; set; } = HierarchyNodeKind.Unknown;
    public int SortOrder { get; set; }
    public object? DataItem { get; set; }
}
