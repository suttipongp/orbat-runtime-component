namespace OrgHierarchy.Components;

public enum OrbatEquipmentSymbolRole
{
    Composite,
    MainFunction,
    Modifier1,
    Modifier2,
    MobilityIndicator,
    EchelonIndicator
}

public enum OrbatEquipmentCompositionMode
{
    Composite,
    Composable
}

public sealed class OrbatEquipmentSymbolLayout
{
    public float MainScaleWithoutModifiers { get; set; } = 0.80f;
    public float MainScaleWithOneModifier { get; set; } = 0.74f;
    public float MainScaleWithTwoModifiers { get; set; } = 0.68f;
    // ModifierScale is retained for compatibility with version 2 library files.
    public float ModifierScale { get; set; } = 0.22f;
    public float ModifierWidthScale { get; set; } = 0.40f;
    public float ModifierHeightScale { get; set; } = 0.18f;
    public float ModifierCenterOffset { get; set; } = 0.36f;
    public float MainSingleModifierOffset { get; set; } = 0.07f;
    public float MainOffsetX { get; set; }
    public float MainOffsetY { get; set; }

    public static OrbatEquipmentSymbolLayout CreateDefault() => new();
}

public enum OrbatEquipmentMobilityMode
{
    Unspecified,
    Wheeled,
    WheeledCrossCountry,
    Tracked,
    WheeledTracked,
    Towed,
    Railway,
    OverSnow,
    Sled,
    PackAnimals,
    Barge,
    Amphibious
}

public static class OrbatEquipmentMobilityModeInfo
{
    public static string GetDisplayName(this OrbatEquipmentMobilityMode mode) => mode switch
    {
        OrbatEquipmentMobilityMode.Unspecified => "Unspecified",
        OrbatEquipmentMobilityMode.Wheeled => "Wheeled (limited to improved roads)",
        OrbatEquipmentMobilityMode.WheeledCrossCountry => "Wheeled (cross-country)",
        OrbatEquipmentMobilityMode.Tracked => "Tracked",
        OrbatEquipmentMobilityMode.WheeledTracked => "Wheeled and tracked combination",
        OrbatEquipmentMobilityMode.Towed => "Towed",
        OrbatEquipmentMobilityMode.Railway => "Railway",
        OrbatEquipmentMobilityMode.OverSnow => "Over-snow (prime mover)",
        OrbatEquipmentMobilityMode.Sled => "Sled",
        OrbatEquipmentMobilityMode.PackAnimals => "Pack animals",
        OrbatEquipmentMobilityMode.Barge => "Barge",
        OrbatEquipmentMobilityMode.Amphibious => "Amphibious",
        _ => mode.ToString()
    };
}