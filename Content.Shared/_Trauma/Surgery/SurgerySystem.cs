using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Surgery.Graph;
using Content.Shared.Body;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;

namespace Content.Shared._Trauma.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SurgeryGraphSystem _graph = default!;
    [Dependency] private EntityQuery<SurgeryTargetComponent> _targetQuery;

    public override void Initialize()
    {
        base.Initialize();
        InitializeStart();
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
        var instance = new SurgeryProcedureInstance(args.Procedure, args.Organ);
        _graph.TryPerformStep(part, args.Actor, instance, args.StepIndex);
    }

    #endregion

    public bool IsLyingDown(EntityUid entity, EntityUid user, bool showPopup = true)
        => _graph.IsLyingDown(entity, user, showPopup);
}
