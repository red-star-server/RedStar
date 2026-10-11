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
        RefreshHeightSprite(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnHeightChanged(Entity<HumanoidProfileComponent> ent, ref CharacterHeightChangedEvent args)
    {
        RefreshHeightSprite(ent.Owner);
    }

    private void RefreshHeightSprite(EntityUid uid)
    {
        if (!TryComp<ScaleVisualsComponent>(uid, out var visuals) ||
            visuals.LifeStage >= ComponentLifeStage.Stopping ||
            !TryComp<SpriteComponent>(uid, out var sprite) ||
            !TryComp<AppearanceComponent>(uid, out var appearance) ||
            !_heightAppearance.TryGetData<Vector2>(uid, ScaleVisuals.Scale, out var baseScale, appearance))
            return;

        _sprite.SetScale((uid, sprite), ApplyCharacterHeight(uid, baseScale));
    }
}
