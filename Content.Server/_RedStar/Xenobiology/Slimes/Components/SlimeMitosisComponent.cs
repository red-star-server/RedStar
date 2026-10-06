namespace Content.Server._RedStar.Xenobiology.Slimes.Components;

/// <summary>
/// Removes the adult after successful division into offspring.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SlimeMitosisComponent : Component
{
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(90);

    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(120);

    [DataField]
    public TimeSpan GestationDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public float HungerPerBirth = 75f;

    [DataField]
    public int OffspringCount = 2;

    [AutoPausedField]
    public TimeSpan NextAttempt;

    [AutoPausedField]
    public TimeSpan? GestationEnd;
}
