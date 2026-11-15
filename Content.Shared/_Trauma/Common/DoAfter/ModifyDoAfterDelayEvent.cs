namespace Content.Shared._Trauma.Common.DoAfter;

/// <summary>
/// Raised on the target, relayed to bodyparts and bone entities in hand organ entities.
/// </summary>
[ByRefEvent]
public record struct ModifyDoAfterDelayEvent(float Multiplier = 1f)
{
    public const float MaxSlowdown = 10f;

    private float? _slowdown;
    private float? _speedup;

    public void Scale(float factor)
    {
        if (factor < 0f)
            return;

        if (factor >= 1f)
            _slowdown = Math.Min((_slowdown ?? 1f) * factor, MaxSlowdown);
        else
            _speedup = (_speedup ?? 1f) * factor;

        Multiplier = (_slowdown ?? 1f) * (_speedup ?? 1f);
    }
}
