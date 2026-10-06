using Content.Server._RedStar.Xenobiology.Components;
using Content.Server._RedStar.Xenobiology.Events;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class XenobiologyDatabaseConsoleSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private XenobiologyResearchSystem _xenobiology = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnOpened(Entity<XenobiologyDatabaseConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(XenobiologyDatabaseConsoleUiKey.Key))
            return;

        ent.Comp.LastState = null;
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnRegistrationChanged(Entity<XenobiologyDatabaseConsoleComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnDatabaseChanged(Entity<XenobiologyDatabaseConsoleComponent> ent, ref XenobiologyResearchDatabaseChangedEvent args)
    {
        UpdateState(ent);
    }

    private void UpdateState(Entity<XenobiologyDatabaseConsoleComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, XenobiologyDatabaseConsoleUiKey.Key))
            return;

        var connected = _research.TryGetClientServer(ent.Owner, out var server, out _) &&
                        HasComp<XenobiologyResearchDatabaseComponent>(server.Value);
        var entries = new List<XenobiologyDatabaseEntry>();
        if (connected && server is { } serverUid)
        {
            foreach (var id in _xenobiology.GetDiscoveredSamples(serverUid))
            {
                if (id.TryGet(out var sample, ProtoMan, Factory))
                    entries.Add(new XenobiologyDatabaseEntry(id, Math.Max(0, sample.ResearchValue)));
            }
        }

        entries.Sort((left, right) => string.CompareOrdinal(left.Sample.Id, right.Sample.Id));
        var state = new XenobiologyDatabaseConsoleUiState(connected, entries.ToArray());
        if (ent.Comp.LastState is { } previous && previous.Connected == state.Connected &&
            previous.Entries.SequenceEqual(state.Entries))
            return;

        ent.Comp.LastState = state;
        _ui.SetUiState(ent.Owner, XenobiologyDatabaseConsoleUiKey.Key, state);
    }
}

