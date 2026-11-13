using System.Numerics;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.CCVar;
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
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
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
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<SurgeryTargetComponent> _targetQuery;

    private static readonly ProtoId<TagPrototype> SurgeryToolTag = "SurgeryTool";
    private static readonly ProtoId<DamageTypePrototype> Poison = "Poison";
    private static readonly FixedPoint2 SepsisDamageLimit = FixedPoint2.New(90);

    private bool _noSelfOperate;
    private readonly List<SurgeryGraphPrototype> _graphs = [];

    private void InitializeInteraction()
    {
        Subs.CVar(_cfg, SurgeryCVars.CanOperateOnSelf, x => _noSelfOperate = !x, true);

        LoadGraphs();
    }

    [SubscribeLocalEvent]
    private void OnBodyMapInit(Entity<BodyComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<SurgeryTargetComponent>(ent);
        // Raise the event for organ-less bodies; initial bodies are handled by InitialBodySystem.
        if (HasComp<InitialBodyComponent>(ent))
            return;

        var ev = new BodyInitEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnBodyInteractUsing(Entity<SurgeryTargetComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.Used != _hands.GetActiveItemOrSelf(args.User))
            return;

        var part = ResolveClickedPart(ent.Owner, args.ClickLocation);
        if (TryOperate(ent.Owner, part, args.User, args.Used))
            args.Handled = true;
    }

    private EntityUid ResolveClickedPart(EntityUid body, EntityCoordinates clickLocation)
    {
        var torso = _part.FindBodyPart(body, BodyPartType.Torso)?.Owner ?? body;

        var xform = Transform(body);
        var offset = _transform.ToMapCoordinates(clickLocation).Position - _transform.GetWorldPosition(xform);
        var local = (-xform.LocalRotation).RotateVec(offset);

        var type = ClassifyRegion(local);
        if (type == BodyPartType.Torso)
            return torso;

        var symmetry = local.X < 0 ? BodyPartSymmetry.Left : BodyPartSymmetry.Right;
        return _part.FindBodyPart(body, type, symmetry)?.Owner ?? torso;
    }

    private static BodyPartType ClassifyRegion(Vector2 local)
    {
        if (local.Y > 0.55f)
            return BodyPartType.Head;

        if (local.Y < -0.35f)
            return local.Y < -0.85f ? BodyPartType.Foot : BodyPartType.Leg;

        if (MathF.Abs(local.X) > 0.35f)
            return MathF.Abs(local.X) > 0.7f ? BodyPartType.Hand : BodyPartType.Arm;

        return BodyPartType.Torso;
    }

    [SubscribeLocalEvent]
    private void OnPartInteractUsing(Entity<BodyPartComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.Used != _hands.GetActiveItemOrSelf(args.User))
            return;

        var body = _body.GetBody(ent.Owner) ?? ent.Owner;
        if (TryOperate(body, ent.Owner, args.User, args.Used))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnExamined(Entity<SurgeryTargetComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !IsLyingDown(ent, args.Examiner, false))
            return;

        var held = _hands.GetActiveItemOrSelf(args.Examiner);
        if (!_tag.HasTag(held, SurgeryToolTag))
            return;

        foreach (var part in _part.GetBodyParts(ent.Owner))
        {
            if (!TryComp<OperatedComponent>(part.Owner, out var operated)
                || !TryGetGraph(operated, out var graph)
                || GetNode(graph, operated.CurrentNode) is not { } node)
                continue;

            var labels = new List<string>();
            foreach (var edge in GetAllEdges(node))
            {
                if (!EdgeConditionsMet(part, edge))
                    continue;

                var label = edge.Label is { Length: > 0 } text
                    ? Loc.TryGetString(text, out var localized) ? localized : text
                    : edge.Id;
                if (!labels.Contains(label))
                    labels.Add(label);

                if (labels.Count >= MaxHintedEdges)
                    break;
            }

            if (labels.Count == 0)
                continue;

            args.PushMarkup(Loc.GetString("surgery-world-target-hint",
                ("part", MetaData(part).EntityName),
                ("steps", string.Join(", ", labels))));
        }
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

    [SubscribeLocalEvent]
    private void RelaySanitization(Entity<HandsComponent> ent, ref SurgerySanitizationEvent args)
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
}
