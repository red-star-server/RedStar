using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Shared.Audio;
using Content.Shared.Tools.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// While a meat colony lives on a grid, every vent of that grid hums with bees, so a player who stands by a vent can
/// hear that something lives in the pipes. The vent's own ambient sound plays the buzz for that time (the client plays
/// it by distance), so the server does nothing per tick: a pass over the vents every few seconds switches the sound
/// where it is due and puts the old one back where it is not. Welded vents stay quiet, as no bee can pass them.
/// </summary>
public sealed partial class BeeVentHumSystem : EntitySystem
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private WeldableSystem _weldable = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _grids = new();
    private readonly List<EntityUid> _quiet = new();
    private TimeSpan _next;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _next)
            return;

        _next = now + Interval;

        // Grids with a meat colony that is alive.
        _grids.Clear();
        var hives = EntityQueryEnumerator<BeehiveComponent, TransformComponent>();
        while (hives.MoveNext(out _, out var hive, out var hiveXform))
        {
            if (hive.Wild && hive.Diet == BeehiveDiet.Meat && hive.Bees + hive.OutBees > 0 && hiveXform.GridUid is { } grid)
                _grids.Add(grid);
        }

        // Vents that hum and should not any more go quiet: no colony on the grid, or welded shut.
        _quiet.Clear();
        var humming = EntityQueryEnumerator<BeeVentHumComponent, TransformComponent>();
        while (humming.MoveNext(out var vent, out _, out var xform))
        {
            if (!ShouldHum(vent, xform))
                _quiet.Add(vent);
        }

        foreach (var vent in _quiet)
        {
            StopHum(vent);
        }

        if (_grids.Count == 0)
            return;

        // The vents that should hum now start.
        var vents = EntityQueryEnumerator<GasVentPumpComponent, AmbientSoundComponent, TransformComponent>();
        while (vents.MoveNext(out var vent, out _, out var ambient, out var xform))
        {
            if (!HasComp<BeeVentHumComponent>(vent) && ShouldHum(vent, xform))
                StartHum((vent, ambient));
        }
    }

    private bool ShouldHum(EntityUid vent, TransformComponent xform)
    {
        return xform.GridUid is { } grid && _grids.Contains(grid) && !_weldable.IsWelded(vent);
    }

    private void StartHum(Entity<AmbientSoundComponent> vent)
    {
        var hum = AddComp<BeeVentHumComponent>(vent);
        hum.OldSound = vent.Comp.Sound;
        hum.OldVolume = vent.Comp.Volume;
        hum.OldRange = vent.Comp.Range;
        _ambient.SetSound(vent, hum.Sound, vent.Comp);
        _ambient.SetVolume(vent, hum.Volume, vent.Comp);
        _ambient.SetRange(vent, hum.Range, vent.Comp);
    }

    private void StopHum(EntityUid vent)
    {
        if (!TryComp<BeeVentHumComponent>(vent, out var hum))
            return;

        if (TryComp<AmbientSoundComponent>(vent, out var ambient) && hum.OldSound is { } sound)
        {
            _ambient.SetSound(vent, sound, ambient);
            _ambient.SetVolume(vent, hum.OldVolume, ambient);
            _ambient.SetRange(vent, hum.OldRange, ambient);
        }

        RemComp<BeeVentHumComponent>(vent);
    }
}
