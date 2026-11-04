using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Common.EntityEffects;

/// <summary>
/// Scales the efficiency of an effect based on the temperature of the entity.
/// </summary>
/// <param name="Min">The minimum temperature to scale the effect.</param>
/// <param name="Max">The maximum temperature to scale the effect.</param>
/// <param name="Scale">The scale to use for the efficiency.</param>
[DataRecord, Serializable]
public partial record struct TemperatureScaling(FixedPoint2 Min, FixedPoint2 Max, FixedPoint2 Scale)
{
    /// <summary>
    /// Converts temperature scaling to its serialized tuple values.
    /// </summary>
    public static implicit operator (FixedPoint2, FixedPoint2, FixedPoint2)(TemperatureScaling p) => (p.Min, p.Max, p.Scale);

    /// <summary>
    /// Creates temperature scaling from its serialized tuple values.
    /// </summary>
    public static implicit operator TemperatureScaling((FixedPoint2, FixedPoint2, FixedPoint2) p) => new(p.Item1, p.Item2, p.Item3);

    /// <summary>
    /// Calculates the efficiency multiplier based on the given temperature.
    /// </summary>
    /// <param name="temperature">The temperature to calculate efficiency for.</param>
    /// <param name="invert"> If true, efficiency increases with temperature. If false, efficiency decreases with temperature.</param>
    /// <returns>The calculated efficiency multiplier.</returns>

    public FixedPoint2 GetEfficiencyMultiplier(FixedPoint2 temperature, bool invert = false)
    {
        if (Min > Max) // If the minimum is greater than the max, swap them to prevent issues.
            (Min, Max) = (Max, Min);

        if (Min == Max)
            return FixedPoint2.New(1); // If the min is equal to the max, return one or full efficiency since the range is meaningless.

        // Clamp the temperature within a given range.
        temperature = FixedPoint2.Clamp(temperature, Min, Max);

        // Calculate the distance from the minimum.
        var distance = FixedPoint2.Abs(temperature - Min);
        // Calculate the full possible temperature range between min and max.
        var totalRange = Max - Min;

        // Calculate scaled distance
        var scaledDistance = distance / totalRange;

        // Calculate final efficiency based on the inversion flag:
        // If inverted, efficiency increases with temperature (1 + scaled distance)
        // If not inverted, efficiency decreases with temperature (1 - scaled distance)
        // Then apply the scale factor to the result
        return invert
            ? FixedPoint2.New(1) + (scaledDistance * Scale)
            : (FixedPoint2.New(1) - scaledDistance) * Scale;
    }
}
