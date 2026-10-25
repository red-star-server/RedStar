using Robust.Shared.GameStates;
using Content.Shared._Trauma.Common.Surgery.Tools; // RS14

namespace Content.Shared.Body;

/// <summary>
/// Marker components for child organs that are considered "internal" to their parent. e.g. kidneys are internal to a torso, but an arm isn't.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)] // RS14
public sealed partial class InternalChildOrganComponent : BaseSurgeryToolComponent; // RS14
