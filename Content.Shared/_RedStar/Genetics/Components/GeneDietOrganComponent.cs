using Content.Shared._RedStar.Genetics.Systems;
using Content.Shared.Metabolism;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Components;

/// <summary>
/// Stores the native diet and replicated effective diet of a genetically adapted stomach.
/// Metabolism ownership records contain only types actually introduced by this adaptation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GeneDietSystem))]
public sealed partial class GeneDietOrganComponent : Component
{
    /// <summary>
    /// Special-food whitelist to restore when the last dietary gene is removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? OriginalWhitelist;

    /// <summary>
    /// Native restriction to special food, preserved independently of the effective diet.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OriginalExclusive;

    /// <summary>
    /// Effective whitelist applied on both server and client.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Whether the effective diet is restricted to the effective whitelist.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Exclusive;

    /// <summary>
    /// Metabolism types requested by the active dietary genes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<MetabolizerTypePrototype>> MetabolizerTypes = [];

    /// <summary>
    /// Locally introduced types to remove on refresh or shutdown. Native types are never recorded here.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MetabolizerTypePrototype>> AppliedMetabolizers = [];
}
