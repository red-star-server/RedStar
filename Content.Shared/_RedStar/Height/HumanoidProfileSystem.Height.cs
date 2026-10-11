using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem
{
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
