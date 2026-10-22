using Robust.Shared.Prototypes;

namespace Content.Shared.Metabolism;

public sealed partial class MetabolizerSystem
{
    /// <summary>
    /// Removes one supported metabolism type without changing the organ's other types or processing stages.
    /// Returns false if the organ or the specified type is absent.
    /// </summary>
    public bool TryRemoveMetabolizerType(Entity<MetabolizerComponent?> ent, ProtoId<MetabolizerTypePrototype> type)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.MetabolizerTypes?.Remove(type) == true;
    }
}
