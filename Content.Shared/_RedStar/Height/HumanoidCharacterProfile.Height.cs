namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    public HumanoidCharacterProfile WithHeight(float height) => new(this) { Height = height };
}
