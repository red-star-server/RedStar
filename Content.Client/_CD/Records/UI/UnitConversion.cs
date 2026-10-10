namespace Content.Client._CD.Records.UI;

public static class UnitConversion
{
    /// <summary>
    /// Reference height in centimeters for a character with visual scale 1.
    /// </summary>
    private const int AverageHeightCm = 175;

    /// <summary>
    /// The input includes the species scale and the character's chosen height.
    /// </summary>
    /// <param name="scale"></param>
    /// <returns></returns>
    private static int GetMetricHeightFromScale(float scale = 1)
    {
        return (int)Math.Max(scale * AverageHeightCm, 1);
    }

    /// <summary>
    /// Formats a scaled height in metric and imperial units.
    /// </summary>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static string GetMetricAndImperialDisplayFromScale(float scale = 1)
    {
        var metricHeight = GetMetricHeightFromScale(scale);
        return $"{metricHeight}cm ({GetImperialDisplayLength(metricHeight)})";
    }

    public static string GetImperialDisplayLength(int lengthCm)
    {
        var heightIn = (int)Math.Round(lengthCm * 0.3937007874 /* cm to in*/);
        return $"{heightIn / 12}'{heightIn % 12}\"";
    }

    public static string GetImperialDisplayMass(int massKg)
    {
        var weightLbs = (int)Math.Round(massKg * 2.2046226218 /* kg to lbs */);
        return $"{weightLbs} lbs";
    }
}
