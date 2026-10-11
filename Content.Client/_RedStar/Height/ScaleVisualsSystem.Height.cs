using System.Numerics;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client.Sprite;

public sealed partial class ScaleVisualsSystem
{
    [Dependency] private SharedAppearanceSystem _heightAppearance = default!;

    private Vector2 ApplyCharacterHeight(EntityUid uid, Vector2 baseScale)
    {
        if (!TryComp<HumanoidProfileComponent>(uid, out var profile))
            return baseScale;

        var height = float.IsFinite(profile.Height) ? profile.Height : 1f;
        return baseScale * new Vector2(height);
    }

    [SubscribeLocalEvent]
    private void OnHeightState(Entity<HumanoidProfileComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<ScaleVisualsComponent>(ent, out _) ||
            !TryComp<SpriteComponent>(ent, out var sprite) ||
            !TryComp<AppearanceComponent>(ent, out var appearance) ||
            !_heightAppearance.TryGetData<Vector2>(ent.Owner, ScaleVisuals.Scale, out var baseScale, appearance))
            return;

        _sprite.SetScale((ent.Owner, sprite), ApplyCharacterHeight(ent.Owner, baseScale));
    }
}
