namespace OrgHierarchy.Components;

public enum OrbatUnitMainFunctionCategory
{
    All,
    CommandAndControl,
    Fires,
    Intelligence,
    MovementAndManeuver,
    Protection,
    Sustainment,
    SpecialOperations
}

public enum OrbatUnitMainFunction
{
    Unspecified,
    Headquarters,
    CyberspaceOperations,
    ElectromagneticWarfare,
    InformationOperationsForce,
    InterpreterOrTranslator,
    IsolatedPersonnel,
    Liaison,
    MultinationalOperations,
    PublicAffairs,
    Signal,
    SpaceForces,
    SpecialTroops,
    AirDefenseArtillery,
    AirAndMissileDefense,
    AirNavalGunfireLiaison,
    DirectedEnergy,
    FieldArtillery,
    Missile,
    MissileDefense,
    ShortRangeAirDefense,
    IntelligenceOperations,
    AntiArmor,
    ArmorTracked,
    ArmoredTrackedCavalry,
    RotaryWingAviation,
    RotaryWingAviationReconnaissance,
    FixedWingAviation,
    CavalryReconnaissance,
    CombinedArms,
    Infantry,
    MechanizedArmoredTrackedInfantry,
    Mortar,
    Surveillance,
    BureauAlcoholTobaccoFirearmsExplosives,
    Cbrn,
    Cbrne,
    CbrnReconnaissance,
    DrugEnforcementAdministration,
    Engineer,
    FederalBureauInvestigation,
    FireDepartment,
    ManeuverEnhancement,
    MilitaryPolice,
    PoliceDepartment,
    Security,
    UnmannedAircraftSystem,
    AerialDelivery,
    ArmyFieldSupport,
    Ammunition,
    Band,
    ContractingSupport,
    ExplosiveOrdnanceDisposal,
    FieldFeeding,
    FinanceOperations,
    HumanResources,
    JudgeAdvocateGeneral,
    Maintenance,
    Medical,
    MedicalTreatmentFacility,
    MortuaryAffairs,
    Ordnance,
    Quartermaster,
    ReligiousSupport,
    ShowerAndLaundryOperations,
    Support,
    Sustainment,
    Transportation,
    CivilAffairs,
    CivilMilitaryCooperation,
    PsychologicalOperations,
    Rangers,
    SearchAndRescue,
    SealTeam,
    SpecialForces,
    SpecialOperationsForces,
    MultidomainOperations
}

public static class OrbatUnitMainFunctionCatalog
{
    private sealed class Entry
    {
        public Entry(
            OrbatUnitMainFunction function,
            OrbatUnitMainFunctionCategory category,
            string displayName)
        {
            Function = function;
            Category = category;
            DisplayName = displayName;
        }

        public OrbatUnitMainFunction Function { get; }
        public OrbatUnitMainFunctionCategory Category { get; }
        public string DisplayName { get; }
    }

    private static readonly Entry[] Entries =
    {
        E(OrbatUnitMainFunction.Unspecified, OrbatUnitMainFunctionCategory.CommandAndControl, "Unspecified"),
        E(OrbatUnitMainFunction.Headquarters, OrbatUnitMainFunctionCategory.CommandAndControl, "Headquarters"),
        E(OrbatUnitMainFunction.CyberspaceOperations, OrbatUnitMainFunctionCategory.CommandAndControl, "Cyberspace operations"),
        E(OrbatUnitMainFunction.ElectromagneticWarfare, OrbatUnitMainFunctionCategory.CommandAndControl, "Electromagnetic warfare"),
        E(OrbatUnitMainFunction.InformationOperationsForce, OrbatUnitMainFunctionCategory.CommandAndControl, "Information operations force"),
        E(OrbatUnitMainFunction.InterpreterOrTranslator, OrbatUnitMainFunctionCategory.CommandAndControl, "Interpreter or translator"),
        E(OrbatUnitMainFunction.IsolatedPersonnel, OrbatUnitMainFunctionCategory.CommandAndControl, "Isolated personnel"),
        E(OrbatUnitMainFunction.Liaison, OrbatUnitMainFunctionCategory.CommandAndControl, "Liaison"),
        E(OrbatUnitMainFunction.MultinationalOperations, OrbatUnitMainFunctionCategory.CommandAndControl, "Multinational operations"),
        E(OrbatUnitMainFunction.PublicAffairs, OrbatUnitMainFunctionCategory.CommandAndControl, "Public affairs"),
        E(OrbatUnitMainFunction.Signal, OrbatUnitMainFunctionCategory.CommandAndControl, "Signal"),
        E(OrbatUnitMainFunction.SpaceForces, OrbatUnitMainFunctionCategory.CommandAndControl, "Space forces"),
        E(OrbatUnitMainFunction.SpecialTroops, OrbatUnitMainFunctionCategory.CommandAndControl, "Special troops"),

        E(OrbatUnitMainFunction.AirDefenseArtillery, OrbatUnitMainFunctionCategory.Fires, "Air defense artillery"),
        E(OrbatUnitMainFunction.AirAndMissileDefense, OrbatUnitMainFunctionCategory.Fires, "Air and missile defense"),
        E(OrbatUnitMainFunction.AirNavalGunfireLiaison, OrbatUnitMainFunctionCategory.Fires, "Air-naval gunfire liaison"),
        E(OrbatUnitMainFunction.DirectedEnergy, OrbatUnitMainFunctionCategory.Fires, "Directed energy"),
        E(OrbatUnitMainFunction.FieldArtillery, OrbatUnitMainFunctionCategory.Fires, "Field artillery"),
        E(OrbatUnitMainFunction.Missile, OrbatUnitMainFunctionCategory.Fires, "Missile"),
        E(OrbatUnitMainFunction.MissileDefense, OrbatUnitMainFunctionCategory.Fires, "Missile defense"),
        E(OrbatUnitMainFunction.ShortRangeAirDefense, OrbatUnitMainFunctionCategory.Fires, "Short-range air defense"),

        E(OrbatUnitMainFunction.IntelligenceOperations, OrbatUnitMainFunctionCategory.Intelligence, "Intelligence operations"),

        E(OrbatUnitMainFunction.AntiArmor, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Anti-armor (anti-tank)"),
        E(OrbatUnitMainFunction.ArmorTracked, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Armor (tracked)"),
        E(OrbatUnitMainFunction.ArmoredTrackedCavalry, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Armored (tracked) cavalry"),
        E(OrbatUnitMainFunction.RotaryWingAviation, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Rotary-wing aviation"),
        E(OrbatUnitMainFunction.RotaryWingAviationReconnaissance, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Aviation (rotary-wing) reconnaissance"),
        E(OrbatUnitMainFunction.FixedWingAviation, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Aviation fixed wing"),
        E(OrbatUnitMainFunction.CavalryReconnaissance, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Cavalry (reconnaissance)"),
        E(OrbatUnitMainFunction.CombinedArms, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Combined arms"),
        E(OrbatUnitMainFunction.Infantry, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Infantry"),
        E(OrbatUnitMainFunction.MechanizedArmoredTrackedInfantry, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Mechanized armored (tracked) infantry"),
        E(OrbatUnitMainFunction.Mortar, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Mortar"),
        E(OrbatUnitMainFunction.Surveillance, OrbatUnitMainFunctionCategory.MovementAndManeuver, "Surveillance"),

        E(OrbatUnitMainFunction.BureauAlcoholTobaccoFirearmsExplosives, OrbatUnitMainFunctionCategory.Protection, "Bureau of Alcohol, Tobacco, Firearms and Explosives (ATF)"),
        E(OrbatUnitMainFunction.Cbrn, OrbatUnitMainFunctionCategory.Protection, "CBRN"),
        E(OrbatUnitMainFunction.Cbrne, OrbatUnitMainFunctionCategory.Protection, "CBRNE"),
        E(OrbatUnitMainFunction.CbrnReconnaissance, OrbatUnitMainFunctionCategory.Protection, "CBRN reconnaissance"),
        E(OrbatUnitMainFunction.DrugEnforcementAdministration, OrbatUnitMainFunctionCategory.Protection, "Drug Enforcement Administration (DEA)"),
        E(OrbatUnitMainFunction.Engineer, OrbatUnitMainFunctionCategory.Protection, "Engineer"),
        E(OrbatUnitMainFunction.FederalBureauInvestigation, OrbatUnitMainFunctionCategory.Protection, "Federal Bureau of Investigation (FBI)"),
        E(OrbatUnitMainFunction.FireDepartment, OrbatUnitMainFunctionCategory.Protection, "Fire department"),
        E(OrbatUnitMainFunction.ManeuverEnhancement, OrbatUnitMainFunctionCategory.Protection, "Maneuver enhancement"),
        E(OrbatUnitMainFunction.MilitaryPolice, OrbatUnitMainFunctionCategory.Protection, "Military police"),
        E(OrbatUnitMainFunction.PoliceDepartment, OrbatUnitMainFunctionCategory.Protection, "Police department"),
        E(OrbatUnitMainFunction.Security, OrbatUnitMainFunctionCategory.Protection, "Security"),
        E(OrbatUnitMainFunction.UnmannedAircraftSystem, OrbatUnitMainFunctionCategory.Protection, "Unmanned aircraft system"),

        E(OrbatUnitMainFunction.AerialDelivery, OrbatUnitMainFunctionCategory.Sustainment, "Aerial delivery"),
        E(OrbatUnitMainFunction.ArmyFieldSupport, OrbatUnitMainFunctionCategory.Sustainment, "Army field support"),
        E(OrbatUnitMainFunction.Ammunition, OrbatUnitMainFunctionCategory.Sustainment, "Ammunition"),
        E(OrbatUnitMainFunction.Band, OrbatUnitMainFunctionCategory.Sustainment, "Band"),
        E(OrbatUnitMainFunction.ContractingSupport, OrbatUnitMainFunctionCategory.Sustainment, "Contracting support"),
        E(OrbatUnitMainFunction.ExplosiveOrdnanceDisposal, OrbatUnitMainFunctionCategory.Sustainment, "Explosive ordnance disposal"),
        E(OrbatUnitMainFunction.FieldFeeding, OrbatUnitMainFunctionCategory.Sustainment, "Field feeding"),
        E(OrbatUnitMainFunction.FinanceOperations, OrbatUnitMainFunctionCategory.Sustainment, "Finance operations"),
        E(OrbatUnitMainFunction.HumanResources, OrbatUnitMainFunctionCategory.Sustainment, "Human resources"),
        E(OrbatUnitMainFunction.JudgeAdvocateGeneral, OrbatUnitMainFunctionCategory.Sustainment, "Judge advocate general"),
        E(OrbatUnitMainFunction.Maintenance, OrbatUnitMainFunctionCategory.Sustainment, "Maintenance"),
        E(OrbatUnitMainFunction.Medical, OrbatUnitMainFunctionCategory.Sustainment, "Medical"),
        E(OrbatUnitMainFunction.MedicalTreatmentFacility, OrbatUnitMainFunctionCategory.Sustainment, "Medical treatment facility"),
        E(OrbatUnitMainFunction.MortuaryAffairs, OrbatUnitMainFunctionCategory.Sustainment, "Mortuary affairs"),
        E(OrbatUnitMainFunction.Ordnance, OrbatUnitMainFunctionCategory.Sustainment, "Ordnance"),
        E(OrbatUnitMainFunction.Quartermaster, OrbatUnitMainFunctionCategory.Sustainment, "Quartermaster"),
        E(OrbatUnitMainFunction.ReligiousSupport, OrbatUnitMainFunctionCategory.Sustainment, "Religious support"),
        E(OrbatUnitMainFunction.ShowerAndLaundryOperations, OrbatUnitMainFunctionCategory.Sustainment, "Shower and laundry operations"),
        E(OrbatUnitMainFunction.Support, OrbatUnitMainFunctionCategory.Sustainment, "Support"),
        E(OrbatUnitMainFunction.Sustainment, OrbatUnitMainFunctionCategory.Sustainment, "Sustainment"),
        E(OrbatUnitMainFunction.Transportation, OrbatUnitMainFunctionCategory.Sustainment, "Transportation"),

        E(OrbatUnitMainFunction.CivilAffairs, OrbatUnitMainFunctionCategory.SpecialOperations, "Civil affairs"),
        E(OrbatUnitMainFunction.CivilMilitaryCooperation, OrbatUnitMainFunctionCategory.SpecialOperations, "Civil-military cooperation"),
        E(OrbatUnitMainFunction.PsychologicalOperations, OrbatUnitMainFunctionCategory.SpecialOperations, "Psychological operations"),
        E(OrbatUnitMainFunction.Rangers, OrbatUnitMainFunctionCategory.SpecialOperations, "Rangers"),
        E(OrbatUnitMainFunction.SearchAndRescue, OrbatUnitMainFunctionCategory.SpecialOperations, "Search and rescue"),
        E(OrbatUnitMainFunction.SealTeam, OrbatUnitMainFunctionCategory.SpecialOperations, "SEAL team"),
        E(OrbatUnitMainFunction.SpecialForces, OrbatUnitMainFunctionCategory.SpecialOperations, "Special forces"),
        E(OrbatUnitMainFunction.SpecialOperationsForces, OrbatUnitMainFunctionCategory.SpecialOperations, "Special operations forces"),
        E(OrbatUnitMainFunction.MultidomainOperations, OrbatUnitMainFunctionCategory.SpecialOperations, "Multidomain operations")
    };

    public static OrbatUnitMainFunctionCategory GetCategory(OrbatUnitMainFunction function) =>
        Entries.FirstOrDefault(entry => entry.Function == function)?.Category
        ?? OrbatUnitMainFunctionCategory.CommandAndControl;

    public static IReadOnlyList<OrbatUnitMainFunction> GetFunctions(OrbatUnitMainFunctionCategory category) =>
        Entries
            .Where(entry => category == OrbatUnitMainFunctionCategory.All || entry.Category == category)
            .OrderBy(entry => entry.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(entry => entry.Function)
            .ToArray();

    public static string GetCategoryDisplayName(OrbatUnitMainFunctionCategory category) =>
        category switch
        {
            OrbatUnitMainFunctionCategory.CommandAndControl => "Command and Control",
            OrbatUnitMainFunctionCategory.MovementAndManeuver => "Movement and Maneuver",
            OrbatUnitMainFunctionCategory.SpecialOperations => "Special Operations",
            _ => category.ToString()
        };

    public static string GetDisplayName(OrbatUnitMainFunction function) =>
        Entries.FirstOrDefault(entry => entry.Function == function)?.DisplayName ?? function.ToString();

    public static bool TryParseDisplayName(string value, out OrbatUnitMainFunction function)
    {
        if (Enum.TryParse(value, true, out function))
            return true;

        var normalized = Normalize(value);
        var match = Entries.FirstOrDefault(entry =>
            Normalize(entry.Function.ToString()) == normalized || Normalize(entry.DisplayName) == normalized);
        function = match?.Function ?? OrbatUnitMainFunction.Unspecified;
        return match != null;
    }

    public static OrbatUnitMainFunction FromLegacyUnitType(OrbatUnitType unitType) =>
        unitType switch
        {
            OrbatUnitType.Headquarters => OrbatUnitMainFunction.Headquarters,
            OrbatUnitType.Infantry => OrbatUnitMainFunction.Infantry,
            OrbatUnitType.Armor => OrbatUnitMainFunction.ArmorTracked,
            OrbatUnitType.MechanizedInfantry => OrbatUnitMainFunction.MechanizedArmoredTrackedInfantry,
            OrbatUnitType.Artillery => OrbatUnitMainFunction.FieldArtillery,
            OrbatUnitType.AirDefense => OrbatUnitMainFunction.AirDefenseArtillery,
            OrbatUnitType.Aviation => OrbatUnitMainFunction.RotaryWingAviation,
            OrbatUnitType.Engineer => OrbatUnitMainFunction.Engineer,
            OrbatUnitType.Reconnaissance => OrbatUnitMainFunction.CavalryReconnaissance,
            OrbatUnitType.Signal => OrbatUnitMainFunction.Signal,
            OrbatUnitType.MilitaryPolice => OrbatUnitMainFunction.MilitaryPolice,
            OrbatUnitType.Medical => OrbatUnitMainFunction.Medical,
            OrbatUnitType.CBRN => OrbatUnitMainFunction.Cbrn,
            OrbatUnitType.Logistics => OrbatUnitMainFunction.Sustainment,
            OrbatUnitType.Ordnance => OrbatUnitMainFunction.Ordnance,
            OrbatUnitType.Quartermaster => OrbatUnitMainFunction.Quartermaster,
            OrbatUnitType.Maintenance => OrbatUnitMainFunction.Maintenance,
            OrbatUnitType.Transportation => OrbatUnitMainFunction.Transportation,
            OrbatUnitType.SpecialOperations => OrbatUnitMainFunction.SpecialOperationsForces,
            OrbatUnitType.Cyber => OrbatUnitMainFunction.CyberspaceOperations,
            OrbatUnitType.Intelligence => OrbatUnitMainFunction.IntelligenceOperations,
            OrbatUnitType.PsychologicalOperations => OrbatUnitMainFunction.PsychologicalOperations,
            OrbatUnitType.Air => OrbatUnitMainFunction.FixedWingAviation,
            _ => OrbatUnitMainFunction.Unspecified
        };

    public static OrbatUnitType ToLegacyUnitType(OrbatUnitMainFunction function) =>
        function switch
        {
            OrbatUnitMainFunction.Headquarters => OrbatUnitType.Headquarters,
            OrbatUnitMainFunction.Infantry => OrbatUnitType.Infantry,
            OrbatUnitMainFunction.ArmorTracked => OrbatUnitType.Armor,
            OrbatUnitMainFunction.MechanizedArmoredTrackedInfantry => OrbatUnitType.MechanizedInfantry,
            OrbatUnitMainFunction.FieldArtillery => OrbatUnitType.Artillery,
            OrbatUnitMainFunction.AirDefenseArtillery or
            OrbatUnitMainFunction.AirAndMissileDefense or
            OrbatUnitMainFunction.MissileDefense or
            OrbatUnitMainFunction.ShortRangeAirDefense => OrbatUnitType.AirDefense,
            OrbatUnitMainFunction.RotaryWingAviation or
            OrbatUnitMainFunction.RotaryWingAviationReconnaissance => OrbatUnitType.Aviation,
            OrbatUnitMainFunction.FixedWingAviation => OrbatUnitType.Air,
            OrbatUnitMainFunction.Engineer => OrbatUnitType.Engineer,
            OrbatUnitMainFunction.CavalryReconnaissance or
            OrbatUnitMainFunction.Surveillance => OrbatUnitType.Reconnaissance,
            OrbatUnitMainFunction.Signal => OrbatUnitType.Signal,
            OrbatUnitMainFunction.MilitaryPolice => OrbatUnitType.MilitaryPolice,
            OrbatUnitMainFunction.Medical or
            OrbatUnitMainFunction.MedicalTreatmentFacility => OrbatUnitType.Medical,
            OrbatUnitMainFunction.Cbrn or
            OrbatUnitMainFunction.Cbrne or
            OrbatUnitMainFunction.CbrnReconnaissance => OrbatUnitType.CBRN,
            OrbatUnitMainFunction.Ordnance or
            OrbatUnitMainFunction.ExplosiveOrdnanceDisposal or
            OrbatUnitMainFunction.Ammunition => OrbatUnitType.Ordnance,
            OrbatUnitMainFunction.Quartermaster => OrbatUnitType.Quartermaster,
            OrbatUnitMainFunction.Maintenance => OrbatUnitType.Maintenance,
            OrbatUnitMainFunction.Transportation => OrbatUnitType.Transportation,
            OrbatUnitMainFunction.Sustainment or
            OrbatUnitMainFunction.Support => OrbatUnitType.Logistics,
            OrbatUnitMainFunction.SpecialOperationsForces or
            OrbatUnitMainFunction.SpecialForces or
            OrbatUnitMainFunction.Rangers or
            OrbatUnitMainFunction.SealTeam => OrbatUnitType.SpecialOperations,
            OrbatUnitMainFunction.CyberspaceOperations => OrbatUnitType.Cyber,
            OrbatUnitMainFunction.IntelligenceOperations => OrbatUnitType.Intelligence,
            OrbatUnitMainFunction.PsychologicalOperations => OrbatUnitType.PsychologicalOperations,
            _ => OrbatUnitType.Unspecified
        };

    private static Entry E(
        OrbatUnitMainFunction function,
        OrbatUnitMainFunctionCategory category,
        string displayName) =>
        new(function, category, displayName);

    private static string Normalize(string value) =>
        new((value ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
}
