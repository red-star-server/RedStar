using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Genetics.Events;

/// <summary>
/// Includes the displayed patient so a delayed request cannot operate on a replacement occupant.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneticsProcedureMessage(NetEntity patient, ProtoId<GenePrototype> gene, GeneticProcedure procedure) : BoundUserInterfaceMessage
{
    public readonly NetEntity Patient = patient;
    public readonly ProtoId<GenePrototype> Gene = gene;
    public readonly GeneticProcedure Procedure = procedure;
}

[Serializable, NetSerializable]
public sealed class GeneticsCancelMessage : BoundUserInterfaceMessage;
