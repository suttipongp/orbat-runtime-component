namespace OrgHierarchy.Components;

public sealed class OrbatSidcParseResult
{
    public string Sidc { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public OrbatAffiliation? Affiliation { get; set; }
    public OrbatEchelon? Echelon { get; set; }
    public OrbatUnitType? UnitType { get; set; }
    public bool? Headquarters { get; set; }
    public bool? TaskForce { get; set; }
    public bool? PlannedAnticipated { get; set; }
}
