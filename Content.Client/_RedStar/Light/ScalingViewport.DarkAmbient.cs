using System.Numerics;
using Content.Client.Overlays;
using Content.Shared.Light;
using Robust.Client.Graphics;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Viewport;

public sealed partial class ScalingViewport
{
    private static readonly ProtoId<ShaderPrototype> DarkAmbientShaderId = "DarkAmbient";
    [Dependency] private IOverlayManager _darkAmbientOverlays = default!;
    [Dependency] private ILightManager _darkAmbientLighting = default!;

    private ShaderInstance? _darkAmbientShader;

    /// <summary>
    /// Only the main game viewport opts in; previews and secondary cameras retain their colors.
    /// </summary>
    public bool ApplyDarkAmbient { get; set; }

    private bool ApplyDarkAmbientShader(IRenderHandle handle)
    {
        if (!ApplyDarkAmbient || _viewport?.Eye is not { DrawLight: true } ||
            !_darkAmbientLighting.Enabled || !_darkAmbientLighting.DrawLighting ||
            _darkAmbientOverlays.HasOverlay<NightVisionOverlay>() ||
            !_cfg.GetCVar(LightAtmosphereCVars.Enabled))
            return false;

        var mapSystem = _entityManager.System<SharedMapSystem>();
        if (!mapSystem.TryGetMap(_viewport.Eye.Position.MapId, out var mapUid) ||
            !_entityManager.TryGetComponent(mapUid.Value, out MapComponent? map) || !map.LightingEnabled)
            return false;

        var strength = Math.Clamp(_cfg.GetCVar(LightAtmosphereCVars.DarkeningStrength), 0, 100) / 100f;
        if (strength <= 0f)
            return false;

        _darkAmbientShader ??= _prototypeManager.Index(DarkAmbientShaderId).InstanceUnique();
        var size = _viewport.RenderTarget.Texture.Size;
        _darkAmbientShader.SetParameter("Intensity", strength);
        _darkAmbientShader.SetParameter("Sharpness", _sharpnessStrength);
        _darkAmbientShader.SetParameter("PixelSize", new Vector2(1f / size.X, 1f / size.Y));
        handle.DrawingHandleScreen.UseShader(_darkAmbientShader);
        return true;
    }

    protected override void ExitedTree()
    {
        _darkAmbientShader?.Dispose();
        _darkAmbientShader = null;
        base.ExitedTree();
    }
}
