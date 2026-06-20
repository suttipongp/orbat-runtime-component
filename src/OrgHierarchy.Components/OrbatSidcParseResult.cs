namespace OrgHierarchy.Components;

public sealed class OrbatSidcParseResult
{
    public string Sidc { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string SymbolSet { get; set; } = string.Empty;
    public string EntityCode { get; set; } = string.Empty;
    public bool IsSupportedSymbolSet { get; set; }
    public bool HasKnownUnitType { get; set; }
    public OrbatAffiliation? Affiliation { get; set; }
    public OrbatEchelon? Echelon { get; set; }
    public OrbatUnitType? UnitType { get; set; }
    public bool? Headquarters { get; set; }
    public bool? TaskForce { get; set; }
    public bool? PlannedAnticipated { get; set; }
    public List<string> Warnings { get; } = new();

    public string GetSummary()
    {
        if (!IsValid)
            return Warnings.Count == 0 ? "SIDC must contain at least 20 digits." : string.Join(" ", Warnings);

        var unitType = UnitType?.ToString() ?? OrbatUnitType.Unspecified.ToString();
        var echelon = Echelon?.ToString() ?? OrbatEchelon.Unspecified.ToString();
        var affiliation = Affiliation?.ToString() ?? OrbatAffiliation.Unspecified.ToString();
        var flags = new List<string>();
        if (Headquarters == true)
            flags.Add("HQ");
        if (TaskForce == true)
            flags.Add("TF");
        if (PlannedAnticipated == true)
            flags.Add("Planned");

        var suffix = flags.Count == 0 ? string.Empty : $" ({string.Join(", ", flags)})";
        return $"{affiliation} / {echelon} / {unitType}{suffix}";
    }
}
