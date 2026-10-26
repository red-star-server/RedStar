using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Clothing;

[RegisterComponent]
public sealed partial class ClothingGrantTagComponent : Component
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    [DataField]
    public bool IsActive;
}
