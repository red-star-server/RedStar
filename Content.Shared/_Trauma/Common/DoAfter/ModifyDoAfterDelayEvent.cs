namespace Content.Shared._Trauma.Common.DoAfter;

/// <summary>
/// Raised on the target, relayed to bodyparts and bone entities in hand organ entities.
/// </summary>
[ByRefEvent]
public record struct ModifyDoAfterDelayEvent(float Multiplier = 1f)
{
    public const float MaxSlowdown = 10f;

    private float _slowdown;
    private float _speedup;

    public void Scale(float factor)
    {
        if (factor >= 1f)
            _slowdown = Math.Min((_slowdown == 0f ? 1f : _slowdown) * factor, MaxSlowdown);
        else
            _speedup = (_speedup == 0f ? 1f : _speedup) * factor;

        Multiplier = (_slowdown == 0f ? 1f : _slowdown) * (_speedup == 0f ? 1f : _speedup);
    }
}
