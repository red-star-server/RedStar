using Content.Shared._Trauma.Body;
using Content.Shared.Body.Systems;

namespace Content.Shared.Body.Components;

public sealed partial class BloodstreamComponent
{
    /// <summary>
    /// Bleeding caused by wounds, separate from simple bleeding and abilities.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(BloodstreamSystem), typeof(BodyBloodstreamSystem))]
    public float BleedAmountFromWounds;

    /// <summary>
    /// Bleeding contribution from sources other than wound entities.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(BloodstreamSystem), typeof(BodyBloodstreamSystem))]
    public float BleedAmountNotFromWounds;
}
