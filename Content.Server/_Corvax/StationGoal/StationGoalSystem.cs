using Content.Server._RedStar.Paperwork;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Fax;
using Content.Server.MassMedia.Systems;
using Content.Shared._Corvax.CCCVars;
using Content.Shared.Cargo.Components;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Corvax.StationGoal;

/// <summary>
/// Handles station goals and their associated paperwork and supplies.
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
            StationGoalPrototype? selectedGoal = null;

            while (tempGoals.Count > 0)
            {
                var goalId = _random.Pick(tempGoals);
                var goal = _proto.Index(goalId);

                if (playerCount > goal.MaxPlayers ||
                    playerCount < goal.MinPlayers)
                {
                    tempGoals.Remove(goalId);
                    continue;
                }

                selectedGoal = goal;
                break;
            }

            if (selectedGoal is null)
                return;

            if (SendStationGoal(uid, selectedGoal))
                Log.Info($"Goal {selectedGoal.ID} has been sent to station {MetaData(uid).EntityName}");
        }
    }

    public bool SendStationGoal(EntityUid station, ProtoId<StationGoalPrototype> goal)
    {
        return SendStationGoal(station, _proto.Index(goal));
    }

    /// <summary>
    /// Sends a station goal to all faxes authorized to receive it.
    /// </summary>
    /// <returns>True if at least one station fax received the goal.</returns>
    private bool SendStationGoal(EntityUid station, StationGoalPrototype goal)
    {
        var stationName = MetaData(station).EntityName;

        var goalText = Loc.GetString(
            goal.Text,
            ("station", stationName));

        var paperwork = _proto.Index(goal.Paperwork);
        var paperPrototype = _proto.Index(paperwork.PaperPrototype);

        var printout = new FaxPrintout(
            _paperwork.Render(station, paperwork, goalText),
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
                !(fax.ReceiveStationGoal && _station.GetOwningStation(faxUid) == station))
                continue;

            _fax.Receive(faxUid, printout, null, fax);

            wasSent |= fax.ReceiveStationGoal;
        }

        if (!wasSent)
            return false;

        PublishStationGoalNews(station, goalText);
        TryDeliverStartingEquipment(station, goal);

        return true;
    }

    /// <summary>
    /// Delivers equipment associated with the station goal through the cargo system.
    /// </summary>
    private void TryDeliverStartingEquipment(EntityUid station, StationGoalPrototype goal)
    {
        if (goal.StartingEquipment.Count == 0 ||
            !TryComp<StationCargoOrderDatabaseComponent>(station, out var cargoDb) ||
            !TryComp<StationDataComponent>(station, out var stationData) ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        foreach (var (productId, amount) in goal.StartingEquipment)
        {
            if (amount <= 0)
                continue;

            var product = _proto.Index(productId);

            _cargo.AddAndApproveOrder(
                station,
                product,
                amount,
                Loc.GetString("station-goal-cargo-sender"),
                Loc.GetString("station-goal-cargo-description"),
                Loc.GetString("station-goal-cargo-destination"),
                cargoDb,
                bank.PrimaryAccount,
                (station, stationData));
        }
    }

    /// <summary>
    /// Publishes a news article about the station goal.
    /// </summary>
    private void PublishStationGoalNews(EntityUid station, string content)
    {
        var stationName = MetaData(station).EntityName;

        var title = Loc.GetString(
            "station-goal-news-title",
            ("station", stationName));

        _news.TryAddNews(
            station,
            title,
            content,
            out _,
            Loc.GetString("station-goal-news-author"));
    }
}
