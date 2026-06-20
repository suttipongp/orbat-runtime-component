using System.Data;
using System.Globalization;

namespace OrgHierarchy.Components;

public static class DataTableHierarchyMapper
{
    public static IReadOnlyList<HierarchyNodeRecord> FromDataTable(
        DataTable table,
        string idColumn,
        string parentIdColumn,
        string displayColumn,
        string? subtitleColumn = null,
        string? kindColumn = null,
        string? sortColumn = null)
    {
        if (table == null)
            throw new ArgumentNullException(nameof(table));

        EnsureColumn(table, idColumn);
        EnsureColumn(table, parentIdColumn);
        EnsureColumn(table, displayColumn);

        if (!string.IsNullOrWhiteSpace(subtitleColumn))
            EnsureColumn(table, subtitleColumn);
        if (!string.IsNullOrWhiteSpace(kindColumn))
            EnsureColumn(table, kindColumn);
        if (!string.IsNullOrWhiteSpace(sortColumn))
            EnsureColumn(table, sortColumn);

        return table.Rows
            .Cast<DataRow>()
            .Select(row => new HierarchyNodeRecord
            {
                Id = ReadString(row, idColumn) ?? string.Empty,
                ParentId = ReadString(row, parentIdColumn),
                DisplayName = ReadString(row, displayColumn) ?? string.Empty,
                Subtitle = string.IsNullOrWhiteSpace(subtitleColumn) ? null : ReadString(row, subtitleColumn),
                Kind = string.IsNullOrWhiteSpace(kindColumn) ? HierarchyNodeKind.Unknown : ReadKind(row, kindColumn),
                SortOrder = string.IsNullOrWhiteSpace(sortColumn) ? 0 : ReadInt(row, sortColumn),
                DataItem = row
            })
            .Where(record => !string.IsNullOrWhiteSpace(record.Id) && !string.IsNullOrWhiteSpace(record.DisplayName))
            .ToList();
    }

    private static void EnsureColumn(DataTable table, string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName) || !table.Columns.Contains(columnName))
            throw new ArgumentException($"DataTable does not contain column '{columnName}'.", nameof(columnName));
    }

    private static string? ReadString(DataRow row, string columnName)
    {
        var value = row[columnName];
        if (value == null || value == DBNull.Value)
            return null;

        var text = Convert.ToString(value, CultureInfo.CurrentCulture);
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static int ReadInt(DataRow row, string columnName)
    {
        var value = row[columnName];
        if (value == null || value == DBNull.Value)
            return 0;

        return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var result) ? result : 0;
    }

    private static HierarchyNodeKind ReadKind(DataRow row, string columnName)
    {
        var value = row[columnName];
        if (value == null || value == DBNull.Value)
            return HierarchyNodeKind.Unknown;

        if (value is int number && Enum.IsDefined(typeof(HierarchyNodeKind), number))
            return (HierarchyNodeKind)number;

        return Enum.TryParse<HierarchyNodeKind>(Convert.ToString(value, CultureInfo.InvariantCulture), true, out var kind)
            ? kind
            : HierarchyNodeKind.Unknown;
    }
}
