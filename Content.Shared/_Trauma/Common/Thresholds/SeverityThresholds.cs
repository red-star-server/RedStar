using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Common.Thresholds;

public static class SeverityThresholds
{
    /// <summary>
    /// Picks the severity with the highest threshold that is not above <paramref name="value"/>.
    /// If the value is below every threshold the severity with the lowest threshold is used,
    /// and if there are no thresholds at all <paramref name="fallback"/> is returned.
    /// </summary>
    public static T Nearest<T>(IReadOnlyDictionary<T, FixedPoint2> thresholds, FixedPoint2 value, T fallback)
        where T : notnull
    {
        var found = false;
        var nearest = fallback;
        var nearestThreshold = FixedPoint2.Zero;
        var lowest = fallback;
        var lowestThreshold = FixedPoint2.Zero;
        var any = false;

        foreach (var (severity, threshold) in thresholds)
        {
            if (!any || threshold < lowestThreshold)
            {
                lowest = severity;
                lowestThreshold = threshold;
                any = true;
            }

            if (value < threshold || found && nearestThreshold >= threshold)
                continue;

            nearest = severity;
            nearestThreshold = threshold;
            found = true;
        }

        return found ? nearest : lowest;
    }
}
