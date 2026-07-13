namespace OrgHierarchy.Components;

public enum OrbatEquipmentModifier2
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

public static class OrbatEquipmentModifier2Info
{
    public static string GetSymbolText(this OrbatEquipmentModifier2 modifier) => modifier switch
    {
        OrbatEquipmentModifier2.Heavy => "H",
        OrbatEquipmentModifier2.Medium => "M",
        OrbatEquipmentModifier2.Light => "L",
        _ => string.Empty
    };

    public static string GetDisplayName(this OrbatEquipmentModifier2 modifier) => modifier switch
    {
        OrbatEquipmentModifier2.Unspecified => "Unspecified",
        OrbatEquipmentModifier2.ArmoredTracked => "Armored tracked",
        OrbatEquipmentModifier2.Amphibious => "Amphibious",
        OrbatEquipmentModifier2.Launcher => "Launcher",
        OrbatEquipmentModifier2.PackAnimal => "Pack animal",
        OrbatEquipmentModifier2.Rail => "Rail",
        OrbatEquipmentModifier2.TractorTrailer => "Tractor trailer",
        OrbatEquipmentModifier2.WheeledHighMobility => "Wheeled high mobility (cross country)",
        OrbatEquipmentModifier2.WheeledStandardMobility => "Wheeled standard mobility",
        _ => $"{modifier} ({modifier.GetSymbolText()})"
    };
}
