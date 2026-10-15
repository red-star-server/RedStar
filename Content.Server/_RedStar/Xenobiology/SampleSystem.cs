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
using Content.Shared._RedStar.Xenobiology.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Creates laboratory samples from current genomes and handles biopsy and transfer.
/// </summary>
public sealed partial class SampleSystem : EntitySystem
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
    private void OnSampleGenerationInit(Entity<SampleGenerationComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Generated || !TryComp<SampleContainerComponent>(ent, out var container))
            return;

        ent.Comp.Generated = true;
        // Preserve saved samples from before the generation flag was introduced.
        if (!container.Empty)
            return;

        foreach (var entry in ent.Comp.Samples)
        {
            if (!_genomes.TryGenerate(entry.Genome, out var genome) || genome is null)
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

    [SubscribeLocalEvent]
    private void OnCollectorInteract(Entity<SampleCollectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is null)
            return;

        TryComp<SampleContainerComponent>(args.Target, out var containerComponent);
        TryComp<SampleContainerComponent>(ent, out var collectorContainer);
        var biological = HasComp<GenomeComponent>(args.Target.Value);
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
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            BlockDuplicate = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

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
                if (HasComp<GenomeComponent>(target))
                {
                    var damage = GetCellularDamage(target);
                    if (!_genomes.TryGetSnapshot(target, out var genome) || genome is null)
                        return false;

                    if (!AddSample(ent.Owner, GenerateBiopsySample(genome, damage)))
                        return false;

                    if (ent.Comp.Damage is not null)
                        _damageable.TryChangeDamage(target, ent.Comp.Damage, origin: user);
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

    private float GetCellularDamage(EntityUid source)
    {
        if (!TryComp<DamageableComponent>(source, out var damageable) ||
            !_damageable.GetPositiveDamage((source, damageable)).DamageDict.TryGetValue("Cellular", out var damage))
            return 0f;

        return (float) damage;
    }

    private BiologicalSample GenerateBiopsySample(GenomeSnapshot genome, float cellularDamage)
    {
        var geneCount = genome.GeneCount;
        var stability = MathF.Max(0.1f, 1.0f - geneCount * 0.05f + _random.NextFloat(-0.03f, 0.03f));
        stability *= 1f - 0.5f * Math.Clamp(cellularDamage, 0f, 120f) / 100f;
        return CreateSample(genome, stability);
    }

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
        foreach (var id in genome.Genes.OrderBy(id => id.Id, StringComparer.Ordinal))
        {
            AppendField(canonical, id.ToString());
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

    private void UpdateCollectorAppearance(Entity<SampleCollectorComponent, SampleContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        _appearance.SetData(ent, CellCollectorVisuals.State, ent.Comp2.Empty);
    }

    private bool CollectorInteractValidate(Entity<SampleCollectorComponent, SampleContainerComponent?> ent,
        Entity<SampleContainerComponent?> target,
        SampleCollectorDirection direction,
        EntityUid user)
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
                    _popup.PopupEntity(Loc.GetString("cell-collector-full"), ent, user, PopupType.SmallCaution);
                    return false;
                }

                var biological = HasComp<GenomeComponent>(target.Owner);
                switch (biological)
                {
                    case true when !_genomes.TryGetSnapshot(target.Owner, out _):
                        _popup.PopupEntity(Loc.GetString("cell-collector-target-cant-collected"), ent, user, PopupType.SmallCaution);
                        return false;
                    case true when GetCellularDamage(target.Owner) >= 120f:
                        _popup.PopupEntity(Loc.GetString("cell-collector-sample-damaged"), ent, user, PopupType.SmallCaution);
                        return false;
                    case false when target.Comp is not { AllowCollection: true, Empty: false }:
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
