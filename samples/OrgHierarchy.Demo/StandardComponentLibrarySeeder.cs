using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrgHierarchy.Components;

namespace OrgHierarchy.Demo;

internal static class StandardComponentLibrarySeeder
{
    public static void EnsureFiles(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());

        foreach (var modifier in Enum.GetValues<OrbatLandUnitModifier1>()
            .Where(value => value != OrbatLandUnitModifier1.Unspecified))
        {
            SaveIfMissing(
                folder,
                $"LandUnit.Modifier1.{modifier}.orbatsymbol.json",
                CreateDefinition(
                    modifier.ToString(),
                    OrbatEquipmentSymbolRole.Modifier1,
                    BuiltInSymbolLibrary.Create(modifier),
                    physicalDomain: SymbolPhysicalDomain.LandUnit,
                    landUnitModifier1Type: modifier.ToString()),
                options);
        }

        foreach (var modifier in Enum.GetValues<OrbatLandUnitModifier2>()
            .Where(value => value != OrbatLandUnitModifier2.Unspecified))
        {
            SaveIfMissing(
                folder,
                $"LandUnit.Modifier2.{modifier}.orbatsymbol.json",
                CreateDefinition(
                    modifier.ToString(),
                    OrbatEquipmentSymbolRole.Modifier2,
                    BuiltInSymbolLibrary.Create(modifier),
                    physicalDomain: SymbolPhysicalDomain.LandUnit,
                    landUnitModifier2Type: modifier.ToString()),
                options);
        }

        foreach (var modifier in Enum.GetValues<OrbatEquipmentModifier1>()
            .Where(value => value != OrbatEquipmentModifier1.Unspecified))
        {
            SaveIfMissing(
                folder,
                $"Equipment.Modifier1.{modifier}.orbatsymbol.json",
                CreateDefinition(
                    modifier.ToString(),
                    OrbatEquipmentSymbolRole.Modifier1,
                    BuiltInSymbolLibrary.Create(modifier),
                    modifier1Type: modifier.ToString()),
                options);
        }

        foreach (var modifier in Enum.GetValues<OrbatEquipmentModifier2>()
            .Where(value => value != OrbatEquipmentModifier2.Unspecified))
        {
            SaveIfMissing(
                folder,
                $"Equipment.Modifier2.{modifier}.orbatsymbol.json",
                CreateDefinition(
                    modifier.ToString(),
                    OrbatEquipmentSymbolRole.Modifier2,
                    BuiltInSymbolLibrary.Create(modifier),
                    modifier2Type: modifier.ToString()),
                options);
        }

        foreach (var echelon in Enum.GetValues<OrbatEchelon>()
            .Where(value => value != OrbatEchelon.Unspecified))
        {
            SaveIfMissing(
                folder,
                $"LandUnit.Amplifier.B.{echelon}.orbatsymbol.json",
                CreateDefinition(
                    echelon.ToString(),
                    OrbatEquipmentSymbolRole.EchelonIndicator,
                    BuiltInSymbolLibrary.Create(echelon),
                    physicalDomain: SymbolPhysicalDomain.LandUnit,
                    echelonType: echelon.ToString()),
                options);
        }

        foreach (var mobility in Enum.GetValues<OrbatEquipmentMobilityMode>()
            .Where(value => value != OrbatEquipmentMobilityMode.Unspecified))
        {
            SaveIfMissing(
                folder,
                $"Equipment.Amplifier.R.{mobility}.orbatsymbol.json",
                CreateDefinition(
                    mobility.ToString(),
                    OrbatEquipmentSymbolRole.MobilityIndicator,
                    BuiltInSymbolLibrary.Create(mobility),
                    mobilityType: mobility.ToString()),
                options);
        }
    }

    private static SymbolLibraryDefinition CreateDefinition(
        string name,
        OrbatEquipmentSymbolRole role,
        IReadOnlyList<SymbolDrawCommand> commands,
        string? modifier1Type = null,
        string? modifier2Type = null,
        string? mobilityType = null,
        string? echelonType = null,
        SymbolPhysicalDomain physicalDomain = SymbolPhysicalDomain.Equipment,
        string? landUnitModifier1Type = null,
        string? landUnitModifier2Type = null)
    {
        var definition = new SymbolLibraryDefinition
        {
            Version = SymbolLibraryDefinition.CurrentSchemaVersion,
            Name = name,
            UnitType = OrbatUnitType.Unspecified.ToString(),
            UnitCategory = OrbatUnitMainFunctionCategory.All.ToString(),
            UnitMainFunction = OrbatUnitMainFunction.Unspecified.ToString(),
            EquipmentFunction = OrbatEquipmentFunction.Unspecified.ToString(),
            Variant = string.Empty,
            SymbolRole = role,
            CompositionMode = OrbatEquipmentCompositionMode.Composable,
            Layout = OrbatEquipmentSymbolLayout.CreateDefault(),
            Modifier1Type = modifier1Type ?? OrbatEquipmentModifier1.Unspecified.ToString(),
            Modifier2Type = modifier2Type ?? OrbatEquipmentModifier2.Unspecified.ToString(),
            LandUnitModifier1Type = landUnitModifier1Type ?? OrbatLandUnitModifier1.Unspecified.ToString(),
            LandUnitModifier2Type = landUnitModifier2Type ?? OrbatLandUnitModifier2.Unspecified.ToString(),
            MobilityType = mobilityType ?? OrbatEquipmentMobilityMode.Unspecified.ToString(),
            EchelonType = echelonType ?? OrbatEchelon.Unspecified.ToString(),
            Affiliation = SymbolAffiliation.Friendly,
            PhysicalDomain = physicalDomain,
            FrameShape = physicalDomain == SymbolPhysicalDomain.Equipment
                ? SymbolFrameShape.FriendlyEquipment
                : SymbolFrameShape.FriendlyUnit,
            FrameStatus = SymbolFrameStatus.Present,
            OperatingState = OrbatEquipmentOperatingState.Ground,
            Commands = commands.Select(command => command.Clone()).ToList()
        };
        definition.LibraryId = definition.GetEffectiveLibraryId();
        definition.LibraryVersion = 1;
        return definition;
    }

    private static void SaveIfMissing(
        string folder,
        string fileName,
        SymbolLibraryDefinition definition,
        JsonSerializerOptions options)
    {
        var path = Path.Combine(folder, fileName);
        if (File.Exists(path) || definition.Commands.Count == 0)
            return;

        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(definition, options), Encoding.UTF8);
        }
        catch
        {
            // A read-only library still works through the built-in fallback.
        }
    }
}