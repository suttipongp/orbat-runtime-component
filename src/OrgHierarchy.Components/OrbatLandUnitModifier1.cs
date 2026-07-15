namespace OrgHierarchy.Components;

public enum OrbatLandUnitModifier1
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

public static class OrbatLandUnitModifier1Info
{
    public static string GetSymbolText(this OrbatLandUnitModifier1 modifier) => modifier switch
    {
        OrbatLandUnitModifier1.Attack => "A",
        OrbatLandUnitModifier1.CommandAndControl => "C2",
        OrbatLandUnitModifier1.Heavy => "H",
        OrbatLandUnitModifier1.Light => "L",
        OrbatLandUnitModifier1.Medium => "M",
        OrbatLandUnitModifier1.Multifunctional => "MF",
        OrbatLandUnitModifier1.Utility => "U",
        OrbatLandUnitModifier1.EchelonCompany => "I",
        OrbatLandUnitModifier1.EchelonBattalion => "II",
        OrbatLandUnitModifier1.EchelonRegiment => "III",
        OrbatLandUnitModifier1.EchelonBrigade => "X",
        OrbatLandUnitModifier1.EchelonDivision => "XX",
        _ => string.Empty
    };

    public static string GetDisplayName(this OrbatLandUnitModifier1 modifier) => modifier switch
    {
        OrbatLandUnitModifier1.Unspecified => "Unspecified",
        OrbatLandUnitModifier1.ArmoredProtection => "Armored protection",
        OrbatLandUnitModifier1.EchelonOfSupport => "Echelon of support (legacy II)",
        OrbatLandUnitModifier1.Cargo => "Cargo",
        OrbatLandUnitModifier1.MedicalEvacuation => "Medical evacuation",
        OrbatLandUnitModifier1.PetroleumOilsAndLubricants => "Petroleum, oils and lubricants (POL)",
        OrbatLandUnitModifier1.RecoveryAndMaintenance => "Recovery and maintenance",
        OrbatLandUnitModifier1.RoboticGuidedAndAutonomous => "Robotic, guided and autonomous",
        OrbatLandUnitModifier1.Water => "Water",
        OrbatLandUnitModifier1.EchelonCompany => "Echelon of support: Company (I)",
        OrbatLandUnitModifier1.EchelonBattalion => "Echelon of support: Battalion (II)",
        OrbatLandUnitModifier1.EchelonRegiment => "Echelon of support: Regiment (III)",
        OrbatLandUnitModifier1.EchelonBrigade => "Echelon of support: Brigade (X)",
        OrbatLandUnitModifier1.EchelonDivision => "Echelon of support: Division (XX)",
        _ => $"{modifier} ({modifier.GetSymbolText()})"
    };
}
