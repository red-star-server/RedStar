using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Events;
using Robust.Shared.Utility;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// The queen egg. A colony that stays full for a while lays a second queen's egg (bees taken out count as part of it,
/// so only losing bees resets the wait, not taking them). Royal jelly can be
/// collected from it into a container in hand; left alone, the egg dies. Either way no new egg comes for a while.
/// </summary>
public sealed partial class BeeQueenSystem
{
    public void UpdateEgg(Entity<BeehiveComponent> hive, bool hasQueen)
    {
        var comp = hive.Comp;
        var now = _timing.CurTime;

        if (comp.HasEgg)
        {
            // The egg lives on its own timer, paused hive or not.
            if (now >= comp.EggDiesAt)
            {
                // Nobody collected the jelly: the old queen may swarm out and leave the young one behind.
                _swarm.TrySwarm(hive);
                EndEgg(hive);
            }
            return;
        }

        var ready = hasQueen &&
                    comp.Pause == BeehivePauseReason.None &&
                    comp.MaxBees > 0 &&
                    comp.Bees + comp.OutBees >= comp.MaxBees &&
                    now >= comp.EggCooldownUntil;
        if (!ready)
        {
            comp.EggFullSince = null;
            return;
        }

        var traits = _hive.GetHiveTotals(hive);
        comp.EggFullSince ??= now;
        if (now - comp.EggFullSince < comp.EggFullTime * traits.EggWait)
            return;

        comp.EggFullSince = null;
        comp.HasEgg = true;
        comp.EggDiesAt = now + comp.EggLifetime * traits.EggLife;
    }

    /// <summary>
    /// The egg is gone, collected or dead; the next one waits for the cooldown.
    /// </summary>
    public void EndEgg(Entity<BeehiveComponent> hive)
    {
        hive.Comp.HasEgg = false;
        hive.Comp.EggCooldownUntil = _timing.CurTime +
                                     hive.Comp.EggCooldown * _hive.GetHiveTotals(hive).EggCooldown;
    }

    /// <summary>
    /// "Collect royal jelly" in the hive's alternative menu, with a container in hand that has room.
    /// </summary>
    public void AddJellyVerb(Entity<BeehiveComponent> hive, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!hive.Comp.HasEgg || args.Using is not { } container ||
            !TryComp<RefillableSolutionComponent>(container, out var refillable))
        {
            return;
        }

        var user = args.User;
        var hasRoom = _solutions.TryGetSolution(container, refillable.Solution, out _, out var solution) &&
                      solution.AvailableVolume >= FixedPoint2.New(hive.Comp.JellyAmount);

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("beekeeping-verb-collect-jelly"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/spill.svg.192dpi.png")),
            Priority = BeehiveSystem.FramePriority + 1,
            Disabled = !hasRoom,
            Message = hasRoom ? null : Loc.GetString("beekeeping-jelly-no-room"),
            Act = () => _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, hive.Comp.JellyDelay,
                new BeeJellyDoAfterEvent(), hive, target: hive, used: container)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            }),
        });
    }

    [SubscribeLocalEvent]
    private void OnJellyCollected(Entity<BeehiveComponent> hive, ref BeeJellyDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } container)
            return;

        args.Handled = true;

        if (!hive.Comp.HasEgg ||
            !TryComp<RefillableSolutionComponent>(container, out var refillable) ||
            !_solutions.TryGetSolution(container, refillable.Solution, out var soln, out var solution) ||
            solution.AvailableVolume < FixedPoint2.New(hive.Comp.JellyAmount))
        {
            return;
        }

        var jelly = new Solution(hive.Comp.JellyReagent, FixedPoint2.New(hive.Comp.JellyAmount));
        _solutions.TryAddSolution(soln.Value, jelly);
        EndEgg(hive);

        _popup.PopupEntity(Loc.GetString("beekeeping-jelly-collected"), hive, args.User);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):user} collected royal jelly from {ToPrettyString(hive):hive}");
    }
}
