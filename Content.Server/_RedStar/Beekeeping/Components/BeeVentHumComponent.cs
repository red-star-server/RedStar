using Content.Server._RedStar.Beekeeping.Systems;
using Robust.Shared.Audio;

namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A vent that hums with bees while a meat colony lives on its grid. Its own ambient sound plays the buzz meanwhile;
/// the sound it had is kept here and put back when the colony is gone.
/// </summary>
[RegisterComponent, Access(typeof(BeeVentHumSystem))]
public sealed partial class BeeVentHumComponent : Component
{
    /// <summary>
    /// The buzz the vent plays instead of its own sound.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RedStar/Ambience/Beekeeping/bee_vent.ogg");

    [DataField]
    public float Volume = -16f;

    [DataField]
    public float Range = 1f;

    /// <summary>
    /// The vent's own sound, put back when the buzz stops.
    /// </summary>
    [ViewVariables]
    public SoundSpecifier? OldSound;

    [ViewVariables]
    public float OldVolume;

    [ViewVariables]
    public float OldRange;
}
