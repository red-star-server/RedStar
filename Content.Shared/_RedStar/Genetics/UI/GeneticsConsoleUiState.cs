using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Genetics.Data;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Genetics.UI;

[Serializable, NetSerializable]
public enum GeneticsConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum GeneticsConsoleStatus : byte
{
    NoChamber,
    NoPatient,
    Ready,
    Working
}

/// <summary>
/// Live chamber report and procedure progress. No patient data is retained after disconnection.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneticsConsoleUiState(
    GeneticAnalyzerUiState? profile,
    GeneticsConsoleStatus status,
    List<GeneticProcedureInfo> procedures,
    List<ReagentQuantity>? beaker,
    bool busy,
    float progress,
    ProtoId<GenePrototype>? workingGene,
    GeneticProcedure? operation) : BoundUserInterfaceState
{
    public readonly GeneticAnalyzerUiState? Profile = profile;
    public readonly GeneticsConsoleStatus Status = status;
    public readonly List<GeneticProcedureInfo> Procedures = procedures;
    public readonly List<ReagentQuantity>? Beaker = beaker;
    public readonly bool Busy = busy;
    public readonly float Progress = progress;
    public readonly ProtoId<GenePrototype>? WorkingGene = workingGene;
    public readonly GeneticProcedure? Operation = operation;
}

[Serializable, NetSerializable]
public readonly record struct GeneticProcedureInfo(GeneticProcedure Procedure, ProtoId<ReagentPrototype> Reagent, FixedPoint2 Required, FixedPoint2 Available, float Seconds);

