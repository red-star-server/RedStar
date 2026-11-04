using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Clothing;

[RegisterComponent]
public sealed partial class ClothingGrantTagComponent : Component
{
    /// <summary>
    /// Tag granted to the wearer while this clothing is equipped.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    /// <summary>
    /// Whether the tag is currently granted to the wearer.
    /// </summary>
    [DataField]
    public bool IsActive;
}
