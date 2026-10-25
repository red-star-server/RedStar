namespace Content.Shared._Trauma.Common.DoAfter;

/// <summary>
/// Raised on the target, relayed to bodyparts and bone entities in hand organ entities.
/// </summary>
// has to be a class because BodyRelayedEvent is dogshit and cant modify the inner event
public sealed class ModifyDoAfterDelayEvent(float multiplier = 1f)
{
    public float Multiplier = multiplier;
}
