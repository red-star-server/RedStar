using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Genetics.Data;

[Serializable, NetSerializable]
public enum GeneticProcedure : byte
{
    /// <summary>
    /// Expresses a dormant gene.
    /// </summary>
    Activate,

    /// <summary>
    /// Returns an active gene to the dormant state.
    /// </summary>
    Deactivate,

    /// <summary>
    /// Deletes an acquired gene.
    /// </summary>
    Remove,

    /// <summary>
    /// Adds the genes of an inserted vial as dormant genes. Not bound to a single gene.
    /// </summary>
    Inject
}

public static class GeneticProcedureExtensions
{
    /// <summary>
    /// Checks acquired expression state for gene procedures; natural catalogue genes and injection have no applicable gene.
    /// </summary>
    public static bool CanProcess(this GeneticProcedure procedure, GeneStatus? status) => procedure switch
    {
        GeneticProcedure.Activate => status == GeneStatus.Dormant,
        GeneticProcedure.Deactivate => status == GeneStatus.Active,
        GeneticProcedure.Remove => status is not null,
        _ => false
    };
}
