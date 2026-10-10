using System.Numerics;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Height;

/// <summary>
/// Applies species and character height to sprite appearance while leaving the scale component's
/// base value available to other scaling systems.
/// </summary>
public sealed partial class CharacterHeightSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedScaleVisualsSystem _scale = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    public void Refresh(EntityUid uid)
    {
        var baseScale = TryComp<ScaleVisualsComponent>(uid, out var visuals)
            ? visuals.Scale
            : Vector2.One;
        _scale.SetSpriteScale(uid, baseScale);
    }

    [SubscribeLocalEvent]
    private void OnScaleChanged(Entity<HumanoidProfileComponent> ent, ref ScaleEntityEvent args)
    {
        if (!TryComp<ScaleVisualsComponent>(ent, out var visuals) ||
            visuals.LifeStage >= ComponentLifeStage.Stopping)
            return;

        if (!_prototypes.TryIndex(ent.Comp.Species, out var species))
            return;

        var height = float.IsFinite(ent.Comp.Height) ? ent.Comp.Height : 1f;
        var visualScale = args.Scale * species.BaseScale * new Vector2(height);
        _appearance.SetData(ent.Owner, ScaleVisuals.Scale, visualScale);
    }
}
