using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Content.Shared.Popups;
using Content.Shared.Speech;

namespace Content.Shared._Trauma.Body;

public sealed partial class NeedsTongueSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<EnabledOrganComponent> _enabledQuery = default!;

    [SubscribeLocalEvent]
    private void OnSpeakAttempt(Entity<NeedsTongueComponent> ent, ref SpeakAttemptEvent args)
    {
        if (args.Cancelled || _body.GetOrgan(ent.Owner, ent.Comp.Category) is {} tongue && _enabledQuery.HasComp(tongue))
            return;

        // TODO: change to PopupEntity if chat gets predicted
        _popup.PopupEntity(Loc.GetString("trauma-organ-no-tongue"), ent, ent);
        args.Cancel();
    }
}
