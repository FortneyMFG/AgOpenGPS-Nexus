using Aog.Agio.Nmea;
using Xunit;

namespace Aog.Agio.Windows.Tests;

public sealed class NmeaSentenceParserTests
{
    private readonly NmeaSentenceParser _parser = new();

    [Fact]
    public void TryParse_GgaSentence_ReturnsExpectedValues()
    {
        const string sentence = "$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47";

        var success = _parser.TryParse(sentence, out var parsed, out var error);

        Assert.True(success);
        Assert.Null(error);
        var gga = Assert.IsType<NmeaGgaSentence>(parsed);
        Assert.Equal("GP", gga.TalkerId);
        Assert.Equal(NmeaFixQuality.Gps, gga.FixQuality);
        Assert.Equal(8, gga.SatelliteCount);
        Assert.Equal(545.4, gga.AltitudeMeters);
        Assert.Equal(46.9, gga.GeoidSeparationMeters);
        Assert.Equal(48.1173, Math.Round(gga.LatitudeDegrees!.Value, 4));
        Assert.Equal(11.5167, Math.Round(gga.LongitudeDegrees!.Value, 4));
    }

    [Fact]
    public void TryParse_RmcSentence_ReturnsExpectedValues()
    {
        const string sentence = "$GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394,003.1,W*6A";

        var success = _parser.TryParse(sentence, out var parsed, out var error);

        Assert.True(success);
        Assert.Null(error);
        var rmc = Assert.IsType<NmeaRmcSentence>(parsed);
        Assert.Equal("GP", rmc.TalkerId);
        Assert.Equal(NmeaFixStatus.Active, rmc.Status);
        Assert.Equal(22.4, rmc.SpeedKnots);
        Assert.Equal(84.4, rmc.TrackTrueDegrees);
        Assert.Equal(-3.1, rmc.MagneticVariationDegrees);
        Assert.NotNull(rmc.Timestamp);
        Assert.Equal(new DateTimeOffset(1994, 3, 23, 12, 35, 19, TimeSpan.Zero), rmc.Timestamp);
    }

[Fact]
public void TryParse_RmcSentenceWithSouthLatitude_IsNegative()
{
    const string sentence = "$GPRMC,123520,A,3723.2475,S,12158.3416,W,000.0,000.0,230394,000.0,E*7F";

    var success = _parser.TryParse(sentence, out var parsed, out var error);

    Assert.True(success);
    Assert.Null(error);
    var rmc = Assert.IsType<NmeaRmcSentence>(parsed);
    Assert.True(rmc.LatitudeDegrees.HasValue);
    Assert.True(rmc.LongitudeDegrees.HasValue);
    Assert.Equal(-37.3875, Math.Round(rmc.LatitudeDegrees!.Value, 4));
    Assert.Equal(-121.9724, Math.Round(rmc.LongitudeDegrees!.Value, 4));
}

[Fact]
public void TryParse_GgaSentenceWithBlankNumericFields_AllowsNulls()
{
    const string sentence = "$GPGGA,123519,4807.038,N,01131.000,E,1,08,,,M,,M,,*5B";

    var success = _parser.TryParse(sentence, out var parsed, out var error);

    Assert.True(success);
    Assert.Null(error);
    var gga = Assert.IsType<NmeaGgaSentence>(parsed);
    Assert.Equal("GP", gga.TalkerId);
    Assert.Equal(NmeaFixQuality.Gps, gga.FixQuality);
    Assert.Equal(8, gga.SatelliteCount);
    Assert.Null(gga.HorizontalDilution);
    Assert.Null(gga.AltitudeMeters);
    Assert.Null(gga.GeoidSeparationMeters);
}


    [Fact]
    public void TryParse_VtgSentence_ReturnsExpectedValues()
    {
        const string sentence = "$GPVTG,054.7,T,034.4,M,005.5,N,010.2,K*48";

        var success = _parser.TryParse(sentence, out var parsed, out var error);

        Assert.True(success);
        Assert.Null(error);
        var vtg = Assert.IsType<NmeaVtgSentence>(parsed);
        Assert.Equal("GP", vtg.TalkerId);
        Assert.Equal(54.7, vtg.TrueCourseDegrees);
        Assert.Equal(34.4, vtg.MagneticCourseDegrees);
        Assert.Equal(5.5, vtg.SpeedKnots);
        Assert.Equal(10.2, vtg.SpeedKilometersPerHour);
    }

    [Fact]
    public void TryParse_VtgRmcSequence_WithMissingLongitudeHemisphere_Succeeds()
    {
        const string vtgSentence = "$GPVTG,360.0,T,,M,000.0,N,000.0,K*65";
        const string rmcSentence = "$GPRMC,181908,A,3723.2475,N,12158.3416,,000.0,360.0,130998,015.5,E*3A";

        var vtgSuccess = _parser.TryParse(vtgSentence, out var vtgParsed, out var vtgError);

        Assert.True(vtgSuccess);
        Assert.Null(vtgError);
        Assert.IsType<NmeaVtgSentence>(vtgParsed);

        var rmcSuccess = _parser.TryParse(rmcSentence, out var rmcParsed, out var rmcError);

        Assert.True(rmcSuccess);
        Assert.Null(rmcError);
        var rmc = Assert.IsType<NmeaRmcSentence>(rmcParsed);
        Assert.Null(rmc.LongitudeDegrees);
    }

    [Fact]
    public void TryParse_InvalidChecksum_ReturnsFalse()
    {
        const string sentence = "$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*00";

        var success = _parser.TryParse(sentence, out var parsed, out var error);

        Assert.False(success);
        Assert.Null(parsed);
        Assert.Equal("Checksum mismatch.", error);
    }

    [Fact]
    public void TryParse_UnsupportedSentence_ReturnsFalse()
    {
        const string sentence = "$GPXYZ,1,2,3*53";

        var success = _parser.TryParse(sentence, out var parsed, out var error);

        Assert.False(success);
        Assert.Null(parsed);
        Assert.Null(error);
    }

    [Fact]
    public void TryParse_GgaSentenceWithEmptyLatitudeHemisphere_TreatsAsPositive()
    {
        const string sentence = "$GPGGA,123519,4807.038,,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*09";

        var success = _parser.TryParse(sentence, out var parsed, out var error);

        Assert.True(success);
        Assert.Null(error);
        var gga = Assert.IsType<NmeaGgaSentence>(parsed);
        Assert.Equal(48.1173, Math.Round(gga.LatitudeDegrees!.Value, 4));
    }
}
