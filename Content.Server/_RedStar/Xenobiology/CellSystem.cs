using System.Globalization;
using System.Linq;
using System.Text;
using Content.Server._RedStar.Genetics;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Dataset;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Tools;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Creates laboratory samples from current genomes and handles biopsy and transfer.
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
    [Dependency] private GenomeSystem _genomes = default!;

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
    /// Starts a DoAfter for a living biopsy and handles container transfers immediately.
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

        if (!IsBiologicalSampleSource(args.Target.Value))
        {
            args.Handled = CompleteCollectorInteraction(ent, args.Target.Value, direction, args.User);
            return;
        }

        if (!CollectorInteractValidate(ent, (args.Target.Value, containerComponent), direction, args.User))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new CellCollectorDoAfter(), ent, target: args.Target, used: ent)
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
    /// Completes a living biopsy after its delay.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCollectorCollectDoAfter(Entity<CellCollectorComponent> ent, ref CellCollectorDoAfter args)
    {
        if (args.Handled || args.Cancelled || args.Target is null)
            return;

        args.Handled = CompleteCollectorInteraction(ent, args.Target.Value, CellCollectorDirection.Collection, args.Args.User);
    }

    private bool CompleteCollectorInteraction(Entity<CellCollectorComponent> ent, EntityUid target,
        CellCollectorDirection direction, EntityUid user)
    {
        if (!CollectorInteractValidate(ent, target, direction, user))
            return false;

        switch (direction)
        {
            case CellCollectorDirection.Collection:
                if (IsBiologicalSampleSource(target))
                {
                    var damage = GetCellularDamage(target);
                    if (!_genomes.TryGetSnapshot(target, out var genome) || genome is null)
                        return false;

                    if (!AddCell(ent.Owner, GenerateRandomCell(genome, damage)))
                        return false;

                    if (ent.Comp.Damage is not null)
                        _damageable.TryChangeDamage(target, ent.Comp.Damage);
                }
                else if (TryComp<CellContainerComponent>(target, out var targetComp))
                {
                    if (!MoveFirstCell((target, targetComp), ent.Owner))
                        return false;
                }

                _popup.PopupEntity(Loc.GetString("cell-collector-collected"), ent, user);

                break;

            case CellCollectorDirection.Transfer:
                if (!MoveFirstCell(ent.Owner, target))
                    return false;

                _popup.PopupEntity(Loc.GetString("cell-collector-transfer"), ent, user);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        UpdateCollectorAppearance(ent);
        return true;
    }

    /// <summary>
    /// Identifies an organism targeted by a biopsy attempt.
    /// </summary>
    private bool IsBiologicalSampleSource(EntityUid source)
    {
        return HasComp<GenomeComponent>(source);
    }

    private float GetCellularDamage(EntityUid source)
    {
        if (!TryComp<DamageableComponent>(source, out var damageable) ||
            !_damageable.GetPositiveDamage((source, damageable)).DamageDict.TryGetValue("Cellular", out var damage))
            return 0f;

        return (float) damage;
    }

    /// <summary>
    /// Creates a fresh laboratory sample from the organism's current genome.
    /// </summary>
    private Cell GenerateRandomCell(GenomeSnapshot genome, float cellularDamage)
    {
        var name = GetProceduralName(genome);

        var color = new Color(
            _random.NextFloat(),
            _random.NextFloat(),
            _random.NextFloat());

        var geneCount = genome.GeneCount;
        var stability = MathF.Max(0.1f, 1.0f - geneCount * 0.05f + _random.NextFloat(-0.03f, 0.03f));
        stability *= 1f - 0.5f * Math.Clamp(cellularDamage, 0f, 120f) / 100f;
        return new Cell(
            prototypeId: null,
            color: color,
            name: name,
            stability: stability,
            genome: genome);
    }

    private string GetProceduralName(GenomeSnapshot genome)
    {
        var canonical = new StringBuilder();
        AppendField(canonical, genome.GenomeId.ToString());
        foreach (var (id, state) in genome.GetGenes().OrderBy(entry => entry.Key.ToString(), StringComparer.Ordinal))
        {
            AppendField(canonical, id.ToString());
            AppendField(canonical, state.Allele);
            AppendField(canonical, state.Number?.ToString(CultureInfo.InvariantCulture));
        }

        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;
        var hash = offset;
        foreach (var value in Encoding.UTF8.GetBytes(canonical.ToString()))
        {
            hash = unchecked((hash ^ value) * prime);
        }

        var prefixes = _prototypes.Index(CellPrefixes).Values;
        var suffixes = _prototypes.Index(CellSuffixes).Values;
        var prefix = Loc.GetString(prefixes[(int) (hash % (ulong) prefixes.Count)]);
        var suffix = Loc.GetString(suffixes[(int) (hash / (ulong) prefixes.Count % (ulong) suffixes.Count)]);
        return prefix + suffix;
    }

    private static void AppendField(StringBuilder builder, string? value)
    {
        if (value is null)
            builder.Append("-1:");
        else
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
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
        if (ent.Owner == target.Owner)
            return false;

        if (!Resolve(ent, ref ent.Comp2))
            return false;

        TryComp(target.Owner, out target.Comp);

        switch (direction)
        {
            case CellCollectorDirection.Collection:
                if (ent.Comp2.Cells.Count >= ent.Comp2.MaxCells)
                {
                    if (!popup)
                        return false;

                    _popup.PopupEntity(Loc.GetString("cell-collector-full"), ent, user, PopupType.SmallCaution);
                    return false;
                }

                var biological = IsBiologicalSampleSource(target.Owner);
                if (biological && !_genomes.TryGetSnapshot(target.Owner, out _))
                {
                    if (popup)
                        _popup.PopupEntity(Loc.GetString("cell-collector-target-cant-collected"), ent, user, PopupType.SmallCaution);
                    return false;
                }
                if (biological && GetCellularDamage(target.Owner) >= 120f)
                {
                    if (popup)
                        _popup.PopupEntity(Loc.GetString("cell-collector-sample-damaged"), ent, user, PopupType.SmallCaution);
                    return false;
                }
                if (!biological && target.Comp is not { AllowCollection: true, Empty: false })
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
                    !target.Comp.AllowTransfer ||
                    target.Comp.Cells.Count >= target.Comp.MaxCells)
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
