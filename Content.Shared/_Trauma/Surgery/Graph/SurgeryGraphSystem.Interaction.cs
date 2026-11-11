using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.CCVar;
using Content.Shared._Trauma.Surgery;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

public sealed partial class SurgeryGraphSystem
{
    private const int MaxHintedEdges = 5;

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityQuery<SurgeryTargetComponent> _targetQuery = default!;

    private static readonly ProtoId<DamageTypePrototype> Poison = "Poison";
    private static readonly FixedPoint2 SepsisDamageLimit = FixedPoint2.New(90);

    private bool _noSelfOperate;
    private readonly List<SurgeryGraphPrototype> _graphs = [];

    private void InitializeInteraction()
    {
        Subs.CVar(_cfg, SurgeryCVars.CanOperateOnSelf, x => _noSelfOperate = !x, true);

        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnBodyMapInit);
        SubscribeLocalEvent<SurgeryTargetComponent, InteractUsingEvent>(OnBodyInteractUsing);
        SubscribeLocalEvent<BodyPartComponent, InteractUsingEvent>(OnPartInteractUsing);
        SubscribeLocalEvent<OrganComponent, InteractUsingEvent>(OnOrganInteractUsing);
        SubscribeLocalEvent<SurgeryTargetComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<HandsComponent, SurgerySanitizationEvent>(RelaySanitization);
        SubscribeLocalEvent<SanitizedComponent, HeldRelayedEvent<SurgerySanitizationEvent>>(OnHeldSanitization);
        SubscribeLocalEvent<SanitizedComponent, SurgerySanitizationEvent>(OnSanitization);

        LoadGraphs();
    }

    private void OnBodyMapInit(Entity<BodyComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<SurgeryTargetComponent>(ent);
        // Raise the event for organ-less bodies; initial bodies are handled by InitialBodySystem.
        if (HasComp<InitialBodyComponent>(ent))
            return;

        var ev = new BodyInitEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<SurgeryGraphPrototype>())
            LoadGraphs();
    }

    private void LoadGraphs()
    {
        _graphs.Clear();
        foreach (var graph in _proto.EnumeratePrototypes<SurgeryGraphPrototype>())
        {
            _graphs.Add(graph);
        }
    }

    #region Interaction

    private void OnBodyInteractUsing(Entity<SurgeryTargetComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.Used != _hands.GetActiveItemOrSelf(args.User))
            return;

        var part = _part.FindBodyPart(ent.Owner, BodyPartType.Torso)?.Owner ?? ent.Owner;
        if (TryOperate(ent.Owner, part, args.User, args.Used))
            args.Handled = true;
    }

    private void OnPartInteractUsing(Entity<BodyPartComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.Used != _hands.GetActiveItemOrSelf(args.User))
            return;

        var body = _body.GetBody(ent.Owner) ?? ent.Owner;
        if (TryOperate(body, ent.Owner, args.User, args.Used))
            args.Handled = true;
    }

    private void OnOrganInteractUsing(Entity<OrganComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.Used != _hands.GetActiveItemOrSelf(args.User))
            return;

        if (_body.GetCategory(ent.Owner) is not { } category)
            return;

        var part = _part.GetOuterOrgan(ent.Owner);
        var body = _body.GetBody(part) ?? part;
        if (TryOperate(body, part, args.User, args.Used, category))
            args.Handled = true;
    }

    private bool TryOperate(EntityUid body, EntityUid part, EntityUid user, EntityUid used, ProtoId<OrganCategoryPrototype>? focusOrgan = null)
    {
        if (!IsLyingDown(body, user, true))
            return false;

        if (_noSelfOperate && user == body)
        {
            _popup.PopupEntity(Loc.GetString("surgery-error-self-surgery"), user, user);
            return true;
        }

        if (TryComp<OperatedComponent>(part, out var operated) && operated.GraphId != null)
            return TryPerformStep(part, user, used, focusOrgan);

        SurgeryGraphPrototype? match = null;
        foreach (var graph in _graphs)
        {
            if (GetNode(graph, graph.StartNode) is not { } start)
                continue;

            foreach (var edge in GetAllEdges(start))
            {
                if (edge.Steps.Count == 0 || !EdgeConditionsMet(part, edge) || !TryMatchTool(edge.Steps[0], used))
                    continue;

                if (match != null)
                {
                    _popup.PopupEntity(Loc.GetString("surgery-graph-ambiguous"), part, user, PopupType.MediumCaution);
                    return true;
                }

                match = graph;
                break;
            }
        }

        if (match == null || !TryAssignGraph(part, match.ID))
            return false;

        return TryPerformStep(part, user, used, focusOrgan);
    }

    private void OnExamined(Entity<SurgeryTargetComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !IsLyingDown(ent, args.Examiner, false))
            return;

        var part = _part.FindBodyPart(ent.Owner, BodyPartType.Torso)?.Owner ?? ent.Owner;
        if (!TryComp<OperatedComponent>(part, out var operated)
            || !TryGetGraph(operated, out var graph)
            || GetNode(graph, operated.CurrentNode) is not { } node)
            return;

        var labels = new List<string>();
        foreach (var edge in GetAllEdges(node))
        {
            if (!EdgeConditionsMet(part, edge))
                continue;

            var label = edge.Label is { Length: > 0 } text ? text : edge.Id;
            if (!labels.Contains(label))
                labels.Add(label);

            if (labels.Count >= MaxHintedEdges)
                break;
        }

        if (labels.Count == 0)
            return;

        args.PushMarkup(Loc.GetString("surgery-world-target-hint",
            ("part", MetaData(part).EntityName),
            ("steps", string.Join(", ", labels))));
    }

    private bool IsLyingDown(EntityUid entity, EntityUid user, bool showPopup)
    {
        if (_standing.IsDown(entity))
            return true;

        // Non-humanoid bodies without buckle can still be operated on.
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

    private void RelaySanitization(Entity<HandsComponent> ent, ref SurgerySanitizationEvent args)
    {
        var ev = new HeldRelayedEvent<SurgerySanitizationEvent>(args);
        foreach (var held in _hands.EnumerateHeld(ent.AsNullable()))
        {
            RaiseLocalEvent(held, ref ev);
        }

        args = ev.Args;
    }

    private void OnHeldSanitization(Entity<SanitizedComponent> ent, ref HeldRelayedEvent<SurgerySanitizationEvent> args)
    {
        if (ent.Comp.WorksInHands)
            args.Args.Handled = true;
    }

    private void OnSanitization(Entity<SanitizedComponent> ent, ref SurgerySanitizationEvent args)
    {
        args.Handled = true;
    }

    #endregion
}
