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
        foreach (var sample in ent.Comp.Samples)
        {
            _nextSampleId = Math.Max(_nextSampleId, sample.Id);
        }

        foreach (var sample in ent.Comp.Samples)
        {
            if (sample.Id == 0)
                sample.Id = NextSampleId();
        }
    }

    private int NextSampleId()
    {
        var query = AllEntityQuery<SampleContainerComponent>();
        while (query.MoveNext(out var container))
        {
            foreach (var sample in container.Samples)
            {
                _nextSampleId = Math.Max(_nextSampleId, sample.Id);
            }
        }

        return ++_nextSampleId;
    }

    private void NotifyChanged(EntityUid uid)
    {
        var ev = new SampleContainerChangedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

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

    public bool TryRemoveGene(Entity<SampleContainerComponent?> ent, int sampleId, ProtoId<GenePrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Samples.Find(sample => sample.Id == sampleId) is not { } sample ||
            !sample.TryRemoveGene(id))
            return false;

        NotifyChanged(ent);
        return true;
    }

    public bool TryReduceStability(Entity<SampleContainerComponent?> ent, int sampleId, float amount)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Samples.Find(sample => sample.Id == sampleId) is not { } sample ||
            !sample.TryReduceStability(amount))
            return false;

        NotifyChanged(ent);
        return true;
    }

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
