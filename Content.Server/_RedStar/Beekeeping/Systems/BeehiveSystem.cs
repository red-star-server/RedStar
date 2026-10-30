using Content.Server.Atmos.EntitySystems;
using Content.Server._RedStar.Beekeeping.Events;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared._RedStar.Beekeeping;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Systems;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// Runs the hive colony: population, nectar, combs, overcrowding, the queenless decline, and the pause in bad
/// air. The colony is a counter, not entities; bees only become entities as items in hand and as short-lived
/// visible foragers.
/// </summary>
public sealed partial class BeehiveSystem : SharedBeehiveSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private BeeSwarmSystem _swarm = default!;
    [Dependency] private BeeErrandSystem _errands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private BeeQueenSystem _queen = default!;
    [Dependency] private BeeScoutSystem _scout = default!;
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private SharedMapSystem _map = default!;

    /// <summary>
    /// How often each hive is updated. The work done scales with the time that actually passed.
    /// </summary>
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Longest stretch one update accounts for, so a long server hitch does not dump minutes of work at once.
    /// </summary>
    private const float MaxUpdateSeconds = 5f;

    private readonly List<Entity<BeeFrameComponent>> _frames = new();

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BeehiveComponent> ent, ref MapInitEvent args)
    {
        var now = Timing.CurTime;
        ent.Comp.LastUpdate = now;
        ent.Comp.NextUpdate = now + UpdateInterval;
        ent.Comp.NextQueenlessDecay = now + ent.Comp.QueenlessDecayInterval;
        UpdateMaxBees(ent);
        UpdateHiveAppearance(ent);
        UpdateAmbience(ent);
        EnsureBeeIcons(ent);
    }

    protected override void OnQueenCaught(EntityUid flyer)
    {
        QueueDel(flyer);
    }

    [SubscribeLocalEvent]
    private void OnFrameMapInit(Entity<BeeFrameComponent> ent, ref MapInitEvent args)
    {
        UpdateFrameAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<BeehiveComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var id = args.Container.ID;
        if (id == ent.Comp.QueenSlot)
        {
            if (HasComp<BeeQueenComponent>(args.Entity))
            {
                // The hive buzzes for the whole colony; the queen inside stays quiet.
                _ambient.SetAmbience(args.Entity, false);
                OnQueenInserted(ent);
                if (TryComp<BeeQueenComponent>(args.Entity, out var inserted))
                    _queen.UpdateQueenVisuals((args.Entity, inserted));
            }
            return;
        }

        if (!ent.Comp.FrameSlots.Contains(id))
            return;

        UpdateMaxBees(ent);
        UpdateHiveAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<BeehiveComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // The hive itself is going away and empties its slots: nothing to update.
        if (TerminatingOrDeleted(ent))
            return;

        var id = args.Container.ID;
        if (id == ent.Comp.QueenSlot)
        {
            ent.Comp.NextQueenlessDecay = Timing.CurTime + ent.Comp.QueenlessDecayInterval;
            if (TryComp<BeeQueenComponent>(args.Entity, out var queen))
            {
                queen.LastHive = ent;
                _ambient.SetAmbience(args.Entity, true);
            }
            return;
        }

        if (!ent.Comp.FrameSlots.Contains(id))
            return;

        UpdateMaxBees(ent);
        UpdateHiveAppearance(ent);
    }

    /// <summary>
    /// A queen goes into a hive only while the map has room for one more hive with a queen.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnHiveInsertAttempt(Entity<BeehiveComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled || args.Slot.ID != ent.Comp.QueenSlot || !HasComp<BeeQueenComponent>(args.Item))
            return;

        var map = Transform(ent).MapUid;
        var active = 0;
        var query = EntityQueryEnumerator<BeehiveComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hive, out var xform))
        {
            if (uid != ent.Owner && xform.MapUid == map && GetQueen((uid, hive)) != null)
                active++;
        }

        if (active < _cfg.GetCVar(BeekeepingCVars.ActiveHiveLimit))
            return;

        args.Cancelled = true;
        if (args.User is { } user)
            _popup.PopupEntity(Loc.GetString("beekeeping-queen-hive-limit"), ent, user);
    }

    /// <summary>
    /// A broken hive forgets its sources and calls its scout back; the rest of what it leaves is up to the systems
    /// that listen to <see cref="BeehiveBrokenEvent"/>.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnHiveDestroyed(Entity<BeehiveComponent> ent, ref DestructionEventArgs args)
    {
        ent.Comp.KnownSources.Clear();
        _scout.RecallScout(ent.Comp);

        var ev = new BeehiveBrokenEvent(ent);
        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    /// A queen put into a truly empty hive starts a colony. With bees still outside nothing happens, so taking
    /// the queen out and back in is not a free restart.
    /// </summary>
    private void OnQueenInserted(Entity<BeehiveComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.Bees != 0 || comp.OutBees != 0)
            return;

        comp.Bees = comp.StartingBees;
        comp.BeeProgress = 0f;
        comp.ScoutState = comp.Bees > 0 ? BeeScoutState.Home : BeeScoutState.Lost;
        comp.ScoutReplaceAt = Timing.CurTime;
        // The hive shows it is alive: the first scout flight starts a few seconds after the queen goes in.
        comp.ScoutFirstTripNow = comp.Bees > 0;
        UpdateAmbience(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<BeehiveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            // A long server hitch is worked off over the next updates instead of being lost: each one takes at most
            // MaxUpdateSeconds of the time that passed and leaves the rest for later.
            var passed = Math.Clamp((float) (now - comp.LastUpdate).TotalSeconds, 0f, MaxUpdateSeconds);
            comp.LastUpdate += TimeSpan.FromSeconds(passed);
            comp.NextUpdate = now + UpdateInterval;
            UpdateHive((uid, comp), passed);
        }
    }

    private void UpdateHive(Entity<BeehiveComponent> hive, float seconds)
    {
        var hasQueen = GetQueen(hive) != null;

        UpdatePause(hive);
        _queen.UpdateColonyTraits(hive, hasQueen);
        UpdatePopulation(hive, hasQueen);

        if (hive.Comp.Pause == BeehivePauseReason.None)
        {
            _scout.UpdateScouting(hive);
            _scout.UpdateVisits(hive, seconds);
            if (hasQueen)
                Produce(hive, seconds);
        }
        else
        {
            // A paused hive does not scout; the trip starts over once the air is fine again.
            hive.Comp.NextScoutTrip = TimeSpan.Zero;
            _scout.RecallScout(hive.Comp);
        }

        _queen.UpdateEgg(hive, hasQueen);
        _errands.UpdatePendingForagers(hive);
        UpdateAmbience(hive);
    }

    #region Population

    private void UpdatePopulation(Entity<BeehiveComponent> hive, bool hasQueen)
    {
        var comp = hive.Comp;
        var now = Timing.CurTime;

        // Without a queen the colony slowly dies out.
        if (hasQueen || comp.Bees <= 0)
        {
            comp.NextQueenlessDecay = now + comp.QueenlessDecayInterval;
        }
        else if (now >= comp.NextQueenlessDecay)
        {
            comp.NextQueenlessDecay = now + comp.QueenlessDecayInterval;
            RemoveHomeBee(hive);
        }

        // Too many bees for the frames: a grace period to bring the frames back, then extra bees leave.
        if (comp.Bees + comp.OutBees <= comp.MaxBees)
        {
            comp.CrowdedSince = null;
            comp.Crowding = BeehiveCrowding.None;
        }
        else
        {
            if (comp.CrowdedSince == null)
            {
                comp.CrowdedSince = now;
                comp.NextCrowdingLeave = now + comp.CrowdingGrace;
            }

            if (now - comp.CrowdedSince.Value < comp.CrowdingGrace)
            {
                comp.Crowding = BeehiveCrowding.Cramped;
            }
            else
            {
                // The moment the grace runs out, the extra bees leave together as a swarm; with an egg in the hive the
                // old queen goes with them and a young one stays. Bees still too many after that leave one by one.
                if (comp.Crowding != BeehiveCrowding.Leaving)
                {
                    if (comp.HasEgg && _swarm.TrySwarm(hive))
                        _queen.EndEgg(hive);
                    else
                        _swarm.LeaveInSwarm(hive, comp.Bees + comp.OutBees - comp.MaxBees);
                }

                comp.Crowding = BeehiveCrowding.Leaving;
                if (now >= comp.NextCrowdingLeave)
                {
                    comp.NextCrowdingLeave = now + comp.CrowdingLeaveInterval;
                    RemoveHomeBee(hive);
                }
            }
        }

        // A worker takes the place of a lost scout once the delay is over and enough bees are home.
        if (comp.ScoutState == BeeScoutState.Lost &&
            now >= comp.ScoutReplaceAt &&
            comp.Bees >= comp.ScoutReplaceMinBees)
        {
            comp.ScoutState = BeeScoutState.Home;
        }
    }

    /// <summary>
    /// Takes one bee from home, a worker if there is one. If the last bee was the scout, the scout is lost.
    /// </summary>
    public bool RemoveHomeBee(Entity<BeehiveComponent> hive)
    {
        var comp = hive.Comp;
        if (comp.Bees <= 0)
            return false;

        var scoutLeft = WorkersAtHome(comp) == 0 && comp.ScoutState == BeeScoutState.Home;
        comp.Bees--;
        if (scoutLeft)
        {
            comp.ScoutState = BeeScoutState.Lost;
            comp.ScoutReplaceAt = Timing.CurTime;
        }

        return true;
    }

    public void UpdateMaxBees(Entity<BeehiveComponent> hive)
    {
        GetFrames(hive, _frames);
        var max = hive.Comp.Wild ? hive.Comp.WildMaxBees : _frames.Count * hive.Comp.BeesPerFrame;
        if (hive.Comp.MaxBees == max)
            return;

        hive.Comp.MaxBees = max;
    }

    #endregion

    #region Production

    public void AddNectar(Entity<BeehiveComponent> hive, float amount)
    {
        hive.Comp.Nectar = Math.Clamp(hive.Comp.Nectar + amount, 0f, hive.Comp.MaxNectar);
    }

    /// <summary>
    /// Nectar goes into new bees until the hive is at its limit, then into combs. Combs fill the frames one by
    /// one in slot order.
    /// </summary>
    private void Produce(Entity<BeehiveComponent> hive, float seconds)
    {
        var comp = hive.Comp;
        var traits = GetHiveTotals(hive);

        // The queen lays a bee when she is ready and there is room; whatever nectar is left goes into combs.
        if (comp.Bees + comp.OutBees < comp.MaxBees && Timing.CurTime >= comp.NextLay)
        {
            var beeCost = comp.BeeCost * traits.BeeCost;
            var take = Math.Min(beeCost - comp.BeeProgress, comp.Nectar);
            if (take > 0f)
            {
                comp.BeeProgress += take;
                comp.Nectar -= take;
            }

            if (comp.BeeProgress >= beeCost)
            {
                comp.BeeProgress -= beeCost;
                comp.Bees++;
                // The colony has no scout: the new bee is born one.
                if (comp.ScoutState == BeeScoutState.Lost)
                    comp.ScoutState = BeeScoutState.Home;
                var rate = Math.Max(traits.LayRate, 0.01f);
                comp.NextLay = Timing.CurTime + comp.LayInterval / rate;
            }
        }

        var workers = WorkersAtHome(comp);
        var combCost = comp.CombCost * traits.CombCost;
        if (workers <= 0 || combCost <= 0f)
            return;

        GetFrames(hive, _frames);
        if (comp.Wild ? comp.WildCombs >= comp.MaxWildCombs : !HasRoomForComb(_frames))
            return;

        var made = Math.Min(workers * comp.CombsPerWorkerPerMinute * traits.CombRate * seconds / 60f,
            comp.Nectar / combCost);
        if (made <= 0f)
            return;

        comp.CombProgress += made;
        comp.Nectar -= made * combCost;

        var changed = false;
        while (comp.CombProgress >= 1f && (comp.Wild ? AddWildComb(hive) : AddComb(hive, _frames)))
        {
            comp.CombProgress -= 1f;
            changed = true;
        }

        // Nowhere left to put a finished comb: keep it ready for the next empty frame.
        comp.CombProgress = Math.Min(comp.CombProgress, 1f);

        if (changed)
            UpdateHiveAppearance(hive);
    }

    /// <summary>
    /// Whether the colony is growing: there is a queen, the hive works, workers are at home, there is room for more
    /// bees and nectar is in stock or on its way.
    /// </summary>
    public bool IsBroodGoing(Entity<BeehiveComponent> hive)
    {
        var comp = hive.Comp;
        return GetQueen(hive) != null &&
               comp.Pause == BeehivePauseReason.None &&
               WorkersAtHome(comp) > 0 &&
               comp.Bees + comp.OutBees < comp.MaxBees &&
               (comp.Nectar > 0f || comp.BeeProgress > 0f || comp.OutBees > 0);
    }

    /// <summary>
    /// What the hive's nectar goes into right now, the same order <see cref="Produce"/> uses.
    /// </summary>
    public ApiaryNectarUse GetNectarUse(Entity<BeehiveComponent> hive)
    {
        var comp = hive.Comp;
        if (GetQueen(hive) == null)
            return ApiaryNectarUse.Idle;

        GetFrames(hive, _frames);
        if (!comp.Wild && _frames.Count == 0)
            return ApiaryNectarUse.NoFrames;

        var growing = comp.Bees + comp.OutBees < comp.MaxBees;
        var room = comp.Wild ? comp.WildCombs < comp.MaxWildCombs : HasRoomForComb(_frames);
        var workers = WorkersAtHome(comp) > 0;

        if (growing)
            return room && workers ? ApiaryNectarUse.BeesAndCombs : ApiaryNectarUse.Bees;

        if (!room)
            return ApiaryNectarUse.FramesFull;

        return workers ? ApiaryNectarUse.Combs : ApiaryNectarUse.NoWorkers;
    }

    private static bool HasRoomForComb(List<Entity<BeeFrameComponent>> frames)
    {
        foreach (var frame in frames)
        {
            if (frame.Comp.Combs < frame.Comp.MaxCombs)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Adds one comb to the first frame that is not full.
    /// </summary>
    private bool AddComb(Entity<BeehiveComponent> hive, List<Entity<BeeFrameComponent>> frames)
    {
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames[i];
            if (frame.Comp.Combs >= frame.Comp.MaxCombs)
                continue;

            frame.Comp.Combs++;
            // New honey on a frame whose caps were cut: it is filling again and caps when full.
            frame.Comp.Uncapped = false;
            _queen.TakeCombPayload(hive, frame.Comp);
            Dirty(frame);
            UpdateFrameAppearance(frame);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Coarse nectar stock, for the examine text.
    /// </summary>
    public static BeehiveNectarLevel GetNectarLevel(BeehiveComponent comp)
    {
        var fraction = comp.MaxNectar > 0f ? comp.Nectar / comp.MaxNectar : 0f;
        return fraction >= comp.NectarHighFraction ? BeehiveNectarLevel.High
            : fraction >= comp.NectarMediumFraction ? BeehiveNectarLevel.Medium
            : fraction >= comp.NectarLowFraction ? BeehiveNectarLevel.Low
            : BeehiveNectarLevel.Empty;
    }

    #endregion

    #region Examine

    [SubscribeLocalEvent]
    private void OnHiveExamined(Entity<BeehiveComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        var frames = 0;
        foreach (var slot in comp.FrameSlots)
        {
            if (ItemSlots.GetItemOrNull(ent.Owner, slot) != null)
                frames++;
        }

        var hasQueen = GetQueen(ent) != null;

        using (args.PushGroup(nameof(BeehiveComponent)))
        {
            args.PushMarkup(Loc.GetString(hasQueen
                ? "beekeeping-hive-examine-queen"
                : "beekeeping-hive-examine-no-queen"));
            if (!comp.Wild)
            {
                args.PushMarkup(Loc.GetString("beekeeping-hive-examine-frames",
                    ("count", frames), ("max", comp.FrameSlots.Count)));
            }

            if (comp.Bees > 0 || comp.OutBees > 0)
            {
                args.PushMarkup(Loc.GetString("beekeeping-hive-examine-bees",
                    ("bees", comp.Bees), ("max", comp.MaxBees), ("outside", comp.OutBees)));
            }

            args.PushMarkup(Loc.GetString("beekeeping-hive-examine-nectar",
                ("level", GetNectarLevel(comp).ToString())));

            if (comp.Wild && comp.SwarmModifier is { } modifierId && Proto.TryIndex(modifierId, out var modifier))
            {
                args.PushMarkup(Loc.GetString("beekeeping-hive-examine-swarm-modifier",
                    ("color", modifier.Color.ToHex()),
                    ("name", Loc.GetString(modifier.Name)),
                    ("desc", Loc.GetString(modifier.Description))));
            }

            switch (comp.Crowding)
            {
                case BeehiveCrowding.Cramped:
                    args.PushMarkup(Loc.GetString("beekeeping-hive-examine-cramped"));
                    break;
                case BeehiveCrowding.Leaving:
                    args.PushMarkup(Loc.GetString("beekeeping-hive-examine-leaving"));
                    break;
            }

            if (!hasQueen && comp.Bees > 0)
                args.PushMarkup(Loc.GetString("beekeeping-hive-examine-dying"));

            if (comp.HasEgg)
            {
                args.PushMarkup(Loc.GetString("beekeeping-hive-examine-egg"));
                if (comp.EggDiesAt - Timing.CurTime <= comp.EggDyingWarning)
                    args.PushMarkup(Loc.GetString("beekeeping-hive-examine-egg-dying"));
            }

            if (comp.Bees > 0 || comp.OutBees > 0)
                PushScoutLine(comp, ref args);

            if (InCalmingVapor(ent, comp))
                args.PushMarkup(Loc.GetString("beekeeping-hive-examine-vapor"));

            if (comp.Pause != BeehivePauseReason.None)
            {
                args.PushMarkup(Loc.GetString("beekeeping-hive-examine-paused",
                    ("reason", comp.Pause.ToString())));
            }
        }
    }

    private void PushScoutLine(BeehiveComponent comp, ref ExaminedEvent args)
    {
        switch (comp.ScoutState)
        {
            case BeeScoutState.Outside:
                args.PushMarkup(Loc.GetString("beekeeping-hive-examine-scout-outside"));
                break;
            case BeeScoutState.Lost:
                if (comp.Bees < comp.ScoutReplaceMinBees)
                {
                    args.PushMarkup(Loc.GetString("beekeeping-hive-examine-scout-lost-grow"));
                    break;
                }

                var left = comp.ScoutReplaceAt - Timing.CurTime;
                if (left < TimeSpan.Zero)
                    left = TimeSpan.Zero;

                args.PushMarkup(Loc.GetString("beekeeping-hive-examine-scout-lost",
                    ("minutes", (int) left.TotalMinutes), ("seconds", left.Seconds)));
                break;
        }
    }

    #endregion

    #region Air and sound

    /// <summary>
    /// Bees stop working in bad air and in fire, like plants in a tray read their air.
    /// </summary>
    private void UpdatePause(Entity<BeehiveComponent> hive)
    {
        hive.Comp.Pause = AirFor(hive.Comp, hive);
    }

    /// <summary>
    /// Why bees of this hive could not work in the air where <paramref name="uid"/> is: fire, or pressure or temperature
    /// out of the hive's range. The hive itself pauses on it, and its bees do not fly into such air.
    /// </summary>
    public BeehivePauseReason AirFor(BeehiveComponent comp, EntityUid uid)
    {
        var xform = Transform(uid);
        if (xform.GridUid is { } grid && xform.ParentUid == grid && TryComp<MapGridComponent>(grid, out var gridComp) &&
            _atmos.IsHotspotActive(grid, _map.TileIndicesFor(grid, gridComp, xform.Coordinates)))
        {
            return BeehivePauseReason.Fire;
        }

        var air = _atmos.GetContainingMixture((uid, xform), true) ?? GasMixture.SpaceGas;
        if (air.Pressure < comp.MinPressure)
            return BeehivePauseReason.Vacuum;
        if (air.Pressure > comp.MaxPressure)
            return BeehivePauseReason.HighPressure;
        if (air.Temperature < comp.MinTemperature)
            return BeehivePauseReason.Cold;
        if (air.Temperature > comp.MaxTemperature)
            return BeehivePauseReason.Heat;

        return BeehivePauseReason.None;
    }

    /// <summary>
    /// Water vapor in the air calms bees: a hive standing in it lets no angry bees out, an angry bee in it calms down and
    /// a bee in hand does not sting. The colony keeps working; this is how a botanist handles a vicious hive.
    /// </summary>
    public bool InCalmingVapor(EntityUid uid, BeehiveComponent? comp = null)
    {
        var threshold = comp?.CalmVaporMoles ?? DefaultCalmVaporMoles;
        var air = _atmos.GetContainingMixture(uid, true);
        return air != null && air.GetMoles(Gas.WaterVapor) >= threshold;
    }

    /// <summary>
    /// Water vapor a bee out of any hive (an angry bee, a bee in hand) needs around it to calm down, in moles on its tile.
    /// </summary>
    public const float DefaultCalmVaporMoles = 1f;

    /// <summary>
    /// The hive hums while there are bees at home and they are not paused.
    /// </summary>
    public void UpdateAmbience(Entity<BeehiveComponent> hive)
    {
        _ambient.SetAmbience(hive, hive.Comp.Bees > 0 && hive.Comp.Pause == BeehivePauseReason.None);
    }

    #endregion

    #region Appearance

    /// <summary>
    /// A wild hive keeps its combs inside, up to its limit.
    /// </summary>
    private bool AddWildComb(Entity<BeehiveComponent> hive)
    {
        if (hive.Comp.WildCombs >= hive.Comp.MaxWildCombs)
            return false;

        hive.Comp.WildCombs++;
        _queen.TakeCombPayload(hive, null);

        return true;
    }

    /// <summary>
    /// A wild hive has no frames: it shows its honey once it is full enough.
    /// </summary>
    public override void UpdateHiveAppearance(Entity<BeehiveComponent> hive)
    {
        if (!hive.Comp.Wild)
        {
            base.UpdateHiveAppearance(hive);
            return;
        }

        var shown = hive.Comp.MaxWildCombs > 0 &&
                    hive.Comp.WildCombs >= hive.Comp.MaxWildCombs * hive.Comp.WildHoneyShown;
        Appearance.SetData(hive, WildBeehiveVisuals.Honey, shown);
    }

    public void UpdateFrameAppearance(Entity<BeeFrameComponent> frame)
    {
        UpdateFrameFill(frame);
        Appearance.SetData(frame, BeeFrameVisuals.Color, GetCombColor(frame.Comp));
    }

    /// <summary>
    /// The honey color mixed with the reagents the frame's combs took from the queen, each by its amount.
    /// </summary>
    private Color GetCombColor(BeeFrameComponent frame)
    {
        var honey = Math.Max(frame.Combs, 1) * frame.HoneyPerComb;
        var r = frame.HoneyColor.R * honey;
        var g = frame.HoneyColor.G * honey;
        var b = frame.HoneyColor.B * honey;
        var total = honey;
        foreach (var (reagent, quantity) in frame.Payload.Contents)
        {
            if (!Proto.TryIndex<ReagentPrototype>(reagent.Prototype, out var proto))
                continue;

            var amount = quantity.Float();
            r += proto.SubstanceColor.R * amount;
            g += proto.SubstanceColor.G * amount;
            b += proto.SubstanceColor.B * amount;
            total += amount;
        }

        return new Color(r / total, g / total, b / total);
    }

    #endregion
}
