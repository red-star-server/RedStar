using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Events;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Systems;

/// <summary>
/// Manages samples, transfers, revisions and change notifications.
/// </summary>
/// <seealso cref="SampleContainerComponent"/>
/// <seealso cref="SampleGenerationComponent"/>
/// <seealso cref="SharedCellVisualsSystem"/>
[PublicAPI]
public abstract partial class SharedSampleSystem : EntitySystem
{
    /// <summary>
    /// Adds a new <see cref="BiologicalSample"/> instance to the container.
    /// </summary>
    public bool AddSample(Entity<SampleContainerComponent?> ent, BiologicalSample sample)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Samples.Count >= ent.Comp.MaxSamples)
            return false;

        ent.Comp.Samples.Add(sample);
        ent.Comp.Revision++;

        var ev = new SampleContainerChangedEvent();
        RaiseLocalEvent(ent, ev);

        return true;
    }

    /// <summary>
    /// Replaces two distinct samples with one result and notifies listeners once.
    /// </summary>
    public bool ReplaceSamples(Entity<SampleContainerComponent?> ent, BiologicalSample first, BiologicalSample second, BiologicalSample result)
    {
        if (!Resolve(ent, ref ent.Comp) || ReferenceEquals(first, second) ||
            ReferenceEquals(result, first) || ReferenceEquals(result, second) ||
            !ent.Comp.Samples.Contains(first) || !ent.Comp.Samples.Contains(second) ||
            ent.Comp.Samples.Contains(result))
            return false;

        ent.Comp.Samples.Remove(first);
        ent.Comp.Samples.Remove(second);
        ent.Comp.Samples.Add(result);
        ent.Comp.Revision++;
        RaiseLocalEvent(ent, new SampleContainerChangedEvent());
        return true;
    }

    /// <summary>
    /// Removes one gene from a contained sample and notifies observers once.
    /// </summary>
    public bool TryRemoveGene(Entity<SampleContainerComponent?> ent, BiologicalSample sample, ProtoId<GenePrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp) || !ent.Comp.Samples.Contains(sample) || !sample.TryRemoveGene(id))
            return false;

        ent.Comp.Revision++;
        RaiseLocalEvent(ent, new SampleContainerChangedEvent());
        return true;
    }

    /// <summary>
    /// Reduces contained sample stability and notifies observers only on change.
    /// </summary>
    public bool TryReduceStability(Entity<SampleContainerComponent?> ent, BiologicalSample sample, float amount)
    {
        if (!Resolve(ent, ref ent.Comp) || !ent.Comp.Samples.Contains(sample) ||
            !sample.TryReduceStability(amount))
            return false;

        ent.Comp.Revision++;
        RaiseLocalEvent(ent, new SampleContainerChangedEvent());
        return true;
    }

    /// <summary>
    /// Remove all <see cref="BiologicalSample"/> instances from the container.
    /// </summary>
    public void ClearSamples(Entity<SampleContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Empty)
            return;

        ent.Comp.Samples.Clear();
        ent.Comp.Revision++;
        RaiseLocalEvent(ent, new SampleContainerChangedEvent());
    }

    /// <summary>
    /// Moves the first sample from one container to another without an intermediate notification.
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
        source.Comp.Revision++;
        destination.Comp.Revision++;
        RaiseLocalEvent(source, new SampleContainerChangedEvent());
        RaiseLocalEvent(destination, new SampleContainerChangedEvent());
        return true;
    }
}
