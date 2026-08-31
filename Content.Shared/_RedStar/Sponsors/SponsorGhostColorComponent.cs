// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goobstation/RMC patron ghost color implementation.

using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Sponsors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SponsorGhostColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color? Color;
}
