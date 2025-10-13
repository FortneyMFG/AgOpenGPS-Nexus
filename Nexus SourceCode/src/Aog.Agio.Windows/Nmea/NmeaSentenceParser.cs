using System.Globalization;

namespace Aog.Agio.Windows.Nmea;

/// <summary>
/// Parses the subset of NMEA0183 sentences required by early Nexus builds.
/// </summary>
public sealed class NmeaSentenceParser
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>
    /// Attempts to parse a raw NMEA sentence.
    /// </summary>
    public bool TryParse(string sentence, out NmeaSentence? parsed, out string? error)
    {
        parsed = null;
        error = null;

        if (string.IsNullOrWhiteSpace(sentence))
        {
            error = "Sentence is empty.";
            return false;
        }

        sentence = sentence.Trim();

        if (!sentence.StartsWith('$'))
        {
            error = "Sentence must start with '$'.";
            return false;
        }

        var asteriskIndex = sentence.IndexOf('*');
        if (asteriskIndex < 0 || asteriskIndex + 3 > sentence.Length)
        {
            error = "Sentence must include a checksum.";
            return false;
        }

        var payloadSpan = sentence.AsSpan(1, asteriskIndex - 1);
        var checksumSpan = sentence.AsSpan(asteriskIndex + 1, 2);

        if (!TryParseChecksum(checksumSpan, out var expectedChecksum))
        {
            error = "Checksum is not valid hexadecimal.";
            return false;
        }

        var computedChecksum = ComputeChecksum(payloadSpan);
        if (computedChecksum != expectedChecksum)
        {
            error = "Checksum mismatch.";
            return false;
        }

        var payload = payloadSpan.ToString();
        var fields = payload.Split(',');
        if (fields.Length == 0 || fields[0].Length < 5)
        {
            error = "Sentence header is incomplete.";
            return false;
        }

        var talkerId = fields[0][..2];
        var sentenceId = fields[0][2..];

        return sentenceId switch
        {
            "GGA" => TryParseGga(talkerId, fields, out parsed, out error),
            "RMC" => TryParseRmc(talkerId, fields, out parsed, out error),
            "VTG" => TryParseVtg(talkerId, fields, out parsed, out error),
            _ => false,
        };
    }

    private static bool TryParseGga(string talkerId, string[] fields, out NmeaSentence? parsed, out string? error)
    {
        parsed = null;
        error = null;

        if (fields.Length < 15)
        {
            error = "GGA sentence is truncated.";
            return false;
        }

        var time = TryParseTime(fields[1]);
        var latitude = TryParseLatitude(fields[2], fields[3]);
        var longitude = TryParseLongitude(fields[4], fields[5]);
        var fixQuality = TryParseFixQuality(fields[6]);
        var satellites = TryParseInt(fields[7]);
        var hdop = TryParseDouble(fields[8]);
        var altitude = TryParseDouble(fields[9]);
        var geoid = TryParseDouble(fields[11]);

        parsed = new NmeaGgaSentence(
            talkerId,
            time,
            latitude,
            longitude,
            fixQuality,
            satellites,
            hdop,
            altitude,
            geoid);

        return true;
    }

    private static bool TryParseRmc(string talkerId, string[] fields, out NmeaSentence? parsed, out string? error)
    {
        parsed = null;
        error = null;

        if (fields.Length < 12)
        {
            error = "RMC sentence is truncated.";
            return false;
        }

        var status = fields[2].Equals("A", StringComparison.OrdinalIgnoreCase) ? NmeaFixStatus.Active : NmeaFixStatus.Void;
        var time = TryParseTime(fields[1]);
        var latitude = TryParseLatitude(fields[3], fields[4]);
        var longitude = TryParseLongitude(fields[5], fields[6]);
        var speedKnots = TryParseDouble(fields[7]);
        var trackTrue = TryParseDouble(fields[8]);
        var date = TryParseDate(fields[9], time);
        var magneticVariation = TryParseSignedDouble(fields[10], fields.Length > 11 ? fields[11] : null);
        var mode = fields.Length > 12 && fields[12].Length > 0 ? fields[12][0] : (char?)null;

        parsed = new NmeaRmcSentence(
            talkerId,
            status,
            date,
            latitude,
            longitude,
            speedKnots,
            trackTrue,
            magneticVariation,
            mode);

        return true;
    }

    private static bool TryParseVtg(string talkerId, string[] fields, out NmeaSentence? parsed, out string? error)
    {
        parsed = null;
        error = null;

        if (fields.Length < 8)
        {
            error = "VTG sentence is truncated.";
            return false;
        }

        var trueCourse = TryParseDouble(fields[1]);
        var magneticCourse = TryParseDouble(fields[3]);
        var speedKnots = TryParseDouble(fields[5]);
        var speedKmh = TryParseDouble(fields[7]);
        var mode = fields.Length > 9 && fields[9].Length > 0 ? fields[9][0] : (char?)null;

        parsed = new NmeaVtgSentence(
            talkerId,
            trueCourse,
            magneticCourse,
            speedKnots,
            speedKmh,
            mode);

        return true;
    }

    private static bool TryParseChecksum(ReadOnlySpan<char> checksumSpan, out byte value)
    {
        return byte.TryParse(checksumSpan, NumberStyles.HexNumber, Invariant, out value);
    }

    private static byte ComputeChecksum(ReadOnlySpan<char> payload)
    {
        byte checksum = 0;
        foreach (var ch in payload)
        {
            checksum ^= (byte)ch;
        }

        return checksum;
    }

    private static TimeOnly? TryParseTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 6)
        {
            return null;
        }

        var hours = SafeParseInt(value[..2]);
        var minutes = SafeParseInt(value.Substring(2, 2));
        var secondsComponent = value[4..];

        if (hours is null || minutes is null)
        {
            return null;
        }

        if (!double.TryParse(secondsComponent, NumberStyles.Float, Invariant, out var secondsDouble))
        {
            return null;
        }

        var totalSeconds = (hours.Value * 60 + minutes.Value) * 60 + secondsDouble;
        var ticks = (long)Math.Round(totalSeconds * TimeSpan.TicksPerSecond);

        try
        {
            return TimeOnly.FromTimeSpan(TimeSpan.FromTicks(ticks));
        }
        catch
        {
            return null;
        }
    }

    private static DateTimeOffset? TryParseDate(string value, TimeOnly? time)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 6)
        {
            return null;
        }

        var day = SafeParseInt(value[..2]);
        var month = SafeParseInt(value.Substring(2, 2));
        var yearComponent = SafeParseInt(value.Substring(4, 2));

        if (day is null || month is null || yearComponent is null)
        {
            return null;
        }

        var year = yearComponent.Value >= 80 ? 1900 + yearComponent.Value : 2000 + yearComponent.Value;

        try
        {
            var date = new DateOnly(year, month.Value, day.Value);
            var timeComponent = time ?? TimeOnly.MinValue;
            var dateTime = date.ToDateTime(timeComponent, DateTimeKind.Unspecified);
            return new DateTimeOffset(dateTime, TimeSpan.Zero);
        }
        catch
        {
            return null;
        }
    }

    private static double? TryParseLatitude(string value, string hemisphere)
    {
        return TryParseCoordinate(value, hemisphere, 2);
    }

    private static double? TryParseLongitude(string value, string hemisphere)
    {
        return TryParseCoordinate(value, hemisphere, 3);
    }

    private static double? TryParseCoordinate(string value, string hemisphere, int degreeDigits)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < degreeDigits + 2)
        {
            return null;
        }

        if (!double.TryParse(value, NumberStyles.Float, Invariant, out var raw))
        {
            return null;
        }

        var degrees = Math.Floor(raw / 100);
        var minutes = raw - (degrees * 100);
        var decimalDegrees = degrees + (minutes / 60.0);

        if (!string.IsNullOrWhiteSpace(hemisphere) &&
            (hemisphere.Equals("S", StringComparison.OrdinalIgnoreCase) || hemisphere.Equals("W", StringComparison.OrdinalIgnoreCase)))
        {
            decimalDegrees *= -1.0;
        }

        return decimalDegrees;
    }

    private static NmeaFixQuality TryParseFixQuality(string value)
    {
        return Enum.TryParse<NmeaFixQuality>(value, out var quality) ? quality : NmeaFixQuality.Invalid;
    }

    private static int? TryParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value, NumberStyles.Integer, Invariant, out var result) ? result : null;
    }

    private static int? SafeParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, Invariant, out var result) ? result : null;
    }

    private static double? TryParseDouble(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return double.TryParse(value, NumberStyles.Float, Invariant, out var result) ? result : null;
    }

    private static double? TryParseSignedDouble(string value, string? sign)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!double.TryParse(value, NumberStyles.Float, Invariant, out var result))
        {
            return null;
        }

        if (string.Equals(sign, "W", StringComparison.OrdinalIgnoreCase) || string.Equals(sign, "S", StringComparison.OrdinalIgnoreCase))
        {
            result *= -1.0;
        }

        return result;
    }
}
