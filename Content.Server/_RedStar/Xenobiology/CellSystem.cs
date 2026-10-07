using System.Linq;
using Content.Shared.Body.Components;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Dataset;
using Content.Shared.Random.Helpers;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Traits;
using Content.Shared._RedStar.Xenobiology.Components.Tools;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Server-side cell system that handles trait detection, random cell generation,
/// and collector tool interactions (biopsy/transfer).
/// </summary>
public sealed partial class CellSystem : SharedCellSystem
{
    private static readonly ProtoId<LocalizedDatasetPrototype> CellPrefixes = "CellPrefixes";
    private static readonly ProtoId<LocalizedDatasetPrototype> CellSuffixes = "CellSuffixes";

    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnCellGenerationInit(Entity<CellGenerationComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<CellContainerComponent>(ent, out var container))
            return;

        foreach (var cellId in ent.Comp.Cells)
        {
            AddCell((ent, container), cellId);
        }
    }

    /// <summary>
    /// Starts a DoAfter for biopsy or cell transfer when interacting with a target.
    /// Determines direction based on whether the target has cells or detectable traits.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCollectorInteract(Entity<CellCollectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is null)
            return;

        TryComp<CellContainerComponent>(args.Target, out var containerComponent);
        var direction = IsBiologicalSampleSource(args.Target.Value) ||
            containerComponent is { Empty: false, AllowCollection: true }
            ? CellCollectorDirection.Collection
            : CellCollectorDirection.Transfer;

        if (!CollectorInteractValidate(ent, (args.Target.Value, containerComponent), direction, args.User))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new CellCollectorDoAfter(direction), ent, target: args.Target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    /// <summary>
    /// Completes the collector DoAfter. Biological biopsies use the organism's native sample;
    /// laboratory containers copy their contents. Transfer moves cells into a container.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCollectorCollectDoAfter(Entity<CellCollectorComponent> ent, ref CellCollectorDoAfter args)
    {
        if (args.Handled || args.Cancelled || args.Target is null)
            return;

        if (!CollectorInteractValidate(ent, args.Target.Value, args.Direction, args.Args.User))
            return;

        switch (args.Direction)
        {
            case CellCollectorDirection.Collection:
                if (IsBiologicalSampleSource(args.Target.Value))
                {
                    AddCell(ent.Owner, GetOrCreateNativeSample(args.Target.Value));
                }
                else if (TryComp<CellContainerComponent>(args.Target.Value, out var targetComp))
                {
                    CopyCells(ent.Owner, (args.Target.Value, targetComp));
                }

                _popup.PopupEntity(Loc.GetString("cell-collector-collected"), ent, args.Args.User);

                if (ent.Comp.Damage is not null)
                    _damageable.TryChangeDamage(args.Target.Value, ent.Comp.Damage);

                break;

            case CellCollectorDirection.Transfer:
                MoveCells(ent.Owner, args.Target.Value);

                _popup.PopupEntity(Loc.GetString("cell-collector-transfer"), ent, args.Args.User);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        UpdateCollectorAppearance(ent);
        args.Handled = true;
    }

    /// <summary>
    /// Identifies an organism that can provide a stable native biopsy sample.
    /// </summary>
    private bool IsBiologicalSampleSource(EntityUid source)
    {
        return HasComp<BloodstreamComponent>(source) ||
            HasComp<CellTraitSourceComponent>(source) ||
            HasComp<NativeCellGenomeComponent>(source);
    }

    /// <summary>
    /// Creates an organism's native sample once, independently of injected cells.
    /// </summary>
    public Cell GetOrCreateNativeSample(EntityUid source)
    {
        var genome = EnsureComp<NativeCellGenomeComponent>(source);
        if (genome.Sample is { } sample)
            return sample;

        var traits = TryComp<CellTraitSourceComponent>(source, out var traitSource)
            ? traitSource.Traits.Distinct().OrderBy(id => id.ToString(), StringComparer.Ordinal).ToList()
            : [];
        return genome.Sample = GenerateRandomCell(traits);
    }

    /// <summary>
    /// Creates a new Cell with a random name, random color, and stability/cost
    /// derived from the number of traits. Higher trait count = lower stability + higher cost.
    /// </summary>
    private Cell GenerateRandomCell(List<ProtoId<CellTraitPrototype>> traits)
    {
        var prefix = _random.Pick(_prototypes.Index(CellPrefixes));
        var suffix = _random.Pick(_prototypes.Index(CellSuffixes));
        var name = prefix + suffix;

        var color = new Color(
            _random.NextFloat(),
            _random.NextFloat(),
            _random.NextFloat());

        var traitCount = traits.Count;
        var stability = MathF.Max(0.1f, 1.0f - traitCount * 0.05f + _random.NextFloat(-0.03f, 0.03f));
        var cost = GetCellCost(traitCount);

        return new Cell(
            prototypeId: null,
            color: color,
            name: name,
            stability: stability,
            cost: cost,
            traits: traits);
    }

    /// <summary>
    /// Updates the collector sprite state based on whether its cell container is empty.
    /// </summary>
    private void UpdateCollectorAppearance(Entity<CellCollectorComponent, CellContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        _appearance.SetData(ent, CellCollectorVisuals.State, ent.Comp2.Empty);
    }

    /// <summary>
    /// Validates a collector interaction. For collection: checks collector has space
    /// and target allows collection. For transfer: checks whitelist and that collector has cells.
    /// Shows popups on failure when <paramref name="popup"/> is true.
    /// </summary>
    private bool CollectorInteractValidate(Entity<CellCollectorComponent, CellContainerComponent?> ent,
        Entity<CellContainerComponent?> target,
        CellCollectorDirection direction,
        EntityUid user,
        bool popup = true)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return false;

        TryComp(target.Owner, out target.Comp);

        switch (direction)
        {
            case CellCollectorDirection.Collection:
                if (!ent.Comp2.Empty)
                {
                    if (!popup)
                        return false;

                    _popup.PopupEntity(Loc.GetString("cell-collector-full"), ent, user, PopupType.SmallCaution);
                    return false;
                }

                var biological = IsBiologicalSampleSource(target.Owner);
                if (!biological && (target.Comp is not { AllowCollection: true }))
                {
                    if (!popup)
                        return false;

                    _popup.PopupEntity(Loc.GetString("cell-collector-target-cant-collected"), ent, user, PopupType.SmallCaution);
                    return false;
                }
                break;

            case CellCollectorDirection.Transfer:
                if (target.Comp == null ||
                    _entityWhitelist.IsWhitelistFail(target.Comp.ToolsTransferWhitelist, ent) ||
                    target.Comp.ToolsTransferWhitelist is null ||
                    !target.Comp.AllowTransfer)
                    return false;

                if (ent.Comp2.Empty)
                {
                    if (!popup)
                        return false;

                    _popup.PopupEntity(Loc.GetString("cell-collector-empty"), ent, user, PopupType.SmallCaution);
                    return false;
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        return true;
    }
}
