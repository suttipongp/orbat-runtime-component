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
    public static string GetSymbolText(this OrbatLandUnitModifier2 modifier) => modifier switch
    {
        OrbatLandUnitModifier2.Heavy => "H",
        OrbatLandUnitModifier2.Medium => "M",
        OrbatLandUnitModifier2.Light => "L",
        _ => string.Empty
    };

    public static string GetDisplayName(this OrbatLandUnitModifier2 modifier) => modifier switch
    {
        OrbatLandUnitModifier2.Unspecified => "Unspecified",
        OrbatLandUnitModifier2.ArmoredTracked => "Armored tracked",
        OrbatLandUnitModifier2.Amphibious => "Amphibious",
        OrbatLandUnitModifier2.Launcher => "Launcher",
        OrbatLandUnitModifier2.PackAnimal => "Pack animal",
        OrbatLandUnitModifier2.Rail => "Rail",
        OrbatLandUnitModifier2.TractorTrailer => "Tractor trailer",
        OrbatLandUnitModifier2.WheeledHighMobility => "Wheeled high mobility (cross country)",
        OrbatLandUnitModifier2.WheeledStandardMobility => "Wheeled standard mobility",
        _ => $"{modifier} ({modifier.GetSymbolText()})"
    };
}
