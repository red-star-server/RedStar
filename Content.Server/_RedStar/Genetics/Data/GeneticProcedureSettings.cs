using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics.Data;

/// <summary>
/// Total reagent dose and processing time required for a chamber procedure.
/// </summary>
[DataDefinition]
public sealed partial class GeneticProcedureSettings
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>
    /// Total amount consumed over a complete procedure. Partial processing consumes a proportional dose.
    /// </summary>
    [DataField]
    public FixedPoint2 Dose = 10;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);
}
