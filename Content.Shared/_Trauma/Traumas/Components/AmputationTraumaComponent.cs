using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Traumas;

/// <summary>
/// Component used for dismemberment leftover wounds that makes them distinct if the source
/// part (e.g. arms legs) is different, allowing multiple wounds on the dest part (torso).
/// </summary>
[RegisterComponent, NetworkedComponent]
[EntityCategory("Traumas")]
[AutoGenerateComponentState]
public sealed partial class AmputationTraumaComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<OrganCategoryPrototype> Source;
}
