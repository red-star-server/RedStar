using System.Numerics;
using Content.Shared.Humanoid; // RS14
using Content.Shared.Humanoid.Prototypes; // RS14
using Robust.Shared.Prototypes; // RS14
using Robust.Shared.Serialization;

namespace Content.Shared.Sprite;

public abstract partial class SharedScaleVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IPrototypeManager _proto = default!; // RS14

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScaleVisualsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ScaleVisualsComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnMapInit(Entity<ScaleVisualsComponent> ent, ref MapInitEvent args)
    {
        SetSpriteScale(ent.Owner, ent.Comp.Scale);
    }

    private void OnComponentShutdown(Entity<ScaleVisualsComponent> ent, ref ComponentShutdown args)
    {
        ResetScale(ent);
    }

    protected virtual void ResetScale(Entity<ScaleVisualsComponent> ent)
    {
        _appearance.RemoveData(ent.Owner, ScaleVisuals.Scale);
        var ev = new ScaleEntityEvent(ent.Owner, Vector2.One);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    /// <summary>
    /// Used to set the <see cref="Robust.Client.GameObjects.SpriteComponent.Scale"/> datafield to a certain value from the server.
    /// </summary>
    public void SetSpriteScale(EntityUid uid, Vector2 scale)
    {
        var comp = EnsureComp<ScaleVisualsComponent>(uid);
        comp.Scale = scale;
        // RS14-start
        if (TryComp<HumanoidProfileComponent>(uid, out var profile))
        {
            comp.SpeciesScale = _proto.Index(profile.Species).BaseScale;
            comp.ProfileScale = new Vector2(profile.Height, profile.Height);
            scale *= comp.SpeciesScale * comp.ProfileScale;
        }
        comp.ComputedScale = scale;
        // RS14-end
        Dirty(uid, comp);

        var appearanceComponent = EnsureComp<AppearanceComponent>(uid);
        _appearance.SetData(uid, ScaleVisuals.Scale, scale, appearanceComponent);

        // Raise an event for content use.
        var ev = new ScaleEntityEvent(uid, scale);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Gets the current scale set by <see cref="SetSpriteScale"/>.
    /// This does not include any direct changes made to the SpriteComponent.
    /// </summary>
    public Vector2 GetSpriteScale(EntityUid uid)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearanceComponent))
            return Vector2.One;

        if (!_appearance.TryGetData<Vector2>(uid, ScaleVisuals.Scale, out var scale, appearanceComponent))
            scale = Vector2.One;

        return scale;
    }
}

/// <summary>
/// Raised when a sprite scale is changed.
/// </summary>
[ByRefEvent]
public readonly record struct ScaleEntityEvent(EntityUid Uid, Vector2 Scale);

[Serializable, NetSerializable]
public enum ScaleVisuals : byte
{
    Scale,
}
