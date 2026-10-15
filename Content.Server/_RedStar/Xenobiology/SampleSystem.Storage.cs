using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class SampleSystem : EntitySystem
{
    private int _nextSampleId;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SampleContainerComponent> ent, ref MapInitEvent args)
    {
        foreach (var sample in ent.Comp.Samples)
        {
            sample.Id = ++_nextSampleId;
        }
    }

    public bool AddSample(Entity<SampleContainerComponent?> ent, BiologicalSample sample)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Samples.Count >= ent.Comp.MaxSamples)
            return false;

        sample.Id = ++_nextSampleId;
        ent.Comp.Samples.Add(sample);

        var ev = new SampleContainerChangedEvent();
        RaiseLocalEvent(ent, ev);

        return true;
    }

    public bool ReplaceSamples(Entity<SampleContainerComponent?> ent, BiologicalSample first, BiologicalSample second, BiologicalSample result)
    {
        if (!Resolve(ent, ref ent.Comp) || ReferenceEquals(first, second) ||
            ReferenceEquals(result, first) || ReferenceEquals(result, second) ||
            !ent.Comp.Samples.Contains(first) || !ent.Comp.Samples.Contains(second) ||
            ent.Comp.Samples.Contains(result))
            return false;

        ent.Comp.Samples.Remove(first);
        ent.Comp.Samples.Remove(second);
        result.Id = ++_nextSampleId;
        ent.Comp.Samples.Add(result);
        RaiseLocalEvent(ent, new SampleContainerChangedEvent());
        return true;
    }

    public bool TryRemoveGene(Entity<SampleContainerComponent?> ent, BiologicalSample sample, ProtoId<GenePrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp) || !ent.Comp.Samples.Contains(sample) || !sample.TryRemoveGene(id))
            return false;

        RaiseLocalEvent(ent, new SampleContainerChangedEvent());
        return true;
    }

    public bool TryReduceStability(Entity<SampleContainerComponent?> ent, BiologicalSample sample, float amount)
    {
        if (!Resolve(ent, ref ent.Comp) || !ent.Comp.Samples.Contains(sample) ||
            !sample.TryReduceStability(amount))
            return false;

        RaiseLocalEvent(ent, new SampleContainerChangedEvent());
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
        RaiseLocalEvent(source, new SampleContainerChangedEvent());
        RaiseLocalEvent(destination, new SampleContainerChangedEvent());
        return true;
    }
}
