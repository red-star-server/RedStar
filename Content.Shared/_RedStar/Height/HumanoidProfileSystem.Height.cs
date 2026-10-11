using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem
{
    public void ApplyHeightTo(Entity<HumanoidProfileComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var height = GetVisualHeight(profile);
        if (MathF.Abs(ent.Comp.Height - height) <= 0.0001f)
            return;

        ent.Comp.Height = height;
        Dirty(ent);
        var heightChanged = new CharacterHeightChangedEvent();
        RaiseLocalEvent(ent.Owner, ref heightChanged);
    }

    private float GetVisualHeight(HumanoidCharacterProfile profile)
    {
        if (!ProtoMan.TryIndex(profile.Species, out var species) || !species.HeightAdjustable)
            return 1f;

        return float.IsFinite(profile.Height)
            ? Math.Clamp(profile.Height, species.MinHeight, species.MaxHeight)
            : 1f;
    }
}

[ByRefEvent]
public readonly record struct CharacterHeightChangedEvent;
