using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrgHierarchy.Components;

public enum OrbatEquipmentFunctionCategory
{
    All,
    General,
    Weapons,
    Vehicles,
    Engineering,
    Aviation,
    Maritime,
    C4IsrAndSensors,
    Support,
    Special
}

public static class OrbatEquipmentFunctionCatalog
{
    public static OrbatEquipmentFunctionCategory GetCategory(OrbatEquipmentFunction function)
    {
        switch (function)
        {
            case OrbatEquipmentFunction.Mortar:
            case OrbatEquipmentFunction.Gun:
            case OrbatEquipmentFunction.Howitzer:
            case OrbatEquipmentFunction.DirectFireGun:
            case OrbatEquipmentFunction.AntiTankGun:
            case OrbatEquipmentFunction.Launcher:
            case OrbatEquipmentFunction.MissileLauncher:
            case OrbatEquipmentFunction.GrenadeLauncher:
            case OrbatEquipmentFunction.AirDefenseGun:
            case OrbatEquipmentFunction.Rifle:
            case OrbatEquipmentFunction.MachineGun:
            case OrbatEquipmentFunction.RecoillessGun:
            case OrbatEquipmentFunction.AirDefenseMissile:
            case OrbatEquipmentFunction.AntiTankMissileLauncher:
            case OrbatEquipmentFunction.SurfaceToSurfaceMissileLauncher:
            case OrbatEquipmentFunction.AntiTankRocketLauncher:
            case OrbatEquipmentFunction.SingleRocketLauncher:
            case OrbatEquipmentFunction.MultipleRocketLauncher:
            case OrbatEquipmentFunction.NonlethalWeapon:
            case OrbatEquipmentFunction.StunGun:
            case OrbatEquipmentFunction.WaterCannon:
            case OrbatEquipmentFunction.DirectedEnergy:
                return OrbatEquipmentFunctionCategory.Weapons;

            case OrbatEquipmentFunction.Vehicle:
            case OrbatEquipmentFunction.ArmoredVehicle:
            case OrbatEquipmentFunction.WheeledVehicle:
            case OrbatEquipmentFunction.TrackedVehicle:
            case OrbatEquipmentFunction.ArmoredFightingVehicle:
            case OrbatEquipmentFunction.ArmoredPersonnelCarrier:
            case OrbatEquipmentFunction.ArmoredProtected:
            case OrbatEquipmentFunction.Tank:
            case OrbatEquipmentFunction.Engine:
            case OrbatEquipmentFunction.Flatbed:
                return OrbatEquipmentFunctionCategory.Vehicles;

            case OrbatEquipmentFunction.EngineerEquipment:
            case OrbatEquipmentFunction.Bridge:
            case OrbatEquipmentFunction.Drill:
            case OrbatEquipmentFunction.MineClearing:
            case OrbatEquipmentFunction.MineLaying:
                return OrbatEquipmentFunctionCategory.Engineering;

            case OrbatEquipmentFunction.FixedWing:
            case OrbatEquipmentFunction.RotaryWing:
            case OrbatEquipmentFunction.UnmannedAircraft:
                return OrbatEquipmentFunctionCategory.Aviation;

            case OrbatEquipmentFunction.Destroyer:
            case OrbatEquipmentFunction.MilitaryNoncombatantShip:
            case OrbatEquipmentFunction.CivilianMerchantShip:
                return OrbatEquipmentFunctionCategory.Maritime;

            case OrbatEquipmentFunction.Radar:
            case OrbatEquipmentFunction.Sensor:
            case OrbatEquipmentFunction.Antenna:
            case OrbatEquipmentFunction.CommunicationsEquipment:
            case OrbatEquipmentFunction.CommunicationsSatellite:
            case OrbatEquipmentFunction.Computer:
            case OrbatEquipmentFunction.CyberServer:
                return OrbatEquipmentFunctionCategory.C4IsrAndSensors;

            case OrbatEquipmentFunction.MedicalEquipment:
            case OrbatEquipmentFunction.LogisticsEquipment:
                return OrbatEquipmentFunctionCategory.Support;

            case OrbatEquipmentFunction.CBRN:
            case OrbatEquipmentFunction.PsychologicalOperations:
                return OrbatEquipmentFunctionCategory.Special;

            default:
                return OrbatEquipmentFunctionCategory.General;
        }
    }

    public static bool SupportsInFlightOperatingState(OrbatEquipmentFunction function)
    {
        return GetCategory(function) == OrbatEquipmentFunctionCategory.Aviation
            || function == OrbatEquipmentFunction.CommunicationsSatellite;
    }

    public static OrbatEquipmentOperatingState GetDefaultOperatingState(OrbatEquipmentFunction function)
    {
        return function == OrbatEquipmentFunction.CommunicationsSatellite
            ? OrbatEquipmentOperatingState.InFlight
            : OrbatEquipmentOperatingState.Ground;
    }
    public static IReadOnlyList<OrbatEquipmentFunction> GetFunctions(OrbatEquipmentFunctionCategory category)
    {
        return Enum.GetValues(typeof(OrbatEquipmentFunction))
            .Cast<OrbatEquipmentFunction>()
            .Where(function => category == OrbatEquipmentFunctionCategory.All || GetCategory(function) == category)
            .OrderBy(GetDisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static string GetCategoryDisplayName(OrbatEquipmentFunctionCategory category)
    {
        return category == OrbatEquipmentFunctionCategory.C4IsrAndSensors
            ? "C4ISR & Sensors"
            : SplitPascalCase(category.ToString());
    }

    public static string GetDisplayName(OrbatEquipmentFunction function)
    {
        switch (function)
        {
            case OrbatEquipmentFunction.AntiTankGun:
                return "Anti-tank Gun";
            case OrbatEquipmentFunction.AntiTankMissileLauncher:
                return "Anti-tank Missile Launcher";
            case OrbatEquipmentFunction.AntiTankRocketLauncher:
                return "Anti-tank Rocket Launcher";
            case OrbatEquipmentFunction.SurfaceToSurfaceMissileLauncher:
                return "Surface-to-surface Missile Launcher";
            case OrbatEquipmentFunction.CBRN:
                return "CBRN";
            default:
                return SplitPascalCase(function.ToString());
        }
    }

    public static bool TryParseDisplayName(string value, out OrbatEquipmentFunction function)
    {
        if (Enum.TryParse(value, true, out function))
            return true;

        var normalized = Normalize(value);
        foreach (OrbatEquipmentFunction candidate in Enum.GetValues(typeof(OrbatEquipmentFunction)))
        {
            if (Normalize(candidate.ToString()) == normalized || Normalize(GetDisplayName(candidate)) == normalized)
            {
                function = candidate;
                return true;
            }
        }

        function = OrbatEquipmentFunction.Unspecified;
        return false;
    }

    private static string SplitPascalCase(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0
                && char.IsUpper(current)
                && !char.IsWhiteSpace(value[index - 1])
                && (!char.IsUpper(value[index - 1]) || index + 1 < value.Length && char.IsLower(value[index + 1])))
                builder.Append(' ');
            builder.Append(current);
        }

        return builder.ToString();
    }

    private static string Normalize(string value)
    {
        return new string((value ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}