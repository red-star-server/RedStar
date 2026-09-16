using Content.Server._RedStar.Paperwork;
using Content.Server.Cargo.Systems;
using Content.Server.Fax;
using Content.Server.MassMedia.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Corvax.CCCVars;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Corvax.StationGoal;

/// <summary>
/// System to spawn paper with station goal.
/// </summary>
public sealed partial class StationGoalSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private FaxSystem _fax = default!;
    [Dependency] private NewsSystem _news = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private PaperworkSystem _paperwork = default!;

    [SubscribeLocalEvent]
    private void OnRoundStarted(RoundStartedEvent ev)
    {
        if (!_cfg.GetCVar(CCCVars.StationGoal))
            return;

        var playerCount = _playerManager.PlayerCount;

        var query = EntityQueryEnumerator<StationGoalComponent>();
        while (query.MoveNext(out var uid, out var station))
        {
            var tempGoals = new List<ProtoId<StationGoalPrototype>>(station.Goals);
            StationGoalPrototype? selGoal = null;

            while (tempGoals.Count > 0)
            {
                var goalId = _random.Pick(tempGoals);
                var goalProto = _proto.Index(goalId);

                if (playerCount > goalProto.MaxPlayers ||
                    playerCount < goalProto.MinPlayers)
                {
                    tempGoals.Remove(goalId);
                    continue;
                }

                selGoal = goalProto;
                break;
            }

            if (selGoal is null)
                return;

            if (SendStationGoal(uid, selGoal))
                Log.Info($"Goal {selGoal.ID} has been sent to station {MetaData(uid).EntityName}");
        }
    }

    public bool SendStationGoal(EntityUid ent, ProtoId<StationGoalPrototype> goal)
    {
        return SendStationGoal(ent, _proto.Index(goal));
    }

    /// <summary>
    /// Send a station goal on selected station to all faxes which are authorized to receive it.
    /// </summary>
    /// <returns>True if at least one fax received paper.</returns>
    private bool SendStationGoal(EntityUid ent, StationGoalPrototype goal)
    {
        var stationName = MetaData(ent).EntityName;

        var goalText = Loc.GetString(
            goal.Text,
            ("station", stationName));

        var paperwork = _proto.Index(goal.Paperwork);
        var paperPrototype = _proto.Index(paperwork.PaperPrototype);

        var printout = new FaxPrintout(
            _paperwork.Render(ent, paperwork, goalText),
            paperPrototype.Name,
            null,
            paperwork.PaperPrototype,
            null,
            []
        );

        var wasSent = false;

        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var faxUid, out var fax))
        {
            if (!fax.ReceiveAllStationGoals &&
                !(fax.ReceiveStationGoal && _station.GetOwningStation(faxUid) == ent))
                continue;

            _fax.Receive(faxUid, printout, null, fax);

            wasSent |= fax.ReceiveStationGoal;
        }

        // Publish news if at least one fax received the goal.
        if (!wasSent)
            return false;

        PublishStationGoalNews(ent, goalText);
        TryDeliverGoalCargo(ent, goal);

        return true;
    }

    /// <summary>
    /// Delivers the items required by a station goal to a free incoming cargo pallet.
    /// </summary>
    private void TryDeliverGoalCargo(EntityUid station, StationGoalPrototype goal)
    {
        if (goal.Spawns.Count == 0 ||
            !_cargo.TryGetCargoDeliveryCoordinates(station, 1, out _, out var coordinates))
            return;

        var deliveryCoordinates = coordinates[0];

        foreach (var spawnEnt in goal.Spawns)
        {
            SpawnAtPosition(spawnEnt, deliveryCoordinates);
        }
    }

    /// <summary>
    /// Publishes a news article about the station goal in the mass media.
    /// </summary>
    private void PublishStationGoalNews(EntityUid ent, string content)
    {
        var stationName = MetaData(ent).EntityName;

        var title = Loc.GetString(
            "station-goal-news-title",
            ("station", stationName));

        _news.TryAddNews(
            ent,
            title,
            content,
            out _,
            Loc.GetString("station-goal-news-author"));
    }
}
