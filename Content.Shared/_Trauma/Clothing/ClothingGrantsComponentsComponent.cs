using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Clothing;

[RegisterComponent]
public sealed partial class ClothingGrantsComponentsComponent : Component
{
    /// <summary>
    /// Components granted to the wearer while this clothing is equipped.
    /// </summary>
    [DataField("component", required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Names of the components currently granted by this clothing.
    /// </summary>
    [DataField]
    public HashSet<string> Active = [];
}
