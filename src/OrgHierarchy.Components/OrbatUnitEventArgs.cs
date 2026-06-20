namespace OrgHierarchy.Components;

public sealed class OrbatUnitEventArgs : EventArgs
{
    public OrbatUnitEventArgs(OrbatUnitRecord unit)
    {
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
    }

    public OrbatUnitRecord Unit { get; }
}
