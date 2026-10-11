namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool _updatingHeightControls;

    private void InitializeHeightControls()
    {
        HeightSlider.OnValueChanged += _ =>
        {
            if (_updatingHeightControls || Profile is null)
                return;

            var species = _prototypeManager.Index(Profile.Species);
            if (!species.HeightAdjustable)
                return;

            var height = MathF.Round(MathHelper.Lerp(species.MinHeight, species.MaxHeight, HeightSlider.Value), 2);
            if (MathF.Abs(Profile.Height - height) < 0.005f)
                return;

            Profile = Profile.WithHeight(height);
            HeightLabel.Text = FormatHeight(height);
            ReloadProfilePreview();
        };
    }

    private void UpdateHeightControls()
    {
        if (Profile is null)
            return;

        var species = _prototypeManager.Index(Profile.Species);
        HeightRow.Visible = species.HeightAdjustable;
        if (!species.HeightAdjustable)
            return;

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
            HeightSlider.Value = Math.Clamp(percent, 0f, 1f);
        }
        finally
        {
            _updatingHeightControls = false;
        }

        HeightLabel.Text = FormatHeight(height);
    }

    private static string FormatHeight(float height) => $"{height * 100:0}%";
}
