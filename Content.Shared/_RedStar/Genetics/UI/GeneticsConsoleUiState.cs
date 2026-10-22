using Content.Shared._RedStar.Genetics.Data;
using Content.Shared.Chemistry.Reagent;
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
    float progress,
    GeneticProcedure? operation) : BoundUserInterfaceState
{
    public readonly GeneticAnalyzerUiState? Profile = profile;
    public readonly GeneticsConsoleStatus Status = status;
    public readonly List<GeneticProcedureInfo> Procedures = procedures;
    public readonly List<ReagentQuantity>? Beaker = beaker;
    public bool Busy => Status == GeneticsConsoleStatus.Working;
    public readonly float Progress = progress;
    public readonly GeneticProcedure? Operation = operation;
}

[Serializable, NetSerializable]
public readonly record struct GeneticProcedureInfo(GeneticProcedure Procedure, bool CanStart);

