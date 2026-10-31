using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared._Trauma.Damage;

public sealed partial class PartDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;


    [SubscribeLocalEvent(after: new[] { typeof(InitialBodySystem) })]
    private void OnMapInit(Entity<DamageableComponent> ent, ref MapInitEvent args)
    {
        // update e.g. unidentified corpse part damage when they spawn
        _damage.ApplyInitialBodyDamage(ent);
    }

    [SubscribeLocalEvent(after: new[] { typeof(DamageableSystem) })]
    private void OnDamageDealt(Entity<OrganComponent> ent, ref DamageDealtEvent args)
    {
        if (ent.Comp.Body is not { } body)
            return;

        _damage.UpdateParentDamageFromBodyParts(
            body,
            args.Damage,
            args.InterruptsDoAfters,
            args.Origin);
    }
}
