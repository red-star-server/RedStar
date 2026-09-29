using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RedStar.Emoting.Prototypes;

/// <summary>
/// Defines a cooperative emote performed by two entities.
/// The visual part reuses regular emote animation prototypes.
/// </summary>
[Prototype]
public sealed partial class PairedEmotePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField(required: true)]
    public ProtoId<EmoteAnimationPrototype> InitiatorAnimation;

    [DataField(required: true)]
    public ProtoId<EmoteAnimationPrototype> TargetAnimation;

    /// <summary>
    /// Maximum distance at which the emote can be accepted.
    /// </summary>
    [DataField]
    public float Range = 1.5f;

    /// <summary>
    /// How long the offer remains active.
    /// </summary>
    [DataField]
    public TimeSpan OfferDuration = TimeSpan.FromSeconds(5);

    [DataField(required: true)]
    public LocId AttemptSelf;

    [DataField(required: true)]
    public LocId AttemptTarget;

    [DataField(required: true)]
    public LocId Success;
}
