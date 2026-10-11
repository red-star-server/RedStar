using Content.Shared.Humanoid.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool _updatingHeightControls;

    private void InitializeHeightControls()
    {
        CDHeightSlider.OnValueChanged += _ =>
        {
            if (_updatingHeightControls || Profile is null)
                return;

            var species = _prototypeManager.Index(Profile.Species);
            var height = MathF.Round(MathHelper.Lerp(species.MinHeight, species.MaxHeight, CDHeightSlider.Value), 2);
            if (MathF.Abs(Profile.Height - height) < 0.005f)
                return;

            Profile = Profile.WithHeight(height);
            CDHeightLabel.Text = FormatHeight(height, species);
            ReloadProfilePreview();
        };
    }

    private void UpdateHeightControls()
    {
        if (Profile is null)
            return;

        var species = _prototypeManager.Index(Profile.Species);
        var height = float.IsFinite(Profile.Height) ? Profile.Height : 1f;
        height = Math.Clamp(height, species.MinHeight, species.MaxHeight);
        if (MathF.Abs(Profile.Height - height) >= 0.005f || !float.IsFinite(Profile.Height))
            Profile = Profile.WithHeight(height);

        var percent = species.MaxHeight > species.MinHeight
            ? (height - species.MinHeight) / (species.MaxHeight - species.MinHeight)
            : 0f;

        _updatingHeightControls = true;
        try
        {
            CDHeightSlider.Value = Math.Clamp(percent, 0f, 1f);
        }
        finally
        {
            _updatingHeightControls = false;
        }

        CDHeightLabel.Text = FormatHeight(height, species);
    }

    private static string FormatHeight(float height, SpeciesPrototype species)
    {
        var centimeters = (int)Math.Round(height * species.BaseHeightCm);
        var inches = (int)Math.Round(centimeters / 2.54);
        return $"{centimeters}cm ({inches / 12}'{inches % 12}\")";
    }
}
