
namespace Content.Shared._Trauma.Augments;

public sealed partial class AugmentRelaySystem : EntitySystem
{
    [Dependency] private AugmentSystem _augment = default!;

    [SubscribeLocalEvent]
    private void OnMeleeDamage(Entity<InstalledAugmentsComponent> ent, ref GetUserMeleeDamageEvent args)
        => _augment.RelayEvent(ent, ref args);
}
