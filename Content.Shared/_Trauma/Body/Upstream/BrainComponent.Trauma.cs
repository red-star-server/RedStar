namespace Content.Shared.Body.Components;

public sealed partial class BrainComponent
{
    /// <summary>
    /// Whether this brain currently provides its effects to the body.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = true;
}
