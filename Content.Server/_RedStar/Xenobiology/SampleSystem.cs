using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class SampleSystem : EntitySystem
{
    private int _nextSampleId;
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

        if (ent.Comp.Samples.Count > 0)
            ReserveLoadedSampleIds();

        foreach (var sample in ent.Comp.Samples)
        {
            if (sample.Id == 0)
                sample.Id = NextSampleId();
        }
    }

    private void ReserveLoadedSampleIds()
    {
        var query = AllEntityQuery<SampleContainerComponent>();
        while (query.MoveNext(out var container))
        {
            foreach (var sample in container.Samples)
            {
                _nextSampleId = Math.Max(_nextSampleId, sample.Id);
            }
        }
    }

    private int NextSampleId() => checked(++_nextSampleId);

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

        sample.Id = NextSampleId();
        ent.Comp.Samples.Add(sample);

        NotifyChanged(ent);
        return true;
    }

    /// <summary>
    /// Replaces two distinct samples with one new sample and notifies observers once.
    /// Leaves the container unchanged if either input is missing or the result already has an ID.
    /// </summary>
    public bool ReplaceSamples(Entity<SampleContainerComponent?> ent, int firstId, int secondId, BiologicalSample result)
    {
        if (!Resolve(ent, ref ent.Comp) || result.Id != 0 || firstId == secondId ||
            ent.Comp.Samples.Find(sample => sample.Id == firstId) is not { } first ||
            ent.Comp.Samples.Find(sample => sample.Id == secondId) is not { } second)
            return false;

        result.Id = NextSampleId();
        ent.Comp.Samples.Remove(first);
        ent.Comp.Samples.Remove(second);
        ent.Comp.Samples.Add(result);
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
    /// Moves the first sample without changing its ID and notifies both containers.
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
        destination.Comp.Samples.Add(sample);
        NotifyChanged(source);
        NotifyChanged(destination);
        return true;
    }
}
