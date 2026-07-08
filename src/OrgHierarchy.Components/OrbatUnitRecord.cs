using System.ComponentModel;

namespace OrgHierarchy.Components;

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class OrbatUnitRecord
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? UniqueDesignation { get; set; }
    public OrbatAffiliation Affiliation { get; set; } = OrbatAffiliation.Friend;
    public OrbatSymbolDomain SymbolDomain { get; set; } = OrbatSymbolDomain.LandUnit;
    public OrbatEchelon Echelon { get; set; } = OrbatEchelon.Battalion;
    public OrbatUnitType UnitType { get; set; } = OrbatUnitType.Infantry;
    public string? Sidc { get; set; }
    public string? SymbolText { get; set; }
    public bool Headquarters { get; set; }
    public bool TaskForce { get; set; }
    public bool PlannedAnticipated { get; set; }
    public int StackCount { get; set; } = 1;
    public OrbatReinforcedReduced ReinforcedReduced { get; set; } = OrbatReinforcedReduced.NotApplicable;
    public IDictionary<string, string> Amplifiers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public bool Reinforced { get; set; }
    public bool Reduced { get; set; }
    public int SortOrder { get; set; }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(ShortName) ? Name : ShortName!;
    }
}
