namespace OrgHierarchy.Components;

public sealed class OrbatSymbolInstance
{
    public const int CurrentSchemaVersion = 1;

    public int Version { get; set; } = CurrentSchemaVersion;
    public string InstanceId { get; set; } = Guid.NewGuid().ToString("N");
    public string LibraryId { get; set; } = string.Empty;
    public int LibraryVersion { get; set; }
    public string Modifier1LibraryId { get; set; } = string.Empty;
    public string Modifier2LibraryId { get; set; } = string.Empty;
    public string Modifier1 { get; set; } = string.Empty;
    public string Modifier2 { get; set; } = string.Empty;
    public string Mobility { get; set; } = string.Empty;
    public string MobilityLibraryId { get; set; } = string.Empty;
    public OrbatSymbolDomain Domain { get; set; } = OrbatSymbolDomain.LandUnit;
    public OrbatAffiliation Affiliation { get; set; } = OrbatAffiliation.Friend;
    public string FrameStatus { get; set; } = "Present";
    public string OperatingState { get; set; } = "Ground";
    public string Function { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public float X { get; set; } = 0.5f;
    public float Y { get; set; } = 0.5f;
    public float Width { get; set; } = 1f;
    public float Height { get; set; } = 1f;
    public float RotationDegrees { get; set; }
    public bool Visible { get; set; } = true;
    public Dictionary<string, string> Amplifiers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public void EnsureValidIdentity()
    {
        if (string.IsNullOrWhiteSpace(InstanceId))
            InstanceId = Guid.NewGuid().ToString("N");
        Amplifiers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
