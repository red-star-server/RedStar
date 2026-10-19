using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Genetics.Data;

[Serializable, NetSerializable]
public enum GeneticProcedure : byte
{
    Activate,
    Suppress,
    Remove
}

public static class GeneticProcedureExtensions
{
    /// <summary>
    /// Checks acquired expression state; natural catalogue genes have no applicable procedure.
    /// </summary>
    public static bool CanProcess(this GeneticProcedure procedure, GeneStatus? status) => procedure switch
    {
        GeneticProcedure.Activate => status is GeneStatus.Raw or GeneStatus.Suppressed,
        GeneticProcedure.Suppress => status == GeneStatus.Active,
        GeneticProcedure.Remove => status is not null,
        _ => false
    };
}
