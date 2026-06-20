# OrgHierarchy Runtime Components

Windows Forms runtime component library for Visual Studio .NET. The main control,
`OrgHierarchyTreeView`, displays organization, department, position, and person
hierarchies from database-shaped data.

The package also includes `OrganizationChartView`, a visual organization chart
control that renders the same hierarchy data as connected boxes.

`OrbatChartView` adds an order of battle (ORBAT) chart control for military unit
hierarchies. It uses the same runtime/data-table style as the organization
controls, with configurable unit properties inspired by Battle Staff Tools /
Unit Generator: affiliation, echelon, unit type, headquarters, task force,
reinforced, and reduced status.

## Projects

- `src/OrgHierarchy.Components` - reusable WinForms component library.
- `samples/OrgHierarchy.Demo` - sample WinForms app that loads hierarchy and ORBAT data into the components.

## Install in Visual Studio

Build the install package:

```powershell
dotnet pack .\src\OrgHierarchy.Components\OrgHierarchy.Components.csproj -c Release -o .\artifacts\packages
```

Install from Visual Studio:

1. Open your WinForms project.
2. Go to `Tools > NuGet Package Manager > Package Manager Settings`.
3. Add a package source that points to the local `artifacts\packages` folder.
4. Open `Manage NuGet Packages`, select that source, and install `OrgHierarchy.Components`.
5. Build the project. `OrgHierarchyTreeView` can be created in code, and Visual Studio can also add it to the Toolbox from the referenced assembly.

The package supports `.NET Framework 4.7.2` WinForms projects and modern
`.NET 8`, `.NET 9`, and `.NET 10` Windows Forms projects.

Manual Toolbox install:

1. Right-click the Visual Studio Toolbox and choose `Choose Items`.
2. Browse to `src\OrgHierarchy.Components\bin\Release\net8.0-windows\OrgHierarchy.Components.dll`.
3. Select `OrgHierarchyTreeView`.
4. You can also select `OrganizationChartView` and `OrbatChartView`.

## Expected Database Shape

The component accepts a flat `DataTable` and builds the tree from ID and Parent ID columns.

| Column | Example | Required |
| --- | --- | --- |
| `Id` | `EMP-001` | Yes |
| `ParentId` | `POS-110` | Yes, can be null for root nodes |
| `DisplayName` | `Somchai Prasert` | Yes |
| `Subtitle` | `Employee No. E001` | No |
| `Kind` | `Organization`, `Department`, `Position`, `Person` | No |
| `SortOrder` | `10` | No |

## SQL Example

```sql
SELECT
    CAST(Id AS nvarchar(50)) AS Id,
    CAST(ParentId AS nvarchar(50)) AS ParentId,
    DisplayName,
    Subtitle,
    Kind,
    SortOrder
FROM dbo.OrganizationHierarchy
ORDER BY SortOrder, DisplayName;
```

## Runtime Usage

```csharp
var hierarchy = new OrgHierarchyTreeView();
hierarchy.SetDataLoader(async cancellationToken =>
{
    using var connection = new SqlConnection(connectionString);
    using var command = new SqlCommand(sql, connection);
    using var adapter = new SqlDataAdapter(command);

    var table = new DataTable();
    await connection.OpenAsync(cancellationToken);
    adapter.Fill(table);
    return table;
});

await hierarchy.ReloadAsync();
```

You can rename the expected columns through `IdColumn`, `ParentIdColumn`,
`DisplayColumn`, `SubtitleColumn`, `KindColumn`, and `SortColumn` in Visual Studio
Properties or in code.

## Organization Chart Usage

```csharp
var chart = new OrganizationChartView
{
    Dock = DockStyle.Fill,
    ShowSubtitles = false
};

chart.LoadFromDataTable(table);
chart.FitToView();
```

`OrganizationChartView` supports zoom, fit-to-view, scrolling, node selection,
and the same `NodeActivated` event used by `OrgHierarchyTreeView`.

## ORBAT Database Shape

`OrbatChartView` accepts a flat `DataTable` and builds the command hierarchy
from ID and Parent ID columns.

| Column | Example | Required |
| --- | --- | --- |
| `Id` | `III-CORPS` | Yes |
| `ParentId` | `CJTF-HQ` | Yes, can be null for root units |
| `Name` | `III Corps` | Yes |
| `ShortName` | `3 BCT` | No |
| `UniqueDesignation` | `420` | No |
| `Affiliation` | `Unspecified`, `Friend`, `Hostile`, `Neutral`, `Unknown`, `Suspect`, `Civilian` | No |
| `Echelon` | `Team`, `Company`, `Battalion`, `Brigade`, `Division`, `Corps` | No |
| `UnitType` | `Unspecified`, `Headquarters`, `Infantry`, `Armor`, `MechanizedInfantry`, `Artillery`, `Air`, `Naval`, `SpecialOperations`, `Logistics` | No |
| `Sidc` | `10031010171211000000` | No |
| `SymbolText` | `CG`, `CSS`, `HQ` | No |
| `Headquarters` | `true` | No |
| `TaskForce` | `true` | No |
| `PlannedAnticipated` | `true` | No |
| `StackCount` | `1` through `6` | No |
| `Reinforced` | `true` | No |
| `Reduced` | `false` | No |
| `SortOrder` | `10` | No |

## ORBAT Runtime Usage

```csharp
var orbat = new OrbatChartView
{
    Dock = DockStyle.Fill,
    ShowLegend = true
};

orbat.SetDataLoader(async cancellationToken =>
{
    using var connection = new SqlConnection(connectionString);
    using var command = new SqlCommand(sql, connection);
    using var adapter = new SqlDataAdapter(command);

    var table = new DataTable();
    await connection.OpenAsync(cancellationToken);
    adapter.Fill(table);
    return table;
});

orbat.UnitActivated += (_, args) =>
{
    var selectedUnit = args.Unit;
};

await orbat.ReloadAsync();
orbat.FitToView();
```

You can rename the expected ORBAT columns through `IdColumn`, `ParentIdColumn`,
`NameColumn`, `ShortNameColumn`, `UniqueDesignationColumn`, `AffiliationColumn`,
`EchelonColumn`, `UnitTypeColumn`, `SidcColumn`, `SymbolTextColumn`, `HeadquartersColumn`, `TaskForceColumn`,
`PlannedAnticipatedColumn`, `StackCountColumn`,
`ReinforcedColumn`, `ReducedColumn`, and `SortColumn`.

When `Sidc` is present, `OrbatChartView` can derive common MIL-STD-2525D-style
values for affiliation, echelon, selected unit types, headquarters, task force,
and planned/anticipated status. Explicit table columns still take precedence for
the enum values, while boolean flags are enabled when either the table column or
SIDC indicates them.

When `Sidc` is missing, the component creates a best-effort 20-digit SIDC from
the existing ORBAT fields so older data can still expose a usable SIDC value.
