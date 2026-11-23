using System.Linq;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Data;
using Content.Shared._RedStar.Xenobiology.Events;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SampleSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;

    [SubscribeLocalEvent]
    private void OnSamplesChanged(Entity<SampleContainerComponent> ent, ref SampleContainerChangedEvent args)
    {
        if (!_containers.TryGetContainingContainer(ent.Owner, out var container))
            return;

        var ev = new ContainedSamplesChangedEvent(ent.Owner);
        RaiseLocalEvent(container.Owner, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<SampleContainerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.SampleCount = ent.Comp.Samples.Count;
        Dirty(ent);

        foreach (var sample in ent.Comp.Samples)
        {
            if (sample.Id == 0)
                sample.Id = NextSampleId(ent.Comp);
        }
    }

    /// <summary>
    /// Sample IDs only need to be unique within one container, where they identify samples in UI messages.
    /// </summary>
    private static int NextSampleId(SampleContainerComponent container) =>
        container.Samples.Count == 0 ? 1 : checked(container.Samples.Max(sample => sample.Id) + 1);

    private void NotifyChanged(EntityUid uid)
    {
        var component = Comp<SampleContainerComponent>(uid);
        component.SampleCount = component.Samples.Count;
        Dirty(uid, component);
        var ev = new SampleContainerChangedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Adds a new sample and assigns its ID. Rejects samples with an existing ID or a full container.
    /// </summary>
    public bool AddSample(Entity<SampleContainerComponent?> ent, BiologicalSample sample)
    {
        if (!Resolve(ent, ref ent.Comp) || sample.Id != 0 ||
            ent.Comp.Samples.Count >= ent.Comp.MaxSamples)
            return false;

        sample.Id = NextSampleId(ent.Comp);
        ent.Comp.Samples.Add(sample);

        NotifyChanged(ent);
        return true;
    }

    /// <summary>
    /// Marks a gene of the selected sample as studied and notifies the containing machine on success.
    /// </summary>
    public bool TryMarkDecoded(Entity<SampleContainerComponent?> ent, int sampleId, ProtoId<GenePrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Samples.Find(candidate => candidate.Id == sampleId) is not { } sample ||
            !sample.TryMarkDecoded(id))
            return false;

        NotifyChanged(ent);
        return true;
    }

    /// <summary>
    /// Removes a gene from the selected sample and notifies the containing machine on success.
    /// </summary>
    public bool TryRemoveGene(Entity<SampleContainerComponent?> ent, int sampleId, ProtoId<GenePrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Samples.Find(candidate => candidate.Id == sampleId) is not { } sample ||
            !sample.TryRemoveGene(id))
            return false;

        NotifyChanged(ent);
        return true;
    }

    /// <summary>
    /// Reduces the selected sample's stability, destroying its genome at zero.
    /// Notifies observers only when stability changes.
    /// </summary>
    public bool TryReduceStability(Entity<SampleContainerComponent?> ent, int sampleId, float amount)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Samples.Find(candidate => candidate.Id == sampleId) is not { } sample ||
            !sample.TryReduceStability(amount))
            return false;

        NotifyChanged(ent);
        return true;
    }

    /// <summary>
    /// Raises the selected sample's stability and notifies observers when it changes.
    /// </summary>
    public bool TryRestoreStability(Entity<SampleContainerComponent?> ent, int sampleId, float amount)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Samples.Find(candidate => candidate.Id == sampleId) is not { } sample ||
            !sample.TryRestoreStability(amount))
            return false;

        NotifyChanged(ent);
        return true;
    }

    /// <summary>
    /// Moves the first sample, assigning a new ID only if the destination already uses it, and notifies both containers.
    /// Returns false without moving anything if the source is empty or the destination is full.
    /// </summary>
    public bool MoveFirstSample(Entity<SampleContainerComponent?> source, Entity<SampleContainerComponent?> destination)
    {
        if (source.Owner == destination.Owner ||
            !Resolve(source, ref source.Comp) || !Resolve(destination, ref destination.Comp) ||
            source.Comp.Empty || destination.Comp.Samples.Count >= destination.Comp.MaxSamples)
            return false;

        var sample = source.Comp.Samples[0];
        source.Comp.Samples.RemoveAt(0);
        if (destination.Comp.Samples.Exists(existing => existing.Id == sample.Id))
            sample.Id = NextSampleId(destination.Comp);
        destination.Comp.Samples.Add(sample);
        NotifyChanged(source);
        NotifyChanged(destination);
        return true;
    }
}
