namespace Content.Client._CD.Records.UI;

public static class UnitConversion
{
    public static string GetImperialDisplayMass(int massKg)
    {
        var weightLbs = (int)Math.Round(massKg * 2.2046226218 /* kg to lbs */);
        return $"{weightLbs} lbs";
    }
}
