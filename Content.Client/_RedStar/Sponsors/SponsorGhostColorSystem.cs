// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goobstation/RMC patron ghost color implementation.

using Content.Shared._RedStar.Sponsors;
using Robust.Client.GameObjects;

namespace Content.Client._RedStar.Sponsors;

public sealed partial class SponsorGhostColorSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<SponsorGhostColorComponent> ent, ref ComponentStartup args)
    {
        UpdateColor(ent);
    }

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<SponsorGhostColorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateColor(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<SponsorGhostColorComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetColor((ent.Owner, sprite), Color.White);
    }

    private void UpdateColor(Entity<SponsorGhostColorComponent> ent)
    {
        if (ent.Comp.Color is not { } color ||
            !TryComp<SpriteComponent>(ent, out var sprite))
        {
            return;
        }

        _sprite.SetColor((ent.Owner, sprite), color);
    }
}
