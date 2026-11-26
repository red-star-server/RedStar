using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Xenobiology.Components;

/// <summary>
/// Marks a dead body that has already given a biopsy sample. Removed when the body is no longer dead.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SampledCorpseComponent : Component;
