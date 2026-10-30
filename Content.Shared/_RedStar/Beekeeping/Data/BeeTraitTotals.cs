using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

/// <summary>
/// What a set of bee traits adds up to. Neutral without traits.
/// </summary>
[Serializable, NetSerializable]
public struct BeeTraitTotals()
{
    public float CombRate = 1f;
    public float CombCost = 1f;
    public float NectarPerVisit = 1f;
    public float PollinationChance = 1f;
    public int PollinatorCharges;
    public float BeeCost = 1f;
    public float LayRate = 1f;
    public float EggWait = 1f;
    public float EggLife = 1f;
    public float EggCooldown = 1f;
    public float Lifetime = 1f;
    public float ScoutPause = 1f;
    public int ScoutFinds;
    public float PollinationMutation;
    public bool EmergencyQueen;
    public bool PestControl;
    public bool Sting;
    public bool Aggressive;
    public int AngryPerHit;
    public float AngryDamageStep = 1f;
    public bool GuardedScout;
}
