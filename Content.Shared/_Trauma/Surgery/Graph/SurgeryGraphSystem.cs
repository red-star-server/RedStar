using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.CCVar;
using Content.Shared._Trauma.Surgery.Tools;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Standing;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

public sealed partial class SurgeryGraphSystem : EntitySystem
{
    public static readonly ProtoId<SurgeryStagePrototype> DefaultStage = "Closed";

    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityQuery<SurgeryTargetComponent> _targetQuery;
    [Dependency] private EntityQuery<StackComponent> _stackQuery;
    [Dependency] private EntityQuery<SurgeryIgnoreClothingComponent> _ignoreClothingQuery;

    private static readonly ProtoId<DamageTypePrototype> Poison = "Poison";
    private static readonly FixedPoint2 SepsisDamageLimit = FixedPoint2.New(90);

    private readonly Dictionary<ProtoId<SurgeryStagePrototype>, List<SurgeryProcedurePrototype>> _byStage = new();

    private bool _canOperateOnSelf;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, SurgeryCVars.CanOperateOnSelf, x => _canOperateOnSelf = x, true);
        RebuildCache();
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<SurgeryProcedurePrototype>())
            RebuildCache();
    }

    private void RebuildCache()
    {
        _byStage.Clear();
        foreach (var procedure in _proto.EnumeratePrototypes<SurgeryProcedurePrototype>())
        {
            foreach (var from in procedure.From)
            {
                if (!_byStage.TryGetValue(from, out var list))
                    _byStage[from] = list = [];

                list.Add(procedure);
            }
        }
    }

    #region Queries

    public ProtoId<SurgeryStagePrototype> GetStage(EntityUid part)
    {
        return TryComp<OperatedComponent>(part, out var operated) && operated.Stage is { } stage
            ? stage
            : DefaultStage;
    }

    public bool IsOrganExposed(EntityUid part, ProtoId<OrganCategoryPrototype> category)
    {
        if (!_proto.TryIndex(category, out var categoryProto))
            return false;

        var depth = _proto.TryIndex(GetStage(part), out var stage) ? stage.Depth : 0;
        return depth >= categoryProto.SurgeryDepth;
    }

    public bool ConditionsMet(EntityUid part, SurgeryProcedurePrototype procedure, ProtoId<OrganCategoryPrototype>? organ)
    {
        foreach (var condition in procedure.Conditions)
        {
            if (!condition.CheckWithInvert(part, organ, EntityManager))
                return false;
        }

        return true;
    }

    public List<SurgeryProcedureInstance> GetAvailableProcedures(EntityUid part)
    {
        var result = new List<SurgeryProcedureInstance>();
        if (!_byStage.TryGetValue(GetStage(part), out var candidates))
            return result;

        TryComp<OperatedComponent>(part, out var operated);
        var activeProc = operated?.ActiveProcedure;
        var activeOrgan = operated?.ActiveOrgan;

        bool Available(SurgeryProcedurePrototype proc, ProtoId<OrganCategoryPrototype>? organ) =>
            activeProc is { } active
                ? active == proc.ID && activeOrgan == organ
                : ConditionsMet(part, proc, organ);

        foreach (var procedure in candidates)
        {
            switch (procedure.OrganMode)
            {
                case SurgeryOrganMode.None:
                    if (Available(procedure, null))
                        result.Add(new SurgeryProcedureInstance(procedure.ID, null));
                    break;

                case SurgeryOrganMode.Focus:
                    foreach (var category in _part.GetPartOrgans(part).Keys)
                    {
                        if (procedure.Categories.Count > 0 && !procedure.Categories.Contains(category))
                            continue;

                        if (Available(procedure, category))
                            result.Add(new SurgeryProcedureInstance(procedure.ID, category));
                    }
                    break;

                case SurgeryOrganMode.HeldItem:
                    if (!TryComp<BodyPartComponent>(part, out var partComp))
                        break;

                    if (procedure.Categories.Count > 0)
                    {
                        foreach (var category in procedure.Categories)
                        {
                            if (!partComp.Slots.Contains(category))
                                continue;

                            if (Available(procedure, category))
                                result.Add(new SurgeryProcedureInstance(procedure.ID, category));
                        }
                    }
                    else
                    {
                        foreach (var category in partComp.Slots)
                        {
                            if (Available(procedure, category))
                                result.Add(new SurgeryProcedureInstance(procedure.ID, category));
                        }
                    }
                    break;
            }
        }

        return result;
    }

    public bool MatchTool(SurgeryStepEntry step, EntityUid held, ProtoId<OrganCategoryPrototype>? instanceOrgan)
    {
        if (step.AnyItem)
            return true;

        if (step.ProcedureOrgan)
        {
            return instanceOrgan is { } procedureOrgan
                   && TryComp<OrganComponent>(held, out var heldOrgan)
                   && heldOrgan.Category == procedureOrgan;
        }

        if (step.Organ is { } organ)
        {
            return TryComp<OrganComponent>(held, out var heldOrgan)
                   && heldOrgan.Category == organ;
        }

        if (step.Quality is { } quality)
            return _tool.HasQuality(held, quality);

        return false;
    }

    #endregion

    #region Execution

    public bool CanPerformStep(EntityUid part, EntityUid user, SurgeryProcedureInstance instance, int stepIndex, out string? popup)
    {
        return ValidateStep(part, user, instance, stepIndex, out popup, out _, out _);
    }

    public bool TryPerformStep(EntityUid part, EntityUid user, SurgeryProcedureInstance instance, int stepIndex)
    {
        if (!ValidateStep(part, user, instance, stepIndex, out var popup, out var procedure, out var body))
        {
            if (popup is { } text)
                _popup.PopupEntity(text, user, user, PopupType.Medium);

            return false;
        }

        var comp = EnsureComp<OperatedComponent>(part);
        var held = _hands.GetActiveItemOrSelf(user);
        var step = procedure.Steps[stepIndex];

        var usedEv = new SurgeryUseAttemptEvent(user, body);
        RaiseLocalEvent(held, ref usedEv);
        if (usedEv.Cancelled)
            return false;

        var prevProcedure = comp.ActiveProcedure;
        var prevOrgan = comp.ActiveOrgan;
        var prevSteps = comp.CompletedSteps;

        comp.Stage ??= DefaultStage;
        comp.ActiveProcedure = instance.Procedure;
        comp.ActiveOrgan = instance.Organ;
        comp.CompletedSteps = stepIndex;
        comp.IsOperating = true;

        var ev = new SurgeryGraphStepDoAfterEvent(instance.Procedure, instance.Organ, stepIndex);
        var args = new DoAfterArgs(EntityManager, user, GetStepDuration(step, user, body, held), ev, part, target: body, used: held)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(args, out _))
        {
            comp.ActiveProcedure = prevProcedure;
            comp.ActiveOrgan = prevOrgan;
            comp.CompletedSteps = prevSteps;
            comp.IsOperating = false;
            return false;
        }

        Dirty(part, comp);

        if (step.Sound is { } sound)
            _audio.PlayPredicted(sound, part, user);
        else if (TryComp<ToolComponent>(held, out var tool) && tool.UseSound is { } toolSound)
            _audio.PlayPredicted(toolSound, held, user);

        return true;
    }

    private bool ValidateStep(EntityUid part, EntityUid user, SurgeryProcedureInstance instance, int stepIndex,
        out string? popup, out SurgeryProcedurePrototype procedure, out EntityUid body)
    {
        popup = null;
        procedure = default!;
        body = default!;

        if (!TryComp<BodyPartComponent>(part, out _))
            return false;

        body = _body.GetBody(part) ?? part;
        if (!_targetQuery.HasComp(body))
            return false;

        if (!CanOperate(body, user, out popup))
            return false;

        var held = _hands.GetActiveItemOrSelf(user);
        if (IsCovered(part, body, user, held))
        {
            popup = Loc.GetString("surgery-ui-window-steps-error-armor");
            return false;
        }

        if (!_proto.TryIndex(instance.Procedure, out var proc) ||
            stepIndex < 0 ||
            stepIndex >= proc.Steps.Count)
            return false;

        procedure = proc;

        if (!InstanceAllowed(part, proc, instance))
            return false;

        TryComp<OperatedComponent>(part, out var comp);

        var operating = comp?.IsOperating ?? false;
        var stage = comp?.Stage ?? DefaultStage;
        var activeProc = comp?.ActiveProcedure;
        var activeOrgan = comp?.ActiveOrgan;
        var completed = comp?.CompletedSteps ?? 0;

        if (operating)
            return false;

        if (!procedure.From.Contains(stage))
            return false;

        if (activeProc is { } active)
        {
            if (active != instance.Procedure || activeOrgan != instance.Organ || stepIndex != completed)
                return false;
        }
        else if (stepIndex != 0)
        {
            return false;
        }

        var continuing = activeProc == instance.Procedure && activeOrgan == instance.Organ;
        if (!continuing && !ConditionsMet(part, procedure, instance.Organ))
            return false;

        var step = procedure.Steps[stepIndex];
        if (step.RequireHeldItem && held == user)
        {
            popup = Loc.GetString("surgery-graph-requires-item");
            return false;
        }

        if (MatchTool(step, held, instance.Organ))
            return true;

        popup = step.Quality is { } quality && _proto.TryIndex(quality, out var qualityProto)
            ? Loc.GetString("surgery-graph-requires-quality", ("quality", Loc.GetString(qualityProto.Name)))
            : Loc.GetString("surgery-graph-wrong-tool");

        return false;
    }

    private bool InstanceAllowed(EntityUid part, SurgeryProcedurePrototype procedure, SurgeryProcedureInstance instance)
    {
        if (procedure.OrganMode == SurgeryOrganMode.None)
            return instance.Organ is null;

        if (instance.Organ is not { } organ)
            return false;

        if (procedure.Categories.Count > 0 && !procedure.Categories.Contains(organ))
            return false;

        if (procedure.OrganMode == SurgeryOrganMode.Focus)
            return _part.HasOrgan(part, organ);

        return TryComp<BodyPartComponent>(part, out var partComp) && partComp.Slots.Contains(organ);
    }

    [SubscribeLocalEvent]
    private void OnStepDoAfter(Entity<OperatedComponent> ent, ref SurgeryGraphStepDoAfterEvent args)
    {
        var comp = ent.Comp;
        comp.IsOperating = false;
        args.Repeat = false;

        if (args.Cancelled || args.Handled)
        {
            ResetUnstarted(ent);
            return;
        }

        args.Handled = true;
        if (comp.ActiveProcedure != args.Procedure ||
            comp.ActiveOrgan != args.Organ ||
            comp.CompletedSteps != args.StepIndex)
            return;

        if (!_proto.TryIndex(args.Procedure, out var procedure) ||
            args.StepIndex < 0 ||
            args.StepIndex >= procedure.Steps.Count)
            return;

        var surgeon = args.User;
        var used = args.Used;
        var step = procedure.Steps[args.StepIndex];
        var item = used ?? surgeon;

        if (_body.GetBody(ent) is not { } body ||
            !IsLyingDown(body) ||
            IsCovered(ent, body, surgeon, used ?? _hands.GetActiveItemOrSelf(surgeon)) ||
            step.RequireHeldItem && item == surgeon ||
            !MatchTool(step, item, args.Organ))
        {
            ResetUnstarted(ent);
            return;
        }

        for (var i = comp.AppliedStepEffects; i < step.Effects.Count; i++)
        {
            if (!step.Effects[i].Apply(ent, surgeon, used, args.Organ, EntityManager))
            {
                FailStep(ent, surgeon);
                return;
            }

            comp.AppliedStepEffects = i + 1;
        }

        if (!comp.IsFinalizing &&
            step is { Repeatable: true, RepeatWhile: { } repeatWhile } &&
            repeatWhile.CheckWithInvert(ent, args.Organ, EntityManager))
        {
            ConsumeStepItem(step, surgeon, used);
            comp.AppliedStepEffects = 0;

            args.Repeat = true;
            comp.IsOperating = true;

            HandleSanitization(ent, body, surgeon);
            Dirty(ent, comp);
            return;
        }

        if (args.StepIndex + 1 >= procedure.Steps.Count)
        {
            comp.IsFinalizing = true;

            for (var i = comp.AppliedProcedureEffects; i < procedure.Effects.Count; i++)
            {
                if (!procedure.Effects[i].Apply(ent, surgeon, used, args.Organ, EntityManager))
                {
                    FailStep(ent, surgeon);
                    return;
                }

                comp.AppliedProcedureEffects = i + 1;
            }

            ConsumeStepItem(step, surgeon, used);
            HandleSanitization(ent, body, surgeon);

            comp.Stage = procedure.To ?? comp.Stage;
            comp.ActiveProcedure = null;
            comp.ActiveOrgan = null;
            comp.CompletedSteps = 0;
        }
        else
        {
            ConsumeStepItem(step, surgeon, used);
            HandleSanitization(ent, body, surgeon);

            comp.CompletedSteps = args.StepIndex + 1;
        }

        comp.AppliedStepEffects = 0;
        comp.AppliedProcedureEffects = 0;
        comp.IsFinalizing = false;

        Dirty(ent, comp);
    }

    private void ResetUnstarted(Entity<OperatedComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.CompletedSteps != 0 || comp.AppliedStepEffects != 0 || comp.ActiveProcedure == null)
            return;

        comp.ActiveProcedure = null;
        comp.ActiveOrgan = null;
        Dirty(ent);
    }

    private void FailStep(Entity<OperatedComponent> ent, EntityUid surgeon)
    {
        ResetUnstarted(ent);
        _popup.PopupEntity(Loc.GetString("surgery-graph-step-failed"), surgeon, surgeon, PopupType.Medium);
    }

    private void ConsumeStepItem(SurgeryStepEntry step, EntityUid surgeon, EntityUid? used)
    {
        if (!step.Consume ||
            used is not { } consumed ||
            TerminatingOrDeleted(consumed) ||
            !_hands.IsHolding(surgeon, consumed))
            return;

        if (_stackQuery.TryComp(consumed, out var stack))
            _stack.TryUse((consumed, stack), 1);
        else
            QueueDel(consumed);
    }

    private TimeSpan GetStepDuration(SurgeryStepEntry step, EntityUid surgeon, EntityUid body, EntityUid held)
    {
        var speed = 1f;
        if (TryComp<ToolComponent>(held, out var tool))
            speed *= tool.SpeedModifier;

        if (TryComp<BuckleComponent>(body, out var buckle) &&
            TryComp<OperatingTableComponent>(buckle.BuckledTo, out var table))
            speed *= table.SpeedModifier;

        if (TryComp<SurgerySpeedModifierComponent>(surgeon, out var surgeonSpeed))
            speed *= surgeonSpeed.SpeedModifier;

        return step.Duration / MathF.Max(speed, 0.01f);
    }

    private bool IsCovered(EntityUid part, EntityUid body, EntityUid user, EntityUid held)
    {
        if (!TryComp<BodyPartComponent>(part, out var partComp))
            return false;

        if (_ignoreClothingQuery.HasComp(user) || _ignoreClothingQuery.HasComp(held))
            return false;

        var slots = BodyEquipmentSystem.GetCoveringSlots(partComp.PartType);
        if (slots == SlotFlags.NONE || !_inventory.TryGetContainerSlotEnumerator(body, out var enumerator, slots))
            return false;

        while (enumerator.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } item && !_ignoreClothingQuery.HasComp(item))
                return true;
        }

        return false;
    }

    public bool CanOperate(EntityUid body, EntityUid user, out string? error)
    {
        error = null;
        if (!IsLyingDown(body))
        {
            error = Loc.GetString("surgery-error-laying");
            return false;
        }

        if (_canOperateOnSelf || user != body)
            return true;

        error = Loc.GetString("surgery-error-self-surgery");
        return false;
    }

    public bool IsLyingDown(EntityUid entity)
    {
        if (_standing.IsDown(entity))
            return true;

        if (!TryComp<BuckleComponent>(entity, out var buckle))
            return true;

        if (!TryComp<StrapComponent>(buckle.BuckledTo, out var strap))
            return false;

        var rotation = strap.Rotation;
        return rotation.GetCardinalDir() is Direction.West or Direction.East;
    }

    #endregion

    #region Sanitization

    private void HandleSanitization(EntityUid part, EntityUid body, EntityUid user)
    {
        if (_inventory.TryGetSlotEntity(user, "gloves", out _) && _inventory.TryGetSlotEntity(user, "mask", out _))
            return;

        var sepsisEv = new SurgerySanitizationEvent();
        RaiseLocalEvent(user, ref sepsisEv);
        if (sepsisEv.Handled)
            return;

        if (_targetQuery.TryComp(body, out var target) && target.SepsisImmune)
            return;

        if (_damage.GetDamageAmount(part, Poison) >= SepsisDamageLimit)
            return;

        var sepsis = new DamageSpecifier(_proto.Index(Poison), 5);
        _damage.ChangeDamage(part, sepsis, true, origin: user, ignoreBlockers: true);
    }

    #endregion
}

[Serializable]
public readonly record struct SurgeryProcedureInstance(ProtoId<SurgeryProcedurePrototype> Procedure, ProtoId<OrganCategoryPrototype>? Organ);
