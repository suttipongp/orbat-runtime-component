namespace OrgHierarchy.Components;

public sealed class HierarchyNodeEventArgs : EventArgs
{
    public HierarchyNodeEventArgs(HierarchyNodeRecord record)
    {
        Record = record;
    }

    public HierarchyNodeRecord Record { get; }
}
