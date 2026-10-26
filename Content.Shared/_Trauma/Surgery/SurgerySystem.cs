using System.Linq;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Surgery;
using Content.Shared._Trauma.Surgery.Conditions;
using Content.Shared._Trauma.Surgery.Steps;
using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared._Trauma.Traumas;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Standing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private TraumaSystem _trauma = default!;
    [Dependency] private WoundSystem _wounds = default!;
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery;
    [Dependency] private EntityQuery<StackComponent> _stackQuery;

    private CompName _surgeryName;
    private CompName _stepName;

    /// <summary>
    /// Cache of all surgery prototypes' singleton entities.
    /// Cleared after a prototype reload.
    /// </summary>
    private readonly Dictionary<EntProtoId, EntityUid> _surgeries = new();

    private readonly List<EntProtoId> _allSurgeries = new();

    /// <summary>
    /// Every surgery entity prototype id.
    /// Kept in sync with prototype reloads.
    /// </summary>
    public IReadOnlyList<EntProtoId> AllSurgeries => _allSurgeries;

    [SubscribeLocalEvent]
    private void RelaySurgerySanitizationEvent(Entity<HandsComponent> ent, ref SurgerySanitizationEvent args)
    {
        var ev = new HeldRelayedEvent<SurgerySanitizationEvent>(args);
        foreach (var held in _hands.EnumerateHeld(ent.AsNullable()))
        {
            RaiseLocalEvent(held, ref ev);
        }

        args = ev.Args;
    }

    [SubscribeLocalEvent]
    private void RelaySurgeryIgnorePreviousStepsEvent(Entity<HandsComponent> ent, ref SurgeryIgnorePreviousStepsEvent args)
    {
        var ev = new HeldRelayedEvent<SurgeryIgnorePreviousStepsEvent>(args);
        foreach (var held in _hands.EnumerateHeld(ent.AsNullable()))
        {
            RaiseLocalEvent(held, ref ev);
        }

        args = ev.Args;
    }

    public override void Initialize()
    {
        base.Initialize();

        _surgeryName = Factory.CompName<SurgeryComponent>();
        _stepName = Factory.CompName<SurgeryStepComponent>();


        InitializeStart();

        LoadPrototypes();
    }

    [SubscribeLocalEvent]
    private void OnHeldSanitization(Entity<SanitizedComponent> ent, ref HeldRelayedEvent<SurgerySanitizationEvent> args)
    {
        if (ent.Comp.WorksInHands)
            args.Args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSanitization(Entity<SanitizedComponent> ent, ref SurgerySanitizationEvent args)
    {
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _surgeries.Clear();
    }

    [SubscribeLocalEvent]
    private void OnBodyMapInit(Entity<BodyComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<SurgeryTargetComponent>(ent);
        // raise the event for organ-less bodies...
        if (HasComp<InitialBodyComponent>(ent)) return;
        var ev = new BodyInitEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SurgeryTargetComponent> ent, ref MapInitEvent args)
    {
        var data = new InterfaceData("SurgeryBui");
        _ui.SetUi(ent.Owner, SurgeryUIKey.Key, data);
    }

    [SubscribeLocalEvent]
    private void OnBeforeTargetDoAfter(Entity<SurgeryTargetComponent> ent,
        ref DoAfterAttemptEvent<SurgeryDoAfterEvent> args)
    {
        if (_net.IsClient
            || !args.Event.Repeat) // Only repeatable steps need this completion check.
            return;

        if (!IsSurgeryValid(ent, GetEntity(args.Event.Part), args.Event.Surgery, args.Event.Step, args.Event.User, out var surgery, out var part, out var _)
            || IsStepComplete(ent, part, args.Event.Step, surgery))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnTargetDoAfter(Entity<SurgeryTargetComponent> ent, ref SurgeryDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            var failEv = new SurgeryStepFailedEvent(args.User, ent, args.Surgery, args.Step);
            RaiseLocalEvent(args.User, ref failEv);
            return;
        }

        var tool = _hands.GetActiveItemOrSelf(args.User);
        if (args.Handled
            || !IsSurgeryValid(ent, GetEntity(args.Part), args.Surgery, args.Step, args.User, out var surgery, out var part, out var step)
            || !PreviousStepsComplete(ent, part, surgery, args.Step, args.User)
            || !CanPerformStep(args.User, ent, part, step, tool, false))
        {
            Log.Warning($"{ToPrettyString(args.User)} tried to start invalid surgery.");
            return;
        }

        var complete = IsStepComplete(ent, part, args.Step, surgery);
        args.Repeat = HasComp<SurgeryRepeatableStepComponent>(step) && !complete;
        var ev = new SurgeryStepEvent(args.User, ent, part, tool, surgery, step, complete);
        RaiseLocalEvent(step, ref ev);
        RaiseLocalEvent(args.User, ref ev);

        // consume the tool if it's something like using LV cable as stitches
        if (!args.ToolUsed) return;
        if (_stackQuery.TryComp(tool, out var stack))
            _stack.TryUse((tool, stack), 1);
        else
            QueueDel(tool);
    }

    [SubscribeLocalEvent]
    private void OnCloseIncisionValid(Entity<SurgeryCloseIncisionConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!HasComp<IncisionOpenComponent>(args.Part) ||
            !HasComp<BleedersClampedComponent>(args.Part) ||
            !HasComp<SkinRetractedComponent>(args.Part) ||
            !HasComp<OrganReattachedComponent>(args.Part) ||
            !HasComp<InternalBleedersClampedComponent>(args.Part))
        {
            args.Cancelled = true;
        }
    }

    [SubscribeLocalEvent]
    private void OnWoundedValid(Entity<SurgeryWoundedConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (_wounds.GetWoundableSeverityPoint(args.Part, ent.Comp.DamageGroup, healable: true) > 0) return;
        args.Cancelled = true;
        args.Reason = Loc.GetString(args.User == args.Body ? "trauma-surgery-no-wounds-self" : "trauma-surgery-no-wounds-other");
    }

    [SubscribeLocalEvent]
    private void OnBodyComponentConditionValid(Entity<SurgeryBodyComponentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        args.Cancelled |= !_whitelist.CheckBoth(args.Body, blacklist: ent.Comp.Blacklist, whitelist: ent.Comp.Whitelist);
    }

    [SubscribeLocalEvent]
    private void OnPartComponentConditionValid(Entity<SurgeryPartComponentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        var present = true;
        foreach (var reg in ent.Comp.Components.Values)
        {
            var compType = reg.Component.GetType();
            if (!HasComp(args.Part, compType))
                present = false;
        }

        args.Cancelled |= present == ent.Comp.Inverse;
    }

    // This is literally a duplicate of the checks in OnToolCheck for SurgeryStepComponent.AddOrganOnAdd
    [SubscribeLocalEvent]
    private void OnOrganOnAddConditionValid(Entity<SurgeryOrganOnAddConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (_body.GetBody(args.Part) != args.Body)
        {
            args.Cancelled = true;
            return;
        }

        // The modification itself changes this condition before the final incision-closing step.
        // Keep that final step available while the incision is still open.
        if (HasComp<IncisionOpenComponent>(args.Part)
            && GetNextStep(args.Body, args.Part, ent.Owner, args.User) is {} next
            && GetSingleton(next.Surgery.Comp.Steps[next.Step < 0 ? -next.Step - 1 : next.Step]) is {} nextStep
            && _stepQuery.Comp(nextStep).Remove?.Values.Any(entry => entry.Component is IncisionOpenComponent) == true)
            return;

        var organs = _part.GetPartOrgans(args.Part);

        var allOnAddFound = true;
        var zeroOnAddFound = true;

        foreach (var (category, components) in ent.Comp.Components)
        {
            if (!organs.TryGetValue(category, out var organ) ||
                !TryComp<OrganComponentsComponent>(organ, out var comps))
            {
                allOnAddFound = false;
                continue;
            }

            foreach (var key in components.Keys)
            {
                if (!comps.AddedKeys.Contains(key))
                    allOnAddFound = false;
                else
                    zeroOnAddFound = false;
            }
        }

        args.Cancelled |= ent.Comp.Inverse ? allOnAddFound : zeroOnAddFound;
    }

    [SubscribeLocalEvent]
    private void OnHasBodyConditionValid(Entity<SurgeryHasBodyConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (_body.GetBody(args.Part) != null) return;
        args.Cancelled = true;
        args.Reason = Loc.GetString("trauma-surgery-part-needs-body");
    }

    [SubscribeLocalEvent]
    private void OnPartConditionValid(Entity<SurgeryPartConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!TryComp<BodyPartComponent>(args.Part, out var part))
        {
            args.Cancelled = true;
            return;
        }

        var typeMatch = ent.Comp.Parts.Contains(part.PartType);
        var symmetryMatch = ent.Comp.Symmetry == null || part.Symmetry == ent.Comp.Symmetry;
        var valid = typeMatch && symmetryMatch;

        if (ent.Comp.Inverse ? valid : !valid)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnOrganConditionValid(Entity<SurgeryOrganConditionComponent> ent, ref SurgeryValidEvent args)
    {
        var category = ent.Comp.Organ;
        if (_part.GetOrgan(args.Part, category) is not {} organ)
        {
            // no organ, invalid unless condition is inverted
            args.Cancelled |= !ent.Comp.Inverse;
            return;
        }

        if (!ent.Comp.Inverse)
            return; // organ found, good to go

        // organ found but inverted, do reattaching logic
        args.Cancelled |= !ent.Comp.Reattaching || !HasComp<OrganReattachedComponent>(organ);
    }

    [SubscribeLocalEvent]
    private void OnOrganSlotConditionValid(Entity<SurgeryOrganSlotConditionComponent> ent, ref SurgeryValidEvent args)
    {
        args.Cancelled |= _part.HasOrganSlot(args.Part, ent.Comp.OrganSlot) ^ !ent.Comp.Inverse;
    }

    [SubscribeLocalEvent]
    private void OnPartRemovedConditionValid(Entity<SurgeryPartRemovedConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (_part.GetOrgan(args.Part, ent.Comp.Category) is {} attached)
            args.Cancelled |= !HasComp<OrganReattachedComponent>(attached);
        else if (!_part.CanInsertOrgan(args.Part, ent.Comp.Category))
            args.Cancelled = true;

        // TODO: Replace the reattachment marker with an explicit operation state.
    }

    [SubscribeLocalEvent]
    private void OnPartPresentConditionValid(Entity<SurgeryPartPresentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Part != EntityUid.Invalid && HasComp<BodyPartComponent>(args.Part)) return;
        args.Cancelled = true;
        args.Reason = Loc.GetString("trauma-surgery-not-a-part");
    }

    [SubscribeLocalEvent]
    private void OnTraumaPresentConditionValid(Entity<SurgeryTraumaPresentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled)
            return;

        // not inverted = cancel if no trauma present
        // inverted = cancel if trauma present
        if (_trauma.HasWoundableTrauma(args.Part, ent.Comp.TraumaType) != ent.Comp.Inverted) return;
        var name = Loc.GetString("trauma-surgery-trauma-" + ent.Comp.TraumaType);
        args.Cancelled = true;
        args.Reason = Loc.GetString(ent.Comp.Inverted ? "trauma-surgery-trauma-present" : "trauma-surgery-trauma-absent", ("trauma", name));
    }

    [SubscribeLocalEvent]
    private void OnBleedsPresentConditionValid(Entity<SurgeryBleedsPresentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!TryComp<WoundableComponent>(args.Part, out var woundable))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.Inverted != woundable.Bleeds > 0 || HasComp<BleedersClampedComponent>(args.Part)) return;
        args.Cancelled = true;
        args.Reason = (ent.Comp.Inverted, args.User == args.Body) switch
        {
            (false, false) => Loc.GetString("trauma-surgery-no-bleeding-other"),
            (false, true) => Loc.GetString("trauma-surgery-no-bleeding-self"),
            (true, false) => Loc.GetString("trauma-surgery-heavy-bleeding-other"),
            (true, true) => Loc.GetString("trauma-surgery-heavy-bleeding-self")
        };
    }

    private bool IsSurgeryValid(EntityUid body, EntityUid targetPart, EntProtoId surgery, EntProtoId stepId,
        EntityUid user, out Entity<SurgeryComponent> surgeryEnt, out EntityUid part, out EntityUid step)
    {
        surgeryEnt = default;
        part = default;
        step = default;

        if (!_targetQuery.HasComp(body) ||
            _body.GetBody(targetPart) != body ||
            !IsLyingDown(body, user) ||
            GetSingleton(surgery) is not { } surgeryEntId ||
            !TryComp(surgeryEntId, out SurgeryComponent? surgeryComp) ||
            !surgeryComp.Steps.Contains(stepId) ||
            GetSingleton(stepId) is not { } stepEnt
            || !HasComp<BodyPartComponent>(targetPart)
            && !_bodyQuery.HasComp(targetPart))
            return false;

        var ev = new SurgeryValidEvent(body, targetPart, user);
        RaiseLocalEvent(stepEnt, ref ev);
        if (!ev.Cancelled)
            RaiseLocalEvent(surgeryEntId, ref ev);

        if (ev.Cancelled)
        {
            if (ev.Reason is { } reason)
                _popup.PopupEntity(reason, user, user, PopupType.Medium);
            return false;
        }

        surgeryEnt = (surgeryEntId, surgeryComp);
        part = targetPart;
        step = stepEnt;
        return true;
    }

    public EntityUid? GetSingleton(EntProtoId surgeryOrStep)
    {
        if (!ProtoMan.TryIndex(surgeryOrStep, out var prototype)
            || !prototype.HasComp(_surgeryName) && !prototype.HasComp(_stepName))
            return null;

        // This (for now) assumes that surgery entity data remains unchanged between client
        // and server
        // if it does not you get the bullet
        if (_surgeries.TryGetValue(surgeryOrStep, out var ent) && !TerminatingOrDeleted(ent)) return ent;
        ent = Spawn(surgeryOrStep, MapCoordinates.Nullspace);
        _surgeries[surgeryOrStep] = ent;

        return ent;
    }

    /// <summary>
    /// Checks if someone is lying down (and is able to)
    /// Shows a popup if this is run on the user's client.
    /// </summary>
    public bool IsLyingDown(EntityUid entity, EntityUid user)
    {
        if (_standing.IsDown(entity))
            return true;

        // you can't otherwise operate on something with no buckle
        // Allow surgery on non-humanoid bodies, including goliaths.
        if (!TryComp<BuckleComponent>(entity, out var buckle))
            return true;

        if (TryComp<StrapComponent>(buckle.BuckledTo, out var strap))
        {
            var rotation = strap.Rotation;
            if (rotation.GetCardinalDir() is Direction.West or Direction.East)
                return true;
        }

        _popup.PopupEntity(Loc.GetString("surgery-error-laying"), user, user);
        return false;
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>())
            return;

        LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        // Cache is probably invalid so delete it
        foreach (var uid in _surgeries.Values)
        {
            Del(uid);
        }
        _surgeries.Clear();

        _allSurgeries.Clear();
        foreach (var entity in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.HasComp(_surgeryName))
                _allSurgeries.Add(new EntProtoId(entity.ID));
        }
    }
}
