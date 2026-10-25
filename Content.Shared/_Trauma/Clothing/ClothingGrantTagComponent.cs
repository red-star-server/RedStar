using Robust.Shared.Prototypes;

using Content.Shared.Tag;

namespace Content.Shared._Trauma.Clothing;

[RegisterComponent]
public sealed partial class ClothingGrantTagComponent : Component
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    [DataField]
    public bool IsActive;
}
