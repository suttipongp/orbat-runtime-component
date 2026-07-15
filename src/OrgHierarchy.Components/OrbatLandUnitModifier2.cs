namespace OrgHierarchy.Components;

public enum OrbatLandUnitModifier2
{
    Unspecified,
    ArmoredTracked,
    Amphibious,
    Launcher,
    PackAnimal,
    Rail,
    TractorTrailer,
    WheeledHighMobility,
    WheeledStandardMobility,
    Heavy,
    Medium,
    Light
}

public static class OrbatLandUnitModifier2Info
{
    public static string GetDisplayName(this OrbatLandUnitModifier2 modifier) =>
        Enum.TryParse<OrbatEquipmentModifier2>(modifier.ToString(), out var equipmentModifier)
            ? equipmentModifier.GetDisplayName()
            : modifier.ToString();
}