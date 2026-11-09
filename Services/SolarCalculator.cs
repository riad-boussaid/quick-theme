using QuickTheme.Models;

namespace QuickTheme.Services;

internal static class SolarCalculator
{
    private const double ZenithDegrees = 90.833;
    private const double DegToRad = Math.PI / 180.0;

    public static SunTimes GetSunTimesUtc(DateTime dateUtc, double latitude, double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return new SunTimes(null, null);
        }

        int dayOfYear = dateUtc.DayOfYear;
        double gamma = 2.0 * Math.PI / 365.0 * (dayOfYear - 1);

        double equationOfTime = 229.18 * (
            0.000075 +
            0.001868 * Math.Cos(gamma) -
            0.032077 * Math.Sin(gamma) -
            0.014615 * Math.Cos(2 * gamma) -
            0.040849 * Math.Sin(2 * gamma));

        double declination =
            0.006918 -
            0.399912 * Math.Cos(gamma) +
            0.070257 * Math.Sin(gamma) -
            0.006758 * Math.Cos(2 * gamma) +
            0.000907 * Math.Sin(2 * gamma) -
            0.002697 * Math.Cos(3 * gamma) +
            0.00148 * Math.Sin(3 * gamma);

        double latitudeRadians = latitude * DegToRad;
        double cosHourAngle =
            Math.Cos(ZenithDegrees * DegToRad) /
            (Math.Cos(latitudeRadians) * Math.Cos(declination)) -
            Math.Tan(latitudeRadians) * Math.Tan(declination);

        if (cosHourAngle is < -1 or > 1)
        {
            // Polar day or polar night: no usable sunrise/sunset.
            return new SunTimes(null, null);
        }

        double hourAngleDegrees = Math.Acos(cosHourAngle) / DegToRad;
        double sunriseMinutes = 720.0 - 4.0 * (longitude + hourAngleDegrees) - equationOfTime;
        double sunsetMinutes = 720.0 - 4.0 * (longitude - hourAngleDegrees) - equationOfTime;

        DateTime baseUtc = dateUtc.Date;
        return new SunTimes(
            baseUtc.AddMinutes(sunriseMinutes),
            baseUtc.AddMinutes(sunsetMinutes));
    }
}
