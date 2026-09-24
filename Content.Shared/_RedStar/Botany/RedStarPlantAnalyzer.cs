using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Botany;

[Serializable, NetSerializable]
public enum RedStarPlantAnalyzerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class RedStarPlantAnalyzerDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class RedStarPlantAnalyzerSetMode : BoundUserInterfaceMessage
{
    public bool Advanced { get; }

    public RedStarPlantAnalyzerSetMode(bool advanced)
    {
        Advanced = advanced;
    }
}

[Serializable, NetSerializable]
public sealed class RedStarPlantAnalyzerReport : BoundUserInterfaceMessage
{
    public NetEntity Target { get; init; }
    public bool IsPlant { get; init; }
    public string SeedNameKey { get; init; } = "plant-analyzer-unknown-plant";
    public string[] Chemicals { get; init; } = Array.Empty<string>();
    public string[] ConsumedGases { get; init; } = Array.Empty<string>();
    public string[] EmittedGases { get; init; } = Array.Empty<string>();
    public string[] PossibleMutations { get; init; } = Array.Empty<string>();
    public string HarvestType { get; init; } = "Unknown";
    public float Endurance { get; init; }
    public int Yield { get; init; }
    public float Potency { get; init; }
    public float GeneticInstability { get; init; }
    public float Lifespan { get; init; }
    public float Maturation { get; init; }
    public float Production { get; init; }
    public int GrowthStages { get; init; }
    public bool Advanced { get; init; }
    public float NutrientConsumption { get; init; }
    public float WaterConsumption { get; init; }
    public float IdealHeat { get; init; }
    public float HeatTolerance { get; init; }
    public float ToxinsTolerance { get; init; }
    public float LowPressureTolerance { get; init; }
    public float HighPressureTolerance { get; init; }
    public float PestTolerance { get; init; }
    public float WeedTolerance { get; init; }
    public bool Seedless { get; init; }
    public bool Ligneous { get; init; }
    public bool CanScream { get; init; }
}
