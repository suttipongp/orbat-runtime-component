namespace OrgHierarchy.Components;

public enum OrbatEquipmentSymbolRole
{
    Composite,
    MainFunction,
    Modifier1,
    Modifier2
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
