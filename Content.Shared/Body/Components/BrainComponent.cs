using Content.Shared.Body.Systems;
using Robust.Shared.GameStates; // RS14

namespace Content.Shared.Body.Components;

[RegisterComponent, Access(typeof(BrainSystem))]
[NetworkedComponent, AutoGenerateComponentState] // RS14
public sealed partial class BrainComponent : Component;
