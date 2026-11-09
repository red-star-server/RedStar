using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.CCVar;
using Content.Shared._Trauma.Common.Targeting;
using Content.Shared._Trauma.Surgery.Conditions;
using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery;

public sealed partial class SurgerySystem
{
    private const int MaxHintedSteps = 5;

    [Dependency] private EntityQuery<SurgeryStateComponent> _stateQuery;

    private bool _worldTarget;

    private readonly record struct SurgeryAction(EntProtoId Surgery, EntProtoId Step, EntityUid StepEntity);

    private void InitializeWorldTarget()
    {
        Subs.CVar(_cfg, SurgeryCVars.WorldTarget, x => _worldTarget = x, true);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<SurgeryTargetComponent> ent, ref InteractUsingEvent args)
    {
        if (!_worldTarget || args.Handled || args.Used != _hands.GetActiveItemOrSelf(args.User) || !IsLyingDown(ent, args.User))
            return;

        if (GetTargetedPart(ent, args.User) is not { } part)
            return;

        var actions = GetActions(ent, part, args.User, true);
        if (actions.Count == 0)
            return;

        args.Handled = true;

        if (_noSelfOperate && args.User == ent.Owner)
        {
            _popup.PopupEntity(Loc.GetString("surgery-error-self-surgery"), args.User, args.User);
            return;
        }

        if (actions.Count > 1)
        {
            _popup.PopupEntity(Loc.GetString("surgery-world-target-ambiguous"), ent, args.User);
            _ui.OpenUi(ent.Owner, SurgeryUIKey.Key, args.User);
            return;
        }

        TryDoSurgeryStep(ent, part, args.User, actions[0].Surgery, actions[0].Step);
    }

    [SubscribeLocalEvent]
    private void OnWorldTargetExamined(Entity<SurgeryTargetComponent> ent, ref ExaminedEvent args)
    {
        if (!_worldTarget ||
            !args.IsInDetailsRange ||
            !IsLyingDown(ent, args.Examiner) ||
            GetTargetedPart(ent, args.Examiner) is not { } part ||
            !_stateQuery.TryComp(part, out var state) ||
            !state.CompletedSteps.HasFlag(SurgeryStepFlags.IncisionOpen))
            return;

        var steps = new List<string>();
        foreach (var action in GetActions(ent, part, args.Examiner, false))
        {
            var name = ProtoMan.Index(action.Step).Name;
            if (!steps.Contains(name))
                steps.Add(name);

            if (steps.Count >= MaxHintedSteps)
                break;
        }

        if (steps.Count == 0)
            return;

        args.PushMarkup(Loc.GetString("surgery-world-target-hint",
            ("part", MetaData(part).EntityName),
            ("steps", string.Join(", ", steps))));
    }

    private EntityUid? GetTargetedPart(EntityUid body, EntityUid user)
    {
        var ev = new GetTargetedPartEvent(body);
        RaiseLocalEvent(user, ref ev);
        return ev.Part ?? _part.FindBodyPart(body, BodyPartType.Torso)?.Owner;
    }

    private List<SurgeryAction> GetActions(EntityUid body, EntityUid part, EntityUid user, bool checkTool)
    {
        var actions = new List<SurgeryAction>();
        foreach (var surgeryId in _allSurgeries)
        {
            if (GetSingleton(surgeryId) is not { } surgery)
                continue;

            var valid = new SurgeryValidEvent(body, part, user);
            RaiseLocalEvent(surgery, ref valid);
            if (valid.Cancelled || GetNextStep(body, part, surgery, user) is not { } next)
                continue;

            var steps = next.Surgery.Comp.Steps;
            var index = next.Step >= 0 ? next.Step : -next.Step - 1;
            if (index >= steps.Count ||
                MetaData(next.Surgery.Owner).EntityPrototype?.ID is not { } nextId ||
                GetSingleton(steps[index]) is not { } stepEntity)
                continue;

            var stepValid = new SurgeryValidEvent(body, part, user);
            RaiseLocalEvent(stepEntity, ref stepValid);
            if (!stepValid.Cancelled)
                RaiseLocalEvent(next.Surgery.Owner, ref stepValid);

            if (stepValid.Cancelled ||
                checkTool && !CanPerformStepWithHeld(user, body, part, stepEntity, false, out _))
                continue;

            var action = new SurgeryAction(nextId, steps[index], stepEntity);
            if (!actions.Contains(action))
                actions.Add(action);
        }

        return actions;
    }
}
