namespace OrgHierarchy.Components;

public static class OrbatSidcParser
{
    public static OrbatSidcParseResult Parse(string? sidc)
    {
        var result = new OrbatSidcParseResult();
        var normalized = NormalizeSidc(sidc);
        result.Sidc = normalized;

        if (normalized.Length < 20)
            return result;

        result.IsValid = true;
        result.Affiliation = ParseAffiliation(normalized[3]);
        result.PlannedAnticipated = normalized[6] == '1';
        result.Headquarters = normalized[7] is '2' or '3' or '6' or '7';
        result.TaskForce = normalized[7] is '4' or '5' or '6' or '7';
        result.Echelon = ParseEchelon(normalized.Substring(8, 2));

        var symbolSet = normalized.Substring(4, 2);
        var entity = normalized.Substring(10, 6);
        result.UnitType = ParseUnitType(symbolSet, entity);

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

    private static OrbatUnitType? ParseUnitType(string symbolSet, string entity)
    {
        if (symbolSet != "10")
            return OrbatUnitType.Unspecified;

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
            "190100" => OrbatUnitType.Medical,
            "200100" => OrbatUnitType.Maintenance,
            "210100" => OrbatUnitType.Transportation,
            "220100" => OrbatUnitType.Intelligence,
            _ => OrbatUnitType.Unspecified
        };
    }
}
