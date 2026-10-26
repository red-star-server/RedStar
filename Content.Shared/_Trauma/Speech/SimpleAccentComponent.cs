using Content.Shared.Speech.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Speech;

/// <summary>
/// Applies a single <see cref="SimpleAccentPrototype"/> to the speaking entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SimpleAccentComponent : BaseAccentComponent
{
    [DataField(required: true)]
    public ProtoId<SimpleAccentPrototype> Accent;
}
