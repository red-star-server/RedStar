namespace Content.Server._RedStar.Genetics.Components;

/// <summary>
/// Controls one linked genetic chamber through its dedicated device port.
/// </summary>
[RegisterComponent]
public sealed partial class GeneticsConsoleComponent : Component
{
    public const string ChamberPort = "GeneticChamberSender";

    /// <summary>
    /// Maximum operating distance to the anchored chamber.
    /// </summary>
    [DataField]
    public float MaxDistance = 4;

    /// <summary>
    /// Chamber executing this console's current procedure. Cleared on completion or cancellation.
    /// </summary>
    public EntityUid? ActiveChamber;
}
