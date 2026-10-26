namespace Content.Shared._Trauma.Common.DoAfter;

/// <summary>
/// Raised on the target, relayed to bodyparts and bone entities in hand organ entities.
/// </summary>
// A reference type lets body relays share modifications to the same event.
public sealed class ModifyDoAfterDelayEvent(float multiplier = 1f)
{
    public float Multiplier = multiplier;
}
