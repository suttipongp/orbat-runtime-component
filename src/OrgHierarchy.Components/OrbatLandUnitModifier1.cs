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
    public static string GetDisplayName(this OrbatLandUnitModifier1 modifier) =>
        Enum.TryParse<OrbatEquipmentModifier1>(modifier.ToString(), out var equipmentModifier)
            ? equipmentModifier.GetDisplayName()
            : modifier.ToString();
}