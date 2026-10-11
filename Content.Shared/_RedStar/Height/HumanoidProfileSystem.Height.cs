using System.Numerics;
using Content.Shared.Sprite;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem
{
    [Dependency] private SharedScaleVisualsSystem _heightScale = default!;

    private void RefreshCharacterHeight(EntityUid uid)
    {
        // Re-emit the original scale so profile changes update the client sprite.
        var scale = TryComp<ScaleVisualsComponent>(uid, out var visuals)
            ? visuals.Scale
            : Vector2.One;
        _heightScale.SetSpriteScale(uid, scale);
    }
}
