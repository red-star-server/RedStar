using System.Numerics;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Sprite;

public sealed partial class ScaleVisualsSystem
{
    [Dependency] private IPrototypeManager _heightPrototypes = default!;
    [Dependency] private SharedAppearanceSystem _heightAppearance = default!;

    private Vector2 ApplyCharacterHeight(EntityUid uid, Vector2 baseScale)
    {
        if (!TryComp<HumanoidProfileComponent>(uid, out var profile) ||
            !_heightPrototypes.TryIndex(profile.Species, out var species))
            return baseScale;

        var height = float.IsFinite(profile.Height) ? profile.Height : 1f;
        return baseScale * species.BaseScale * new Vector2(height);
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
