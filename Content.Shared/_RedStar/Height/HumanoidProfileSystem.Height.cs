using System.Numerics;
using Content.Shared.Sprite;
using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem
{
    [Dependency] private SharedScaleVisualsSystem _heightScale = default!;

    private float GetVisualHeight(HumanoidCharacterProfile profile)
    {
        if (!ProtoMan.TryIndex(profile.Species, out var species) || !species.HeightAdjustable)
            return 1f;

        return float.IsFinite(profile.Height)
            ? Math.Clamp(profile.Height, species.MinHeight, species.MaxHeight)
            : 1f;
    }

    private void RefreshCharacterHeight(EntityUid uid)
    {
        // Re-emit the original scale so profile changes update the client sprite.
        var scale = TryComp<ScaleVisualsComponent>(uid, out var visuals)
            ? visuals.Scale
            : Vector2.One;
        _heightScale.SetSpriteScale(uid, scale);
    }
}
