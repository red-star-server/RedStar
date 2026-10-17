using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Server.NPC.Components;
using Content.Shared.Actions;
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

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

public sealed partial class SlimeDigestionSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SatiationSystem _satiation = default!;

    [Dependency] private EntityQuery<SlimeDigestionComponent> _digestionQuery;
    [Dependency] private EntityQuery<SlimeComponent> _slimeQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery;
    [Dependency] private EntityQuery<InjurableComponent> _injurableQuery;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery;
    [Dependency] private EntityQuery<NPCMeleeCombatComponent> _meleeQuery;

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
               IsConsumableVictim(target) && _whitelist.IsWhitelistPassOrNull(ent.Comp.PreyWhitelist, target) &&
               !_doAfter.IsRunning(ent.Comp.ConsumeDoAfter) &&
               (!checkRange || _interaction.InRangeUnobstructed(ent.Owner, target, ent.Comp.ConsumeRange));
    }

    public bool IsConsumableVictim(EntityUid target)
        => !TerminatingOrDeleted(target) &&
           _mobQuery.TryComp(target, out var state) && state.CurrentState is MobState.Critical or MobState.Dead &&
           !_slimeQuery.HasComp(target) && _damageableQuery.HasComp(target) &&
           _injurableQuery.TryComp(target, out var injurable) && injurable.DamageContainer == "Biological";

    public bool TryConsume(Entity<SlimeDigestionComponent?> ent, EntityUid target)
    {
        if (!_digestionQuery.Resolve(ent, ref ent.Comp, false) || !CanConsume(ent, target))
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

        if (_damageable.TryChangeDamage(target, ent.Comp.DigestDamage, out var actual, ignoreResistances: true, origin: ent.Owner) &&
            actual.AnyPositive() && TryComp<SatiationComponent>(ent.Owner, out var satiation))
            _satiation.ModifyValue((ent.Owner, satiation), SatiationSystem.Hunger, ent.Comp.NutritionPerTick.Float());

        if (_mobQuery.TryComp(target, out var state) && state.CurrentState == MobState.Dead &&
            _damageableQuery.TryComp(target, out var damageable) &&
            _damageable.GetPositiveDamage((target, damageable)).DamageDict.GetValueOrDefault("Cellular") >= ent.Comp.FullyDigestedCellularDamage)
            _gibbing.Gib(target);

        args.Handled = true;
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

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<SlimeDigestionComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            _doAfter.Cancel(ent.Comp.ConsumeDoAfter);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SlimeDigestionComponent> ent, ref ComponentShutdown args)
    {
        _doAfter.Cancel(ent.Comp.ConsumeDoAfter);
        _actions.RemoveAction(ent.Comp.ConsumeActionEntity);
    }
}
