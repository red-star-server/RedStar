namespace Content.Shared._RedStar.Xenobiology.Components;

/// <summary>
/// A reusable physical sample with a reward for its first research discovery.
/// </summary>
[RegisterComponent]
public sealed partial class XenobiologySampleComponent : Component
{
    [DataField]
    public int ResearchValue;
}
