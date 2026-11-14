using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Surgery.Graph;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Standing;

namespace Content.Shared._Trauma.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;
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
        if (!HasComp<InitialBodyComponent>(ent))
            RaiseLocalEvent(ent, new BodyInitEvent());
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
        _graph.TryPerformStep(part, args.Procedure, args.Organ, args.StepIndex, args.Actor);
    }

    #endregion

    /// <summary>
    /// Checks if someone is lying down or on an operating table.
    /// </summary>
    public bool IsLyingDown(EntityUid entity, EntityUid user, bool showPopup = true)
    {
        if (_standing.IsDown(entity))
            return true;
        if (!TryComp<BuckleComponent>(entity, out var buckle))
            return true;
        if (TryComp<StrapComponent>(buckle.BuckledTo, out var strap) &&
            strap.Rotation.GetCardinalDir() is Direction.West or Direction.East)
            return true;
        if (showPopup)
            _popup.PopupEntity(Loc.GetString("surgery-error-laying"), user, user);
        return false;
    }
}
