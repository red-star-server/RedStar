using Content.Shared._RedStar.Xenobiology.Slimes;
using Robust.Client.Graphics;

namespace Content.Client._RedStar.Xenobiology.Slimes.Systems;

/// <summary>
/// Installs the renderer for actual entities held in slime stomachs.
/// </summary>
public sealed partial class SlimeDigestionVisualsSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlays = default!;
    private SlimeDigestionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new SlimeDigestionOverlay(EntityManager);
        _overlays.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        _overlays.RemoveOverlay(_overlay);
        base.Shutdown();
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SlimeDigestionVisualsComponent> ent, ref ComponentShutdown args)
        => _overlay.Remove(ent.Owner);
}
