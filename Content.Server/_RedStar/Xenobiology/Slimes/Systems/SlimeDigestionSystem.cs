using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Server.NPC.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared._RedStar.Xenobiology.Slimes;
using Content.Shared._RedStar.Xenobiology.Slimes.Events;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

/// <summary>
/// Consumes the current combat target and digests it inside a single occupant container.
/// </summary>
public sealed partial class SlimeDigestionSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<SlimeDigestionComponent> _digestionQuery;
    [Dependency] private EntityQuery<SlimeComponent> _slimeQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery;
    [Dependency] private EntityQuery<InjurableComponent> _injurableQuery;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery;
    [Dependency] private EntityQuery<NPCMeleeCombatComponent> _meleeQuery;

    [SubscribeLocalEvent]
    private void OnInit(Entity<SlimeDigestionComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Stomach = _containers.EnsureContainer<ContainerSlot>(ent.Owner, SlimeDigestionComponent.ContainerId);
        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SlimeDigestionComponent> ent, ref MapInitEvent args)
        => _actions.AddAction(ent.Owner, ref ent.Comp.ConsumeActionEntity, ent.Comp.ConsumeAction);

    [SubscribeLocalEvent]
    private void OnConsumeAction(Entity<SlimeDigestionComponent> ent, ref SlimeConsumeActionEvent args)
    {
        if (!args.Handled)
            args.Handled = TryConsume(ent.AsNullable(), args.Target);
    }

    public bool CanConsume(Entity<SlimeDigestionComponent?> ent, EntityUid target, bool checkRange = true)
    {
        return _digestionQuery.Resolve(ent, ref ent.Comp, false) &&
               !TerminatingOrDeleted(ent.Owner) && !TerminatingOrDeleted(target) &&
               _mobQuery.TryComp(ent.Owner, out var slimeState) && slimeState.CurrentState == MobState.Alive &&
               IsConsumableVictim(target) && _whitelist.IsWhitelistPass(ent.Comp.PreyWhitelist, target) &&
               ent.Comp.Stomach.ContainedEntity == null &&
               _blocker.CanInteract(ent.Owner, target) &&
               (!checkRange || _interaction.InRangeUnobstructed(ent.Owner, target, range: ent.Comp.ConsumeRange)) &&
               _containers.CanInsert(target, ent.Comp.Stomach);
    }

    public bool IsConsumableVictim(EntityUid target)
    {
        return !TerminatingOrDeleted(target) &&
               _mobQuery.TryComp(target, out var state) && state.CurrentState == MobState.Critical &&
               !_slimeQuery.HasComp(target) && _damageableQuery.HasComp(target) &&
               _injurableQuery.TryComp(target, out var injurable) && injurable.DamageContainer == "Biological" &&
               !_containers.IsEntityInContainer(target);
    }

    public bool TryConsume(Entity<SlimeDigestionComponent?> ent, EntityUid target)
    {
        if (!_digestionQuery.Resolve(ent, ref ent.Comp, false) || _doAfter.IsRunning(ent.Comp.ConsumeDoAfter) ||
            !CanConsume(ent, target))
            return false;

        var args = new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.ConsumeDelay,
            new SlimeConsumeDoAfterEvent(), ent.Owner, target: target)
        {
            NeedHand = false,
            BreakOnMove = true,
            DistanceThreshold = ent.Comp.ConsumeRange,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = false
        };
        return _doAfter.TryStartDoAfter(args, out ent.Comp.ConsumeDoAfter);
    }

    [SubscribeLocalEvent]
    private void OnAttempt(Entity<SlimeDigestionComponent> ent, ref DoAfterAttemptEvent<SlimeConsumeDoAfterEvent> args)
    {
        if (args.Event.Target is not { } target || !CanConsume(ent.AsNullable(), target))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnConsumed(Entity<SlimeDigestionComponent> ent, ref SlimeConsumeDoAfterEvent args)
    {
        ent.Comp.ConsumeDoAfter = null;
        if (args.Cancelled || args.Handled || args.Target is not { } target || !CanConsume(ent.AsNullable(), target))
            return;

        if (!_containers.Insert(target, ent.Comp.Stomach))
            return;

        args.Handled = true;
        ent.Comp.NextDigestTime = _timing.CurTime + ent.Comp.DigestInterval;
        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnAttack(Entity<SlimeDigestionComponent> ent, ref AttackAttemptEvent args)
    {
        if (_doAfter.IsRunning(ent.Comp.ConsumeDoAfter) ||
            _meleeQuery.HasComp(ent.Owner) && args.Target is { } target && CanConsume(ent.AsNullable(), target))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnVerb(Entity<MobStateComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !CanConsume(args.User, ent.Owner))
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("slime-consume-verb"),
            Act = () => TryConsume(user, ent.Owner)
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SlimeDigestionComponent>();
        while (query.MoveNext(out var uid, out var digestion))
        {
            if (digestion.Stomach.ContainedEntity is not { } victim)
                continue;

            if (!_mobQuery.TryComp(uid, out var slimeState) || slimeState.CurrentState != MobState.Alive ||
                TerminatingOrDeleted(victim) || !_mobQuery.TryComp(victim, out var victimState) ||
                victimState.CurrentState is not (MobState.Critical or MobState.Dead) || !_damageableQuery.HasComp(victim) ||
                !_injurableQuery.TryComp(victim, out var injurable) || injurable.DamageContainer != "Biological")
            {
                Release((uid, digestion));
                continue;
            }

            if (digestion.NextDigestTime > _timing.CurTime)
                continue;

            digestion.NextDigestTime = _timing.CurTime + digestion.DigestInterval;
            if (_damageable.TryChangeDamage(victim, digestion.DigestDamage, out var actual, ignoreResistances: true, origin: uid) &&
                actual.AnyPositive() && TryComp<SatiationComponent>(uid, out var satiation))
                _satiation.ModifyValue((uid, satiation), SatiationSystem.Hunger, digestion.NutritionPerTick.Float());

            if (victimState.CurrentState != MobState.Dead ||
                _damageable.GetPositiveDamage((victim, _damageableQuery.Comp(victim))).DamageDict
                    .GetValueOrDefault("Cellular") <
                digestion.FullyDigestedCellularDamage)
                continue;

            Release((uid, digestion));
            _gibbing.Gib(victim);
        }
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<SlimeDigestionComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            Release(ent);
    }

    [SubscribeLocalEvent]
    private void OnTerminating(Entity<SlimeDigestionComponent> ent, ref EntityTerminatingEvent args)
        => Release(ent);

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SlimeDigestionComponent> ent, ref ComponentShutdown args)
    {
        Release(ent);
        _actions.RemoveAction(ent.Comp.ConsumeActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<SlimeDigestionComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container == ent.Comp.Stomach)
            Release(ent);
    }

    /// <summary>
    /// Ejects the victim before container shutdown can delete it alongside the slime.
    /// </summary>
    public void Release(Entity<SlimeDigestionComponent> ent)
    {
        _doAfter.Cancel(ent.Comp.ConsumeDoAfter);
        ent.Comp.ConsumeDoAfter = null;
        if (ent.Comp.Stomach?.ContainedEntity is { } victim)
            _containers.Remove(victim, ent.Comp.Stomach, force: true);

        ent.Comp.NextDigestTime = TimeSpan.Zero;
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<SlimeDigestionComponent> ent)
    {
        if (!TerminatingOrDeleted(ent.Owner))
            _appearance.SetData(ent.Owner, SlimeVisuals.Digesting, ent.Comp.Stomach?.ContainedEntity != null);
    }
}
