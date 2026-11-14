using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Common.Wounds;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Traumas;

/// <summary>
/// Component given to bodyparts that have bones.
/// </summary>
[RegisterComponent, AutoGenerateComponentState(fieldDeltas: true), NetworkedComponent]
public sealed partial class BoneComponent : Component
{
    /// <summary>
    /// Maximum integrity of the bone.
    /// </summary>
    [DataField]
    public FixedPoint2 IntegrityCap = 60f;

    /// <summary>
    /// Current remaining integrity of the bone.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BoneIntegrity = 60f;

    /// <summary>
    /// Severity state derived from the bone's remaining integrity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BoneSeverity BoneSeverity = BoneSeverity.Normal;

    [DataField]
    public Dictionary<BoneSeverity, FixedPoint2> SeverityThresholds = new()
    {
        { BoneSeverity.Normal, 40 },
        { BoneSeverity.Damaged, 25 },
        { BoneSeverity.Cracked, 10 },
        { BoneSeverity.Broken, 0 }
    };

    [DataField]
    public Dictionary<WoundableSeverity, float> TraumaChanceMultipliers = new()
    {
        { WoundableSeverity.Moderate, 0.03f },
        { WoundableSeverity.Severe, 0.1f },
        { WoundableSeverity.Critical, 0.16f },
        { WoundableSeverity.Mangled, 0.21f }
    };

    [DataField]
    public Dictionary<BoneSeverity, float> DismembermentChanceMultipliers = new()
    {
        { BoneSeverity.Normal, 0.3f },
        { BoneSeverity.Damaged, 0.6f },
        { BoneSeverity.Cracked, 1f },
        { BoneSeverity.Broken, 1.2f }
    };

    [DataField]
    public Dictionary<BoneSeverity, float> DoAfterSpeedMultipliers = new()
    {
        { BoneSeverity.Damaged, 0.92f },
        { BoneSeverity.Cracked, 0.84f },
        { BoneSeverity.Broken, 0.75f }
    };

    /// <summary>
    /// Sound played when this bone breaks.
    /// </summary>
    [DataField]
    public SoundSpecifier BoneBreakSound = new SoundCollectionSpecifier("BoneGone");
}
