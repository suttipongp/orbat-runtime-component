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
        string? mobilityType = null)
    {
        return new SymbolLibraryDefinition
        {
            Version = 4,
            Name = name,
            UnitType = OrbatUnitType.Unspecified.ToString(),
            EquipmentFunction = OrbatEquipmentFunction.Unspecified.ToString(),
            Variant = string.Empty,
            SymbolRole = role,
            CompositionMode = OrbatEquipmentCompositionMode.Composable,
            Layout = OrbatEquipmentSymbolLayout.CreateDefault(),
            Modifier1Type = modifier1Type ?? OrbatEquipmentModifier1.Unspecified.ToString(),
            Modifier2Type = modifier2Type ?? OrbatEquipmentModifier2.Unspecified.ToString(),
            MobilityType = mobilityType ?? OrbatEquipmentMobilityMode.Unspecified.ToString(),
            Affiliation = SymbolAffiliation.Friendly,
            PhysicalDomain = SymbolPhysicalDomain.Equipment,
            FrameShape = SymbolFrameShape.FriendlyEquipment,
            FrameStatus = SymbolFrameStatus.Present,
            OperatingState = OrbatEquipmentOperatingState.Ground,
            Commands = commands.Select(command => command.Clone()).ToList()
        };
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