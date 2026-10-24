using Robust.Shared.GameStates;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared._Trauma.Traumas;

/// <summary>
/// Component given to bodyparts that have bones.
/// </summary>
[RegisterComponent, AutoGenerateComponentState(fieldDeltas: true), NetworkedComponent]
public sealed partial class BoneComponent : Component
{
    [DataField]
    public FixedPoint2 IntegrityCap = 60f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BoneIntegrity = 60f;

    [DataField, AutoNetworkedField]
    public BoneSeverity BoneSeverity = BoneSeverity.Normal;

    [DataField]
    public SoundSpecifier BoneBreakSound = new SoundCollectionSpecifier("BoneGone");
}
