namespace OrgHierarchy.Components;

public static class OrbatSidcParser
{
    private const string LandUnitSymbolSet = "10";

    public static string Compose(OrbatUnitRecord unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        return Compose(
            unit.Affiliation,
            unit.Echelon,
            unit.UnitType,
            unit.Headquarters,
            unit.TaskForce,
            unit.PlannedAnticipated);
    }

    public static string Compose(
        OrbatAffiliation affiliation,
        OrbatEchelon echelon,
        OrbatUnitType unitType,
        bool headquarters,
        bool taskForce,
        bool plannedAnticipated)
    {
        return string.Concat(
            "10",
            "0",
            ComposeAffiliation(affiliation),
            "10",
            plannedAnticipated ? "1" : "0",
            ComposeHeadquartersTaskForce(headquarters, taskForce),
            ComposeEchelon(echelon),
            ComposeEntity(unitType),
            "0000");
    }

    public static OrbatSidcParseResult Parse(string? sidc)
    {
        var result = new OrbatSidcParseResult();
        var normalized = NormalizeSidc(sidc);
        result.Sidc = normalized.Length > 20 ? normalized.Substring(0, 20) : normalized;

        if (normalized.Length < 20)
        {
            result.Warnings.Add("SIDC must contain at least 20 digits.");
            return result;
        }

        if (normalized.Length > 20)
            result.Warnings.Add("SIDC contains more than 20 digits; only the first 20 digits were used.");

        result.IsValid = true;
        result.Affiliation = ParseAffiliation(result.Sidc[3]);
        result.PlannedAnticipated = result.Sidc[6] == '1';
        result.Headquarters = result.Sidc[7] is '2' or '3' or '6' or '7';
        result.TaskForce = result.Sidc[7] is '4' or '5' or '6' or '7';
        result.Echelon = ParseEchelon(result.Sidc.Substring(8, 2));

        result.SymbolSet = result.Sidc.Substring(4, 2);
        result.EntityCode = result.Sidc.Substring(10, 6);
        result.IsSupportedSymbolSet = result.SymbolSet == LandUnitSymbolSet;
        result.UnitType = ParseUnitType(result.SymbolSet, result.EntityCode);
        result.HasKnownUnitType = result.UnitType.HasValue && result.UnitType.Value != OrbatUnitType.Unspecified;
        if (!result.IsSupportedSymbolSet)
            result.Warnings.Add($"Symbol set {result.SymbolSet} is not mapped as a land unit.");
        if (!result.HasKnownUnitType)
            result.Warnings.Add($"Entity {result.EntityCode} is not mapped to a known unit type.");

        return result;
    }

    public static bool TryParse(string? sidc, out OrbatSidcParseResult result)
    {
        result = Parse(sidc);
        return result.IsValid;
    }

    private static string NormalizeSidc(string? sidc)
    {
        if (string.IsNullOrWhiteSpace(sidc))
            return string.Empty;

        var chars = new char[sidc.Length];
        var index = 0;
        foreach (var c in sidc)
        {
            if (char.IsDigit(c))
                chars[index++] = c;
        }

        return new string(chars, 0, index);
    }

    private static OrbatAffiliation? ParseAffiliation(char standardIdentity)
    {
        return standardIdentity switch
        {
            '1' => OrbatAffiliation.Unknown,
            '2' => OrbatAffiliation.Friend,
            '3' => OrbatAffiliation.Friend,
            '4' => OrbatAffiliation.Neutral,
            '5' => OrbatAffiliation.Suspect,
            '6' => OrbatAffiliation.Hostile,
            _ => OrbatAffiliation.Unspecified
        };
    }

    private static string ComposeAffiliation(OrbatAffiliation affiliation)
    {
        return affiliation switch
        {
            OrbatAffiliation.Unknown => "1",
            OrbatAffiliation.Friend => "3",
            OrbatAffiliation.Neutral => "4",
            OrbatAffiliation.Suspect => "5",
            OrbatAffiliation.Hostile => "6",
            OrbatAffiliation.Civilian => "4",
            _ => "0"
        };
    }

    private static string ComposeHeadquartersTaskForce(bool headquarters, bool taskForce)
    {
        if (headquarters && taskForce)
            return "6";
        if (headquarters)
            return "2";
        return taskForce ? "4" : "0";
    }

    private static OrbatEchelon? ParseEchelon(string echelon)
    {
        return echelon switch
        {
            "11" => OrbatEchelon.Team,
            "12" => OrbatEchelon.Squad,
            "13" => OrbatEchelon.Section,
            "14" => OrbatEchelon.Platoon,
            "15" => OrbatEchelon.Company,
            "16" => OrbatEchelon.Battalion,
            "17" => OrbatEchelon.Regiment,
            "18" => OrbatEchelon.Brigade,
            "21" => OrbatEchelon.Division,
            "22" => OrbatEchelon.Corps,
            "23" => OrbatEchelon.Army,
            "24" => OrbatEchelon.ArmyGroup,
            "25" => OrbatEchelon.Region,
            "26" => OrbatEchelon.Command,
            _ => OrbatEchelon.Unspecified
        };
    }

    private static string ComposeEchelon(OrbatEchelon echelon)
    {
        return echelon switch
        {
            OrbatEchelon.Team => "11",
            OrbatEchelon.Squad => "12",
            OrbatEchelon.Section => "13",
            OrbatEchelon.Platoon => "14",
            OrbatEchelon.Company => "15",
            OrbatEchelon.Battalion => "16",
            OrbatEchelon.Regiment => "17",
            OrbatEchelon.Brigade => "18",
            OrbatEchelon.Division => "21",
            OrbatEchelon.Corps => "22",
            OrbatEchelon.Army => "23",
            OrbatEchelon.ArmyGroup => "24",
            OrbatEchelon.Region => "25",
            OrbatEchelon.Command => "26",
            _ => "00"
        };
    }

    private static OrbatUnitType? ParseUnitType(string symbolSet, string entity)
    {
        if (symbolSet != LandUnitSymbolSet)
            return OrbatUnitType.Unspecified;

        var exact = ParseExactLandUnitEntity(entity);
        if (exact != OrbatUnitType.Unspecified)
            return exact;

        return ParseLandUnitEntityPrefix(entity);
    }

    private static OrbatUnitType ParseExactLandUnitEntity(string entity)
    {
        return entity switch
        {
            "110000" => OrbatUnitType.Headquarters,
            "121100" => OrbatUnitType.Infantry,
            "121101" => OrbatUnitType.Infantry,
            "121102" => OrbatUnitType.MechanizedInfantry,
            "121200" => OrbatUnitType.Armor,
            "121300" => OrbatUnitType.Reconnaissance,
            "130100" => OrbatUnitType.Artillery,
            "130200" => OrbatUnitType.AirDefense,
            "140100" => OrbatUnitType.Aviation,
            "150100" => OrbatUnitType.Engineer,
            "160100" => OrbatUnitType.Signal,
            "170100" => OrbatUnitType.MilitaryPolice,
            "180100" => OrbatUnitType.Logistics,
            "180200" => OrbatUnitType.Ordnance,
            "180300" => OrbatUnitType.Quartermaster,
            "190100" => OrbatUnitType.Medical,
            "200100" => OrbatUnitType.Maintenance,
            "210100" => OrbatUnitType.Transportation,
            "220100" => OrbatUnitType.Intelligence,
            "220200" => OrbatUnitType.PsychologicalOperations,
            "230100" => OrbatUnitType.CBRN,
            _ => OrbatUnitType.Unspecified
        };
    }

    private static OrbatUnitType ParseLandUnitEntityPrefix(string entity)
    {
        if (entity.StartsWith("121102", StringComparison.Ordinal))
            return OrbatUnitType.MechanizedInfantry;
        if (entity.StartsWith("1211", StringComparison.Ordinal))
            return OrbatUnitType.Infantry;
        if (entity.StartsWith("1212", StringComparison.Ordinal))
            return OrbatUnitType.Armor;
        if (entity.StartsWith("1213", StringComparison.Ordinal))
            return OrbatUnitType.Reconnaissance;
        if (entity.StartsWith("13", StringComparison.Ordinal))
            return OrbatUnitType.Artillery;
        if (entity.StartsWith("14", StringComparison.Ordinal))
            return OrbatUnitType.Aviation;
        if (entity.StartsWith("15", StringComparison.Ordinal))
            return OrbatUnitType.Engineer;
        if (entity.StartsWith("16", StringComparison.Ordinal))
            return OrbatUnitType.Signal;
        if (entity.StartsWith("17", StringComparison.Ordinal))
            return OrbatUnitType.MilitaryPolice;
        if (entity.StartsWith("18", StringComparison.Ordinal))
            return OrbatUnitType.Logistics;
        if (entity.StartsWith("19", StringComparison.Ordinal))
            return OrbatUnitType.Medical;
        if (entity.StartsWith("20", StringComparison.Ordinal))
            return OrbatUnitType.Maintenance;
        if (entity.StartsWith("21", StringComparison.Ordinal))
            return OrbatUnitType.Transportation;
        if (entity.StartsWith("22", StringComparison.Ordinal))
            return OrbatUnitType.Intelligence;
        if (entity.StartsWith("23", StringComparison.Ordinal))
            return OrbatUnitType.CBRN;

        return OrbatUnitType.Unspecified;
    }

    private static string ComposeEntity(OrbatUnitType unitType)
    {
        return unitType switch
        {
            OrbatUnitType.Headquarters => "110000",
            OrbatUnitType.Infantry => "121100",
            OrbatUnitType.MechanizedInfantry => "121102",
            OrbatUnitType.Armor => "121200",
            OrbatUnitType.Reconnaissance => "121300",
            OrbatUnitType.Artillery => "130100",
            OrbatUnitType.AirDefense => "130200",
            OrbatUnitType.Aviation => "140100",
            OrbatUnitType.Engineer => "150100",
            OrbatUnitType.Signal => "160100",
            OrbatUnitType.MilitaryPolice => "170100",
            OrbatUnitType.Logistics => "180100",
            OrbatUnitType.Ordnance => "180200",
            OrbatUnitType.Quartermaster => "180300",
            OrbatUnitType.Medical => "190100",
            OrbatUnitType.Maintenance => "200100",
            OrbatUnitType.Transportation => "210100",
            OrbatUnitType.Intelligence => "220100",
            OrbatUnitType.PsychologicalOperations => "220200",
            OrbatUnitType.CBRN => "230100",
            OrbatUnitType.Naval => "000000",
            OrbatUnitType.Air => "000000",
            OrbatUnitType.Cyber => "000000",
            OrbatUnitType.SpecialOperations => "000000",
            _ => "000000"
        };
    }
}
