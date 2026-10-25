using System.Numerics;
using Content.Shared._Trauma.Common.CCVar;
using Content.Shared._Trauma.Common.Input;
using Content.Shared._Trauma.Common.MouseWheel;
using Content.Shared._Trauma.Targeting;
using Robust.Client.Input;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Input;

namespace Content.Client._Trauma.MouseWheel;

public sealed partial class MouseWheelSystem : CommonMouseWheelSystem
{
    [Dependency] private IInputManager _inputMan = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private InputSystem _input = default!;
    [Dependency] private SharedTargetingSystem _targeting = default!;
    private bool _target;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, TraumaCVars.MouseWheelTargeting, x => _target = x, true);
    }

    public override void HandleMouseWheel(Vector2 delta)
    {
        if (GetMouseWheelBehaviorPriority(TraumaKeyFunctions.TargetingMod, _target) >= 0)
            _targeting.CycleTargeting(delta.Y > 0f ? 1 : -1);
    }

    private int GetMouseWheelBehaviorPriority(BoundKeyFunction function, bool cvar)
    {
        if (!cvar)
            return -1;

        if (!_inputMan.TryGetKeyBinding(function, out _))
            return 0;

        return _input.CmdStates.GetState(function) == BoundKeyState.Down ? 1 : -1;
    }
}
