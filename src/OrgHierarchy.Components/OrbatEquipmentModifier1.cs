namespace OrgHierarchy.Components;

public enum OrbatEquipmentModifier1
{
    Unspecified,
    ArmoredProtection,
    EchelonOfSupport,
    Cargo,
    MedicalEvacuation,
    PetroleumOilsAndLubricants,
    RecoveryAndMaintenance,
    RoboticGuidedAndAutonomous,
    Water,
    Attack,
    CommandAndControl,
    Heavy,
    Light,
    Medium,
    Multifunctional,
    Utility,
    EchelonCompany,
    EchelonBattalion,
    EchelonRegiment,
    EchelonBrigade,
    EchelonDivision
}

public static class OrbatEquipmentModifier1Info
{
    public static string GetSymbolText(this OrbatEquipmentModifier1 modifier) => modifier switch
    {
        OrbatEquipmentModifier1.Attack => "A",
        OrbatEquipmentModifier1.CommandAndControl => "C2",
        OrbatEquipmentModifier1.Heavy => "H",
        OrbatEquipmentModifier1.Light => "L",
        OrbatEquipmentModifier1.Medium => "M",
        OrbatEquipmentModifier1.Multifunctional => "MF",
        OrbatEquipmentModifier1.Utility => "U",
        OrbatEquipmentModifier1.EchelonCompany => "I",
        OrbatEquipmentModifier1.EchelonBattalion => "II",
        OrbatEquipmentModifier1.EchelonRegiment => "III",
        OrbatEquipmentModifier1.EchelonBrigade => "X",
        OrbatEquipmentModifier1.EchelonDivision => "XX",
        _ => string.Empty
    };

    public static string GetDisplayName(this OrbatEquipmentModifier1 modifier) => modifier switch
    {
        OrbatEquipmentModifier1.Unspecified => "Unspecified",
        OrbatEquipmentModifier1.ArmoredProtection => "Armored protection",
        OrbatEquipmentModifier1.EchelonOfSupport => "Echelon of support (legacy II)",
        OrbatEquipmentModifier1.Cargo => "Cargo",
        OrbatEquipmentModifier1.MedicalEvacuation => "Medical evacuation",
        OrbatEquipmentModifier1.PetroleumOilsAndLubricants => "Petroleum, oils and lubricants (POL)",
        OrbatEquipmentModifier1.RecoveryAndMaintenance => "Recovery and maintenance",
        OrbatEquipmentModifier1.RoboticGuidedAndAutonomous => "Robotic, guided and autonomous",
        OrbatEquipmentModifier1.Water => "Water",
        OrbatEquipmentModifier1.EchelonCompany => "Echelon of support: Company (I)",
        OrbatEquipmentModifier1.EchelonBattalion => "Echelon of support: Battalion (II)",
        OrbatEquipmentModifier1.EchelonRegiment => "Echelon of support: Regiment (III)",
        OrbatEquipmentModifier1.EchelonBrigade => "Echelon of support: Brigade (X)",
        OrbatEquipmentModifier1.EchelonDivision => "Echelon of support: Division (XX)",
        _ => $"{modifier} ({modifier.GetSymbolText()})"
    };
}
