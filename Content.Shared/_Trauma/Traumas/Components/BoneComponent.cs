using Content.Shared._Trauma.Common.Traumas;
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

    /// <summary>
    /// Sound played when this bone breaks.
    /// </summary>
    [DataField]
    public SoundSpecifier BoneBreakSound = new SoundCollectionSpecifier("BoneGone");
}
