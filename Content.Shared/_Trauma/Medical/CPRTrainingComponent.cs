using Robust.Shared.GameStates;

using Robust.Shared.Audio;

namespace Content.Shared._Trauma.Medical;

/// <summary>
/// Allows a mob to perform CPR with a verb.
/// Both this mob and the target must have <c>RespiratorComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCPRSystem))]
public sealed partial class CPRTrainingComponent : Component
{
    /// <summary>
    /// How long the doafter lasts.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Chance to attempt reviving after a compression, if conditions are met.
    /// </summary>
    [DataField]
    public float ReviveChance = 0.3f;

    /// <summary>
    /// Chance to make the patient inhale after a compression, if their lungs are healthy.
    /// </summary>
    [DataField]
    public float InhaleChance = 0.6f; // better than nothing but proper training would help

    /// <summary>
    /// Sound played after each compression.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RedStar/Medical/Effects/CPR.ogg");
}
