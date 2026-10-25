using Robust.Shared.Prototypes;
namespace Content.Shared._Trauma.Clothing;

[RegisterComponent]
public sealed partial class ClothingGrantComponentComponent : Component
{
    [DataField("component", required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components = new();

    [DataField]
    public HashSet<string> Active = new();
}
