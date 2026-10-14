namespace Content.Shared._RedStar.Xenobiology.Systems;

public abstract partial class SharedSampleSystem
{
    public static float GetStabilizedStability(BiologicalSample first, BiologicalSample second)
    {
        var max = Math.Max(first.Stability, second.Stability);
        var min = Math.Min(first.Stability, second.Stability);
        var gain = (1f - max) * (0.08f + 0.04f * min);
        return Math.Min(1f, max + gain);
    }

    public static Color GetStabilizedColor(BiologicalSample first, BiologicalSample second)
    {
        return Color.InterpolateBetween(first.Color, second.Color, 0.5f);
    }
}
