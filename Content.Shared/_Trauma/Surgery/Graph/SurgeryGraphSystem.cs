using Content.Shared._Trauma.Augments;
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

/// <summary>
/// Surgery as a graph of stages (anatomical states of a part) connected by procedures.
/// The UI picks a procedure explicitly; the engine validates and executes its steps.
/// </summary>
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

    private static readonly ProtoId<DamageTypePrototype> Poison = "Poison";
    private static readonly FixedPoint2 SepsisDamageLimit = FixedPoint2.New(90);

    private bool _noSelfOperate;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, SurgeryCVars.CanOperateOnSelf, x => _noSelfOperate = !x, true);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        ValidatePrototypes();
    }

    #region Queries

    public ProtoId<SurgeryStagePrototype> GetStage(EntityUid part)
    {
        return TryComp<OperatedComponent>(part, out var operated) && operated.Stage is { } stage
            ? stage
            : DefaultStage;
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

    /// <summary>
    /// Expands procedures available at the part's current stage into concrete instances
    /// (generic organ procedures become one instance per organ/slot) and filters by conditions.
    /// </summary>
    public List<SurgeryProcedureInstance> GetAvailableProcedures(EntityUid part)
    {
        var stage = GetStage(part);
        var result = new List<SurgeryProcedureInstance>();

        foreach (var procedure in _proto.EnumeratePrototypes<SurgeryProcedurePrototype>())
        {
            if (!procedure.From.Contains(stage))
                continue;

            switch (procedure.OrganMode)
            {
                case SurgeryOrganMode.None:
                    if (ConditionsMet(part, procedure, null))
                        result.Add(new SurgeryProcedureInstance(procedure.ID, null));
                    break;

                case SurgeryOrganMode.Focus:
                    foreach (var category in _part.GetPartOrgans(part).Keys)
                    {
                        if (procedure.Categories.Count > 0 && !procedure.Categories.Contains(category))
                            continue;

                        if (ConditionsMet(part, procedure, category))
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
                            if (ConditionsMet(part, procedure, category))
                                result.Add(new SurgeryProcedureInstance(procedure.ID, category));
                        }
                    }
                    else
                    {
                        foreach (var category in partComp.Slots)
                        {
                            if (ConditionsMet(part, procedure, category))
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
            return instanceOrgan is { } procedureOrgan && _body.GetCategory(held) == procedureOrgan;

        if (step.Organ is { } organ)
            return _body.GetCategory(held) == organ;

        if (step.Quality is { } quality)
            return _tool.HasQuality(held, quality);

        return false;
    }

    #endregion

    #region Execution

    /// <summary>
    /// Validates a procedure step without side effects (UI highlighting).
    /// </summary>
    public bool CanPerformStep(EntityUid part, EntityUid user, SurgeryProcedureInstance instance, int stepIndex, out string? popup)
    {
        return ValidateStep(part, user, instance, stepIndex, out popup, out _, out _, out _);
    }

    /// <summary>
    /// Validates and starts a procedure step's do-after.
    /// </summary>
    public bool TryPerformStep(EntityUid part, EntityUid user, SurgeryProcedureInstance instance, int stepIndex)
    {
        if (!ValidateStep(part, user, instance, stepIndex, out var popup, out var procedure, out var comp, out var body))
        {
            if (popup is { } text)
                _popup.PopupEntity(text, user, user, PopupType.Medium);
            return false;
        }

        var held = _hands.GetActiveItemOrSelf(user);
        var step = procedure.Steps[stepIndex];

        var usedEv = new SurgeryUseAttemptEvent(user, body) { IgnoreToggle = HasComp<AugmentComponent>(held) };
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

        // Range/accessibility is checked against the body, not the internal part:
        // a part inside the body container is not accessible from outside and would cancel the DoAfter.
        var ev = new SurgeryGraphStepDoAfterEvent(instance.Procedure, instance.Organ, stepIndex);
        var args = new DoAfterArgs(EntityManager, user, GetStepDuration(step, user, body, held), ev, part, target: body, used: held)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(args, out var doAfter))
        {
            comp.ActiveProcedure = prevProcedure;
            comp.ActiveOrgan = prevOrgan;
            comp.CompletedSteps = prevSteps;
            comp.IsOperating = false;
            return false;
        }

        comp.DoAfter = doAfter;
        Dirty(part, comp);

        if (step.Sound is { } sound)
            _audio.PlayPredicted(sound, part, user);
        else if (TryComp<ToolComponent>(held, out var tool) && tool.UseSound is { } toolSound)
            _audio.PlayPredicted(toolSound, held, user);

        return true;
    }

    private bool ValidateStep(EntityUid part, EntityUid user, SurgeryProcedureInstance instance, int stepIndex,
        out string? popup, out SurgeryProcedurePrototype procedure, out OperatedComponent comp, out EntityUid body)
    {
        popup = null;
        procedure = default!;
        comp = default!;
        body = default!;

        if (!TryComp<BodyPartComponent>(part, out _))
            return false;

        body = _body.GetBody(part) ?? part;
        if (!_targetQuery.HasComp(body))
            return false;

        if (!IsLyingDown(body, user, false))
        {
            popup = Loc.GetString("surgery-error-laying");
            return false;
        }

        if (_noSelfOperate && user == body)
        {
            popup = Loc.GetString("surgery-error-self-surgery");
            return false;
        }

        if (!_proto.TryIndex(instance.Procedure, out var proc) || stepIndex < 0 || stepIndex >= proc.Steps.Count)
            return false;

        procedure = proc;
        comp = EnsureComp<OperatedComponent>(part);

        if (comp.IsOperating)
            return false;

        var stage = comp.Stage ?? DefaultStage;
        if (!procedure.From.Contains(stage))
            return false;

        // Continuing the active procedure requires the exact next step; switching procedures starts over.
        if (comp.ActiveProcedure is { } active)
        {
            if (active == instance.Procedure && comp.ActiveOrgan == instance.Organ)
            {
                if (stepIndex != comp.CompletedSteps)
                    return false;
            }
            else if (stepIndex != 0)
            {
                return false;
            }
        }
        else if (stepIndex != 0)
        {
            return false;
        }

        if (!ConditionsMet(part, procedure, instance.Organ))
            return false;

        var held = _hands.GetActiveItemOrSelf(user);
        if (!MatchTool(procedure.Steps[stepIndex], held, instance.Organ))
        {
            popup = Loc.GetString("surgery-graph-wrong-tool");
            return false;
        }

        return true;
    }

    [SubscribeLocalEvent]
    private void OnStepDoAfter(Entity<OperatedComponent> ent, ref SurgeryGraphStepDoAfterEvent args)
    {
        var comp = ent.Comp;
        comp.IsOperating = false;
        comp.DoAfter = null;
        // Any bail-out below must also stop a running repeat loop.
        args.Repeat = false;

        if (args.Cancelled || args.Handled)
            return;

        if (comp.ActiveProcedure != args.Procedure || comp.ActiveOrgan != args.Organ || comp.CompletedSteps != args.StepIndex)
            return;

        if (!_proto.TryIndex(args.Procedure, out var procedure) || args.StepIndex >= procedure.Steps.Count)
            return;

        var body = _body.GetBody(ent) ?? ent;
        var surgeon = args.User;
        var used = args.Used;

        if (!ConditionsMet(ent, procedure, args.Organ))
            return;

        // The patient may have stood up during the DoAfter; abort before mutating anything.
        if (!IsLyingDown(body, surgeon, false))
            return;

        var step = procedure.Steps[args.StepIndex];
        if (used is { } item && !MatchTool(step, item, args.Organ))
            return;

        // A failed mandatory effect aborts the step without advancing the graph.
        foreach (var effect in step.Effects)
        {
            if (!effect.Apply(ent, surgeon, used, args.Organ, EntityManager))
                return;
        }

        if (step.Consume && used is { } consumed && !TerminatingOrDeleted(consumed) && _hands.IsHolding(surgeon, consumed))
        {
            if (_stackQuery.TryComp(consumed, out var stack))
                _stack.TryUse((consumed, stack), 1);
            else
                QueueDel(consumed);
        }

        // Repeatable steps keep the do-after running while work remains (bleeding, trauma, wounds).
        if (step.Repeatable && step.RepeatWhile is { } repeatWhile &&
            repeatWhile.CheckWithInvert(ent, args.Organ, EntityManager))
        {
            args.Repeat = true;
            comp.IsOperating = true;
            HandleSanitization(ent, body, surgeon);
            Dirty(ent, comp);
            args.Handled = true;
            return;
        }

        comp.CompletedSteps = args.StepIndex + 1;

        HandleSanitization(ent, body, surgeon);

        if (comp.CompletedSteps >= procedure.Steps.Count)
        {
            foreach (var effect in procedure.Effects)
            {
                if (!effect.Apply(ent, surgeon, used, args.Organ, EntityManager))
                    return;
            }

            comp.Stage = procedure.To ?? comp.Stage;
            comp.ActiveProcedure = null;
            comp.ActiveOrgan = null;
            comp.CompletedSteps = 0;
        }

        Dirty(ent, comp);
        args.Handled = true;
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

        return step.Duration / speed;
    }

    private bool IsLyingDown(EntityUid entity, EntityUid user, bool showPopup)
    {
        if (_standing.IsDown(entity))
            return true;

        if (!TryComp<BuckleComponent>(entity, out var buckle))
            return true;

        if (TryComp<StrapComponent>(buckle.BuckledTo, out var strap))
        {
            var rotation = strap.Rotation;
            if (rotation.GetCardinalDir() is Direction.West or Direction.East)
                return true;
        }

        if (showPopup)
            _popup.PopupEntity(Loc.GetString("surgery-error-laying"), user, user);

        return false;
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

/// <summary>
/// A concrete procedure: the prototype plus the organ category it was expanded with.
/// </summary>
[Serializable]
public readonly record struct SurgeryProcedureInstance(ProtoId<SurgeryProcedurePrototype> Procedure, ProtoId<OrganCategoryPrototype>? Organ);