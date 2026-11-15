using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.CCVar;
using Content.Shared._Trauma.Surgery.Graph;
using Content.Shared.Body;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Shared._Trauma.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SurgeryGraphSystem _graph = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private EntityQuery<SurgeryTargetComponent> _targetQuery;

    private bool _noSelfOperate;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, SurgeryCVars.CanOperateOnSelf, x => _noSelfOperate = !x, true);
    }

    #region Sanitization Relay

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

    #endregion

    #region Body Init

    [SubscribeLocalEvent]
    private void OnBodyMapInit(Entity<BodyComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<SurgeryTargetComponent>(ent);
        if (HasComp<InitialBodyComponent>(ent))
            return;

        var ev = new BodyInitEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SurgeryTargetComponent> ent, ref MapInitEvent args)
    {
        _ui.SetUi(ent.Owner, SurgeryUIKey.Key, new InterfaceData("SurgeryBui"));
    }

    #endregion

    #region BUI

    [SubscribeLocalEvent]
    private void OnStepChosenMessage(Entity<SurgeryTargetComponent> ent, ref SurgeryStepChosenBuiMsg args)
    {
        var part = GetEntity(args.Part);
        if (!Exists(part) || _body.GetBody(part) != ent.Owner)
            return;

        var instance = new SurgeryProcedureInstance(args.Procedure, args.Organ);
        _graph.TryPerformStep(part, args.Actor, instance, args.StepIndex);
    }

    #endregion

    private void AttemptStartSurgery(EntityUid user, EntityUid target)
    {
        if (!_graph.IsLyingDown(target, user, true))
            return;
        if (_noSelfOperate && user == target)
        {
            _popup.PopupEntity(Loc.GetString("surgery-error-self-surgery"), user, user);
            return;
        }
        _ui.OpenUi(target, SurgeryUIKey.Key, user);
    }

    [SubscribeLocalEvent]
    private void OnUtilityVerb(Entity<ToolComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !_targetQuery.HasComp(args.Target))
            return;

        var user = args.User;
        var target = args.Target;
        args.Verbs.Add(new UtilityVerb
        {
            Act = () => AttemptStartSurgery(user, target),
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Specific/Medical/Surgery/scalpel.rsi"), "scalpel"),
            Text = Loc.GetString("surgery-verb-text"),
            Message = Loc.GetString("surgery-verb-message"),
            DoContactInteraction = true
        });
    }
}
