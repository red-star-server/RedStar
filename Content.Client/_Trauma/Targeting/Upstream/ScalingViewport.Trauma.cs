using Content.Shared._Trauma.Common.MouseWheel;
using Robust.Client.UserInterface;

namespace Content.Client.Viewport;

public sealed partial class ScalingViewport
{
    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        _entityManager.System<CommonMouseWheelSystem>().HandleMouseWheel(args.Delta);
    }
}
