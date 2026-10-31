using System.Numerics;

namespace Content.Shared._Trauma.Common.MouseWheel;

public abstract class CommonMouseWheelSystem : EntitySystem
{
    /// <summary>
    /// Handles the wheel movement used to cycle a medical targeting selection.
    /// </summary>
    public abstract void HandleMouseWheel(Vector2 delta);
}
