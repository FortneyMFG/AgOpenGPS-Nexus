using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Aog.Core.Legacy;

namespace Aog.Tools.LegacyConfigTranslator;

/// <summary>
/// Loads legacy AgOpenGPS V6 XML settings and projects the values required for machine profile translation.
/// </summary>
public sealed class LegacySettingsLoader
{
    private static readonly string[] SectionPositionNames =
    {
        "setSection_position1",
        "setSection_position2",
        "setSection_position3",
        "setSection_position4",
        "setSection_position5",
        "setSection_position6",
        "setSection_position7",
        "setSection_position8",
        "setSection_position9",
        "setSection_position10",
        "setSection_position11",
        "setSection_position12",
        "setSection_position13",
        "setSection_position14",
        "setSection_position15",
        "setSection_position16",
        "setSection_position17",
    };

    /// <summary>
    /// Loads the legacy settings at the specified path.
    /// </summary>
    public LegacyMachineSettings Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Settings path is required.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Legacy settings not found: {path}", path);
        }

        var document = XDocument.Load(path);
        var settings = new LegacyMachineSettings
        {
            WheelbaseMeters = ReadDouble(document, "setVehicle_wheelbase"),
            TrackWidthMeters = ReadDouble(document, "setVehicle_trackWidth"),
            AntennaHeightMeters = ReadDouble(document, "setVehicle_antennaHeight"),
            AntennaOffsetMeters = ReadDouble(document, "setVehicle_antennaOffset"),
            AntennaPivotMeters = ReadDouble(document, "setVehicle_antennaPivot"),
            MaxSteerAngleDegrees = ReadDouble(document, "setVehicle_maxSteerAngle"),
            ToolWidthMeters = ReadDouble(document, "setVehicle_toolWidth"),
            ToolOverlapMeters = ReadDouble(document, "setVehicle_toolOverlap"),
            ToolOffsetMeters = ReadDouble(document, "setVehicle_toolOffset"),
            ToolLookAheadOnMeters = ReadDouble(document, "setVehicle_toolLookAheadOn"),
            ToolLookAheadOffMeters = ReadDouble(document, "setVehicle_toolLookAheadOff"),
            ToolTrailingHitchLengthMeters = ReadDouble(document, "setTool_toolTrailingHitchLength", fallback: ReadDouble(document, "setVehicle_hitchLength")),
            TankTrailingHitchLengthMeters = ReadDouble(document, "setVehicle_tankTrailingHitchLength"),
            IsToolTrailing = ReadBool(document, "setTool_isToolTrailing"),
            IsToolRearFixed = ReadBool(document, "setTool_isToolRearFixed"),
            IsToolFront = ReadBool(document, "setTool_isToolFront"),
            IsHydraulicEnabled = ReadByte(document, "setArdMac_isHydEnabled") != 0,
            HydraulicRaiseTimeSeconds = ReadDouble(document, "setArdMac_hydRaiseTime"),
            HydraulicLowerTimeSeconds = ReadDouble(document, "setArdMac_hydLowerTime"),
            HydraulicLookAheadMeters = ReadDouble(document, "setVehicle_hydraulicLiftLookAhead"),
            SectionCount = ReadInt(document, "setVehicle_numSections"),
            DefaultSectionWidthMeters = ReadDouble(document, "setTool_defaultSectionWidth"),
            SectionOffDelaySeconds = ReadDouble(document, "setVehicle_toolOffDelay"),
            MinimumCoveragePercent = ReadInt(document, "setVehicle_minCoverage"),
            UsesSectionZones = !ReadBool(document, "setTool_isSectionsNotZones", defaultValue: true),
            SectionPositions = LoadSectionPositions(document),
        };

        return settings;
    }

    private static IReadOnlyList<decimal> LoadSectionPositions(XDocument document)
    {
        var values = new List<decimal>(SectionPositionNames.Length);
        foreach (var name in SectionPositionNames)
        {
            var value = ReadDecimal(document, name);
            values.Add(value);
        }

        return values;
    }

    private static double ReadDouble(XDocument document, string settingName, double? fallback = null)
    {
        var value = ReadSettingValue(document, settingName);
        if (value is null)
        {
            return fallback ?? 0;
        }

        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return fallback ?? 0;
    }

    private static decimal ReadDecimal(XDocument document, string settingName)
    {
        var value = ReadSettingValue(document, settingName);
        if (value is null)
        {
            return 0m;
        }

        if (decimal.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return 0m;
    }

    private static int ReadInt(XDocument document, string settingName)
    {
        var value = ReadSettingValue(document, settingName);
        if (value is null)
        {
            return 0;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return 0;
    }

    private static byte ReadByte(XDocument document, string settingName)
    {
        var value = ReadSettingValue(document, settingName);
        if (value is null)
        {
            return 0;
        }

        if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
        {
            return (byte)Math.Clamp(intResult, 0, 255);
        }

        return 0;
    }

    private static bool ReadBool(XDocument document, string settingName, bool defaultValue = false)
    {
        var value = ReadSettingValue(document, settingName);
        if (value is null)
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
        {
            return intResult != 0;
        }

        return defaultValue;
    }

    private static string? ReadSettingValue(XDocument document, string settingName)
    {
        var element = document
            .Descendants("setting")
            .FirstOrDefault(node => string.Equals(node.Attribute("name")?.Value, settingName, StringComparison.Ordinal));

        return element?.Element("value")?.Value?.Trim();
    }
}
