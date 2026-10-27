using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Traumas;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Body.Cavity;

public sealed partial class InsideBodyPartSystem : CommonInsideBodyPartSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedJitteringSystem _jittering = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private TraumaSystem _trauma = default!;

    private static readonly EntProtoId BluntWound = "WoundBlunt";
    private static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";

    public override void InsertedIntoPart(EntityUid item, EntityUid part)
    {
        var comp = EnsureComp<InsideBodyPartComponent>(item);
        comp.Part = part;
        Dirty(item, comp);
        if (!HasComp<BodyComponent>(item))
            return;

        _actions.AddAction(item, ref comp.ActionEntity, comp.BurstAction);
        if (comp.ActionEntity is {} action)
            _actions.SetEntityIcon(action, part);
    }

    public override void RemovedFromPart(EntityUid item) =>
        RemComp<InsideBodyPartComponent>(item);

    [SubscribeLocalEvent]
    private void OnAction(Entity<InsideBodyPartComponent> ent, ref BodyPartBurstEvent args)
    {
        var part = ent.Comp.Part;
        var delay = ent.Comp.Delay;
        var target = part;
        if (_body.GetBody(part) is {} body)
        {
            target = body;
            // it's easier to burst out of a corpse
            if (_mob.IsAlive(body))
                delay = ent.Comp.AliveDelay;
            _jittering.DoJitter(body, delay, refresh: true);
        }

        var ev = new BurstDoAfterEvent();
        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, delay, ev, eventTarget: ent, target: part));

        var victim = Identity.Name(target, EntityManager);
        _popup.PopupEntity(Loc.GetString("body-part-burst-starting", ("victim", victim), ("part", part)), ent, ent, PopupType.LargeCaution);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<InsideBodyPartComponent> ent, ref BurstDoAfterEvent args)
    {
        var part = ent.Comp.Part;
        if (args.Cancelled || args.Target != part ||
            !_container.TryGetContainingContainer(ent.Owner, out var container) ||
            !_container.Remove(ent.Owner, container))
            return;

        // The part is itself inside the body's organ container. Leave that container before applying damage.
        _transform.AttachToGridOrMap(ent.Owner);
        _damage.TryChangeDamage(part, ent.Comp.BurstDamage, ignoreResistances: true);
        // BoneDamage is a trauma, not a wound prototype. A zero-severity wound holds its inflicter.
        if (_wound.TryCreateWound(part, BluntWound, 0, out var wound, Brute))
            _trauma.ApplyBoneTrauma(part, (wound.Value.Owner, Comp<TraumaInflicterComponent>(wound.Value.Owner)), 20);

        var target = part;
        if (_body.GetBody(part) is {} body)
        {
            target = body;
            _stun.TryUpdateParalyzeDuration(body, ent.Comp.StunTime);
            _jittering.DoJitter(body, ent.Comp.StunTime, refresh: true, frequency: 12f);
        }

        var victim = Identity.Name(target, EntityManager);
        _popup.PopupEntity(Loc.GetString("body-part-burst-finished", ("victim", victim), ("burst", ent.Owner)), ent, ent, PopupType.LargeCaution);

        // this should never happen as container events should indirectly remove it, but just incase
        RemComp(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<InsideBodyPartComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
    }
}
