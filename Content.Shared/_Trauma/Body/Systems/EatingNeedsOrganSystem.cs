using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition;
using Content.Shared.Popups;

namespace Content.Shared._Trauma.Body;

public sealed partial class EatingNeedsOrganSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<EnabledOrganComponent> _enabledQuery;

    [SubscribeLocalEvent]
    private void OnIngestionAttempt(Entity<EatingNeedsOrganComponent> ent, ref IngestionAttemptEvent args)
    {
        if (args.Cancelled || _body.GetOrgan(ent.Owner, ent.Comp.Category) is {} head && _enabledQuery.HasComp(head))
            return;

        var user = args.User ?? ent.Owner;
        var msg = user == ent.Owner
            ? Loc.GetString("trauma-organ-no-mouth")
            : Loc.GetString("trauma-organ-no-mouth-other", ("target", Identity.Entity(ent, EntityManager)));
        _popup.PopupEntity(msg, ent, user);
        args.Cancelled = true;
    }
}
