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
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Creates laboratory samples from current genomes and handles biopsy and transfer.
/// </summary>
public sealed partial class SampleSystem : SharedSampleSystem
{
    private enum SampleCollectorDirection
    {
        Collection,
        Transfer
    }

    private static readonly ProtoId<LocalizedDatasetPrototype> SamplePrefixes = "CellPrefixes";
    private static readonly ProtoId<LocalizedDatasetPrototype> SampleSuffixes = "CellSuffixes";

    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private GenomeSystem _genomes = default!;

    [SubscribeLocalEvent]
    private void OnSampleGenerationInit(Entity<SampleGenerationComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SampleContainerComponent>(ent, out var container))
            return;

        foreach (var entry in ent.Comp.Samples)
        {
            if (_genomes.Generate(entry.Genome, out var genome) != GenomeOperationResult.Success || genome is null)
                continue;

            AddSample((ent, container), CreateSample(genome, entry.Stability));
        }
    }

    [SubscribeLocalEvent]
    private void OnCollectorSamplesChanged(Entity<SampleCollectorComponent> ent, ref SampleContainerChangedEvent args)
    {
        UpdateCollectorAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnCollectorMapInit(Entity<SampleCollectorComponent> ent, ref MapInitEvent args)
    {
        UpdateCollectorAppearance(ent);
    }

    /// <summary>
    /// Starts living biopsies and handles transfers between sample containers.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCollectorInteract(Entity<SampleCollectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is null)
            return;

        TryComp<SampleContainerComponent>(args.Target, out var containerComponent);
        TryComp<SampleContainerComponent>(ent, out var collectorContainer);
        var biological = IsSampleSource(args.Target.Value);
        var direction = biological ||
            collectorContainer is { Empty: true } &&
            containerComponent is { Empty: false, AllowCollection: true }
            ? SampleCollectorDirection.Collection
            : SampleCollectorDirection.Transfer;

        if (!biological)
        {
            args.Handled = CompleteCollectorInteraction(ent, args.Target.Value, direction, args.User);
            return;
        }

        if (!CollectorInteractValidate(ent, (args.Target.Value, containerComponent), direction, args.User))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new SampleCollectorDoAfter(), ent, target: args.Target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            BlockDuplicate = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// Completes a living biopsy after its delay.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCollectorCollectDoAfter(Entity<SampleCollectorComponent> ent, ref SampleCollectorDoAfter args)
    {
        if (args.Handled || args.Cancelled || args.Target is null)
            return;

        args.Handled = CompleteCollectorInteraction(ent, args.Target.Value, SampleCollectorDirection.Collection, args.Args.User);
    }

    private bool CompleteCollectorInteraction(Entity<SampleCollectorComponent> ent, EntityUid target,
        SampleCollectorDirection direction, EntityUid user)
    {
        if (!CollectorInteractValidate(ent, target, direction, user))
            return false;

        switch (direction)
        {
            case SampleCollectorDirection.Collection:
                if (IsSampleSource(target))
                {
                    var damage = GetCellularDamage(target);
                    if (!_genomes.TryGetSnapshot(target, out var genome) || genome is null)
                        return false;

                    if (!AddSample(ent.Owner, GenerateBiopsySample(genome, damage)))
                        return false;

                    if (ent.Comp.Damage is not null)
                        _damageable.TryChangeDamage(target, ent.Comp.Damage);
                }
                else if (TryComp<SampleContainerComponent>(target, out var targetComp))
                {
                    if (!MoveFirstSample((target, targetComp), ent.Owner))
                        return false;
                }

                _popup.PopupEntity(Loc.GetString("cell-collector-collected"), ent, user);

                break;

            case SampleCollectorDirection.Transfer:
                if (!MoveFirstSample(ent.Owner, target))
                    return false;

                _popup.PopupEntity(Loc.GetString("cell-collector-transfer"), ent, user);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        return true;
    }

    /// <summary>
    /// Identifies an organism targeted by a biopsy attempt.
    /// </summary>
    private bool IsSampleSource(EntityUid source)
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
    private BiologicalSample GenerateBiopsySample(GenomeSnapshot genome, float cellularDamage)
    {
        var geneCount = genome.GeneCount;
        var stability = MathF.Max(0.1f, 1.0f - geneCount * 0.05f + _random.NextFloat(-0.03f, 0.03f));
        stability *= 1f - 0.5f * Math.Clamp(cellularDamage, 0f, 120f) / 100f;
        return CreateSample(genome, stability);
    }

    /// <summary>
    /// Derives a sample's initial appearance from its genes without later recalculation.
    /// </summary>
    private BiologicalSample CreateSample(GenomeSnapshot genome, float stability)
    {
        var hash = GetAppearanceHash(genome);
        var prefixes = _prototypes.Index(SamplePrefixes).Values;
        var suffixes = _prototypes.Index(SampleSuffixes).Values;
        var prefix = Loc.GetString(prefixes[(int) (hash % (ulong) prefixes.Count)]);
        var suffix = Loc.GetString(suffixes[(int) (hash / (ulong) prefixes.Count % (ulong) suffixes.Count)]);
        var color = new Color(
            0.2f + (hash & 0xff) / 255f * 0.65f,
            0.2f + (hash >> 8 & 0xff) / 255f * 0.65f,
            0.2f + (hash >> 16 & 0xff) / 255f * 0.65f);
        return new BiologicalSample(color, prefix + suffix, stability, genome);
    }

    private static ulong GetAppearanceHash(GenomeSnapshot genome)
    {
        var canonical = new StringBuilder();
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

        return hash;
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
    private void UpdateCollectorAppearance(Entity<SampleCollectorComponent, SampleContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        _appearance.SetData(ent, CellCollectorVisuals.State, ent.Comp2.Empty);
    }

    /// <summary>
    /// Validates collection or transfer and optionally shows failure popups.
    /// </summary>
    private bool CollectorInteractValidate(Entity<SampleCollectorComponent, SampleContainerComponent?> ent,
        Entity<SampleContainerComponent?> target,
        SampleCollectorDirection direction,
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
            case SampleCollectorDirection.Collection:
                if (ent.Comp2.Samples.Count >= ent.Comp2.MaxSamples)
                {
                    if (!popup)
                        return false;

                    _popup.PopupEntity(Loc.GetString("cell-collector-full"), ent, user, PopupType.SmallCaution);
                    return false;
                }

                var biological = IsSampleSource(target.Owner);
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

            case SampleCollectorDirection.Transfer:
                if (target.Comp == null ||
                    _entityWhitelist.IsWhitelistFail(target.Comp.ToolsTransferWhitelist, ent) ||
                    target.Comp.ToolsTransferWhitelist is null ||
                    !target.Comp.AllowTransfer ||
                    target.Comp.Samples.Count >= target.Comp.MaxSamples)
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
