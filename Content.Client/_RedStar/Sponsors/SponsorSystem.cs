using Content.Shared._RedStar.Sponsors;

namespace Content.Client._RedStar.Sponsors;

public sealed partial class SponsorSystem : EntitySystem
{
    [Dependency] private ClientSponsorManager _manager = default!;

    [SubscribeNetworkEvent]
    private void OnSponsorDataChanged(SponsorDataChangedEvent ev)
    {
        _manager.SetData(ev.Data);
    }
}
