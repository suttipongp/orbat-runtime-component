namespace OrgHierarchy.Components;

public static class OrbatDataTableMapper
{
    public static IReadOnlyList<OrbatUnitRecord> FromDataTable(
        DataTable table,
        string idColumn,
        string parentIdColumn,
        string nameColumn,
        string shortNameColumn,
        string uniqueDesignationColumn,
        string affiliationColumn,
        string echelonColumn,
        string unitTypeColumn,
        string sidcColumn,
        string symbolTextColumn,
        string headquartersColumn,
        string taskForceColumn,
        string plannedAnticipatedColumn,
        string stackCountColumn,
        string reinforcedReducedColumn,
        string reinforcedColumn,
        string reducedColumn,
        string sortColumn,
        bool sidcOverridesFields = true,
        string symbolDomainColumn = "SymbolDomain")
    {
        if (table == null)
            throw new ArgumentNullException(nameof(table));

        RequireColumn(table, idColumn);
        RequireColumn(table, nameColumn);

        var records = new List<OrbatUnitRecord>();
        foreach (DataRow row in table.Rows)
        {
            var id = ReadString(row, idColumn);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var legacyReinforced = ReadBoolean(row, reinforcedColumn);
            var legacyReduced = ReadBoolean(row, reducedColumn);
            var reinforcedReduced = ReadReinforcedReduced(row, reinforcedReducedColumn, legacyReinforced, legacyReduced);
            var sidc = ReadOptionalString(row, sidcColumn);
            var sidcParts = OrbatSidcParser.Parse(sidc);
            var affiliationText = ReadString(row, affiliationColumn);
            var echelonText = ReadString(row, echelonColumn);
            var unitTypeText = ReadString(row, unitTypeColumn);
            var useSidc = sidcOverridesFields && sidcParts.IsValid;
            var affiliation = useSidc && sidcParts.Affiliation.HasValue
                ? sidcParts.Affiliation.Value
                : ReadEnum(affiliationText, sidcParts.Affiliation ?? OrbatAffiliation.Friend);
            var echelon = useSidc && sidcParts.Echelon.HasValue
                ? sidcParts.Echelon.Value
                : ReadEnum(echelonText, sidcParts.Echelon ?? OrbatEchelon.Battalion);
            var unitType = useSidc && sidcParts.UnitType.HasValue
                ? sidcParts.UnitType.Value
                : ReadUnitType(unitTypeText, sidcParts.UnitType ?? OrbatUnitType.Unspecified);
            var symbolDomain = ReadEnum(ReadString(row, symbolDomainColumn), OrbatSymbolDomain.LandUnit);
            var headquarters = useSidc && sidcParts.Headquarters.HasValue
                ? sidcParts.Headquarters.Value
                : ReadBoolean(row, headquartersColumn) || sidcParts.Headquarters == true;
            var taskForce = useSidc && sidcParts.TaskForce.HasValue
                ? sidcParts.TaskForce.Value
                : ReadBoolean(row, taskForceColumn) || sidcParts.TaskForce == true;
            var plannedAnticipated = useSidc && sidcParts.PlannedAnticipated.HasValue
                ? sidcParts.PlannedAnticipated.Value
                : ReadBoolean(row, plannedAnticipatedColumn) || sidcParts.PlannedAnticipated == true;
            sidc ??= OrbatSidcParser.Compose(affiliation, echelon, unitType, headquarters, taskForce, plannedAnticipated);

            var record = new OrbatUnitRecord
            {
                Id = id,
                ParentId = ReadOptionalString(row, parentIdColumn),
                Name = ReadString(row, nameColumn),
                ShortName = ReadOptionalString(row, shortNameColumn),
                UniqueDesignation = ReadOptionalString(row, uniqueDesignationColumn),
                Affiliation = affiliation,
                SymbolDomain = symbolDomain,
                Echelon = echelon,
                UnitType = unitType,
                Sidc = sidc,
                SymbolText = ReadOptionalString(row, symbolTextColumn),
                Headquarters = headquarters,
                TaskForce = taskForce,
                PlannedAnticipated = plannedAnticipated,
                StackCount = Math.Max(1, Math.Min(6, ReadInteger(row, stackCountColumn))),
                ReinforcedReduced = reinforcedReduced,
                Reinforced = reinforcedReduced == OrbatReinforcedReduced.Reinforced || reinforcedReduced == OrbatReinforcedReduced.ReinforcedAndReduced,
                Reduced = reinforcedReduced == OrbatReinforcedReduced.Reduced || reinforcedReduced == OrbatReinforcedReduced.ReinforcedAndReduced,
                SortOrder = ReadInteger(row, sortColumn)
            };
            ReadAmplifiers(row, record);
            records.Add(record);
        }

        return records;
    }

    private static void RequireColumn(DataTable table, string columnName)
    {
        if (!table.Columns.Contains(columnName))
            throw new ArgumentException($"The data table must contain a '{columnName}' column.", nameof(table));
    }

    private static string ReadString(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            return string.Empty;

        return Convert.ToString(row[columnName]) ?? string.Empty;
    }

    private static string? ReadOptionalString(DataRow row, string columnName)
    {
        var value = ReadString(row, columnName);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static TEnum ReadEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
    }

    private static OrbatUnitType ReadUnitType(string value, OrbatUnitType fallback)
    {
        if (value.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return OrbatUnitType.Unspecified;

        return Enum.TryParse(value, true, out OrbatUnitType parsed) ? parsed : fallback;
    }

    private static void ReadAmplifiers(DataRow row, OrbatUnitRecord record)
    {
        var layout = OrbatSymbolAmplifierLayouts.GetLayout(record.SymbolDomain);
        foreach (var field in layout.Fields)
        {
            var value = ReadAmplifierValue(row, field);
            if (!string.IsNullOrWhiteSpace(value))
                record.Amplifiers[field.Key] = value;
        }
    }

    private static string ReadAmplifierValue(DataRow row, OrbatSymbolAmplifierField field)
    {
        var sanitized = SanitizeAmplifierKey(field.Key);
        foreach (DataColumn column in row.Table.Columns)
        {
            var columnName = column.ColumnName;
            if (columnName.Equals(field.Key, StringComparison.OrdinalIgnoreCase)
                || columnName.Equals(sanitized, StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("Amplifier_" + sanitized, StringComparison.OrdinalIgnoreCase))
            {
                return ReadString(row, columnName);
            }
        }

        return string.Empty;
    }

    private static string SanitizeAmplifierKey(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray());
    }

    private static bool ReadBoolean(DataRow row, string columnName)
    {
        var value = ReadString(row, columnName);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (bool.TryParse(value, out var parsed))
            return parsed;

        return value == "1" || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private static OrbatReinforcedReduced ReadReinforcedReduced(DataRow row, string columnName, bool reinforced, bool reduced)
    {
        var value = ReadString(row, columnName).Trim();
        if (!string.IsNullOrWhiteSpace(value))
        {
            var normalized = value.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
            if (Enum.TryParse(normalized, true, out OrbatReinforcedReduced parsed))
                return parsed;

            if (value == "+")
                return OrbatReinforcedReduced.Reinforced;
            if (value == "-")
                return OrbatReinforcedReduced.Reduced;
            if (value == "±" || value == "+/-" || value.Equals("both", StringComparison.OrdinalIgnoreCase))
                return OrbatReinforcedReduced.ReinforcedAndReduced;
        }

        if (reinforced && reduced)
            return OrbatReinforcedReduced.ReinforcedAndReduced;
        if (reinforced)
            return OrbatReinforcedReduced.Reinforced;
        return reduced ? OrbatReinforcedReduced.Reduced : OrbatReinforcedReduced.NotApplicable;
    }

    private static int ReadInteger(DataRow row, string columnName)
    {
        var value = ReadString(row, columnName);
        return int.TryParse(value, out var parsed) ? parsed : 0;
    }
}
