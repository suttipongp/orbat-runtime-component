namespace OrgHierarchy.Components;

public static class OrbatSymbolAmplifierLayouts
{
    private static readonly string[] EvaluationRatings =
    {
        "A1", "A2", "A3", "A4", "A5", "A6",
        "B1", "B2", "B3", "B4", "B5", "B6",
        "C1", "C2", "C3", "C4", "C5", "C6",
        "D1", "D2", "D3", "D4", "D5", "D6",
        "E1", "E2", "E3", "E4", "E5", "E6",
        "F1", "F2", "F3", "F4", "F5", "F6"
    };

    private static readonly string[] SignatureEquipment = { "!" };
    private static readonly string[] HostileSymbols = { "ENY" };
    private static readonly string[] MobilityModes =
    {
        "Wheeled",
        "WheeledCrossCountry",
        "Tracked",
        "WheeledTracked",
        "Towed",
        "Railway",
        "OverSnow",
        "Sled",
        "PackAnimals",
        "Barge",
        "Amphibious"
    };

    private static readonly string[] OperationalConditions =
    {
        "FullyOperational",
        "DamagedButOperational",
        "Destroyed",
        "FullToCapacity"
    };

    private static readonly string[] UnitEchelons =
    {
        "Team (Ø)",
        "Squad (•)",
        "Section (••)",
        "Platoon (•••)",
        "Company (I)",
        "Battalion (II)",
        "Regiment (III)",
        "Brigade (X)",
        "Division (XX)",
        "Corps (XXX)",
        "Army (XXXX)",
        "Army group (XXXXX)",
        "Region (XXXXXX)",
        "Command (++)",
        "Team task force (Ø + D)",
        "Squad task force (• + D)",
        "Section task force (•• + D)",
        "Platoon task force (••• + D)",
        "Company task force (I + D)",
        "Battalion task force (II + D)",
        "Regiment task force (III + D)",
        "Brigade task force (X + D)",
        "Division task force (XX + D)",
        "Corps task force (XXX + D)",
        "Army task force (XXXX + D)",
        "Army group task force (XXXXX + D)",
        "Region task force (XXXXXX + D)",
        "Command task force (++ + D)",
        "Company team (I + D)",
        "Marine expeditionary force (XXX + D)"
    };
    private static readonly string[] EngagementBars =
    {
        "HostileTarget",
        "HostileNonTarget",
        "HostileExpiredTarget",
        "FriendlyParticipating"
    };

    public static OrbatSymbolAmplifierLayout LandUnit { get; } = new(
        OrbatSymbolDomain.LandUnit,
        new[]
        {
            new OrbatSymbolAmplifierField(
                "B/C/D",
                "B / D",
                OrbatAmplifierArea.Top,
                2,
                0,
                "Echelon / task organization",
                "B identifies command level. D draws the task-organization bracket around any echelon symbol.",
                options: UnitEchelons),
            new OrbatSymbolAmplifierField(
                "H/AF",
                "H",
                OrbatAmplifierArea.Right,
                2,
                0,
                "Additional information / unit designation",
                "A unique alphanumeric designation that identifies the displayed unit, beginning with the unit's own designation followed by its higher-echelon command designation. Example: A/6-37.",
                maxLength: 20),
            new OrbatSymbolAmplifierField("A/AA", "A/AA", OrbatAmplifierArea.Center, 0, 0),
            new OrbatSymbolAmplifierField("AO", "AO", OrbatAmplifierArea.Top, 0, 0),
            new OrbatSymbolAmplifierField("AB", "AB", OrbatAmplifierArea.Top, 1, 0),
            new OrbatSymbolAmplifierField("AC", "AC", OrbatAmplifierArea.Top, 3, 0),
            new OrbatSymbolAmplifierField("AR/W", "AR/W", OrbatAmplifierArea.Left, 0, 0),
            new OrbatSymbolAmplifierField("X/Y", "X/Y", OrbatAmplifierArea.Left, 1, 0),
            new OrbatSymbolAmplifierField("V/AD/AE", "V/AD/AE", OrbatAmplifierArea.Left, 2, 0),
            new OrbatSymbolAmplifierField("C/T", "C/T", OrbatAmplifierArea.Left, 3, 0),
            new OrbatSymbolAmplifierField("Z", "Z", OrbatAmplifierArea.Left, 4, 0),
            new OrbatSymbolAmplifierField("F/AS", "F/AS", OrbatAmplifierArea.Right, 0, 0),
            new OrbatSymbolAmplifierField("G", "G", OrbatAmplifierArea.Right, 1, 0),
            new OrbatSymbolAmplifierField("M", "M", OrbatAmplifierArea.Right, 3, 0),
            new OrbatSymbolAmplifierField("J/K/P", "J/K/P", OrbatAmplifierArea.Right, 4, 0),
            new OrbatSymbolAmplifierField("R/AW", "R/AW", OrbatAmplifierArea.Bottom, 0, 0),
            new OrbatSymbolAmplifierField("AL", "AL", OrbatAmplifierArea.Bottom, 1, 0),
            new OrbatSymbolAmplifierField("S", "S", OrbatAmplifierArea.Connector, 0, 0),
            new OrbatSymbolAmplifierField("S2", "S2", OrbatAmplifierArea.Connector, 0, 1),
            new OrbatSymbolAmplifierField("Q", "Q", OrbatAmplifierArea.Connector, 0, 2)
        });

    public static OrbatSymbolAmplifierLayout Equipment { get; } = new(
        OrbatSymbolDomain.Equipment,
        new[]
        {
            new OrbatSymbolAmplifierField("A", "A", OrbatAmplifierArea.Center, 0, 0, "Main function and modifier symbols", "The innermost part of a symbol that represents the equipment main function and modifiers."),
            new OrbatSymbolAmplifierField("AO", "AO", OrbatAmplifierArea.Top, 0, 0, "Engagement bar", "Graphic/color amplifier immediately atop the symbol. Table 4-3 defines designation colors for hostile target, hostile non-target, hostile expired target, and friendly participating.", options: EngagementBars, valueKind: OrbatAmplifierValueKind.ColorStatus),
            new OrbatSymbolAmplifierField("C", "C", OrbatAmplifierArea.Top, 1, 0, "Quantity", "Number of items present.", maxLength: 9),
            new OrbatSymbolAmplifierField("W/R", "W/R", OrbatAmplifierArea.Left, 0, 0, "Date-time group / mobility mode", "W is a date-time group or O/O order. R is equipment mobility mode.", maxLength: 16, options: MobilityModes),
            new OrbatSymbolAmplifierField("Y/Y", "Y/Y", OrbatAmplifierArea.Left, 1, 0, "Location", "Location in MGRS, GARS, latitude/longitude, or other applicable display format.", maxLength: 22),
            new OrbatSymbolAmplifierField("V/AD/AE", "V/AD/AE", OrbatAmplifierArea.Left, 2, 0, "Type / platform / teardown time", "V is equipment type, AD is platform type, AE is teardown time in minutes.", maxLength: 24),
            new OrbatSymbolAmplifierField("T", "T", OrbatAmplifierArea.Left, 3, 0, "Unique identifier", "Command and control unique track identifier. Example: TN: 13579.", maxLength: 30),
            new OrbatSymbolAmplifierField("Z", "Z", OrbatAmplifierArea.Left, 4, 0, "Speed", "Velocity with unit of measure.", maxLength: 8),
            new OrbatSymbolAmplifierField("G/AQ", "G/AQ", OrbatAmplifierArea.Right, 0, 0, "Staff comments / guarded unit", "G is implementation-specific staff comments. AQ is guarded unit during ballistic missile defense.", maxLength: 20),
            new OrbatSymbolAmplifierField("H/AF", "H/AF", OrbatAmplifierArea.Right, 1, 0, "Additional information / common identifier", "H is implementation-specific additional information. AF is common system or model name.", maxLength: 20),
            new OrbatSymbolAmplifierField("J/L/N/P", "J/L/N/P", OrbatAmplifierArea.Right, 2, 0, "Evaluation / signature / hostile / IFF", "J is evaluation rating, L is electronic signature, N is hostile symbol marker, P is IFF/SIF modes and codes.", maxLength: 15, minLength: 2, options: EvaluationRatings.Concat(SignatureEquipment).Concat(HostileSymbols).ToArray()),
            new OrbatSymbolAmplifierField("R/AG", "R/AG", OrbatAmplifierArea.Bottom, 0, 0, "Mobility mode / auxiliary equipment", "R depicts mobility mode. AG indicates auxiliary equipment such as a towed sonar array.", options: MobilityModes),
            new OrbatSymbolAmplifierField("AL", "AL", OrbatAmplifierArea.Bottom, 1, 0, "Operational condition", "Graphic/color amplifier indicating operational condition or capacity. Table 4-5 defines fully operational, damaged but operational, destroyed, and full to capacity.", options: OperationalConditions, valueKind: OrbatAmplifierValueKind.ColorStatus),
            new OrbatSymbolAmplifierField("S2", "S2", OrbatAmplifierArea.Connector, 0, 0, "Offset location indicator", "Graphic amplifier used to indicate the offset or precise location of a single point symbol."),
            new OrbatSymbolAmplifierField("Q", "Q", OrbatAmplifierArea.Connector, 0, 1, "Direction of movement indicator", "Graphic amplifier identifying direction of movement or intended movement.")
        });

    public static OrbatSymbolAmplifierLayout GetLayout(OrbatSymbolDomain domain)
    {
        return domain == OrbatSymbolDomain.Equipment ? Equipment : LandUnit;
    }
}
