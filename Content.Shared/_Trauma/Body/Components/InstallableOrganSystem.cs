using Robust.Shared.Serialization;

using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Shared._Trauma.Body;

public sealed partial class InstallableOrganSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnExamined(Entity<InstallableOrganComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !HasComp<BodyComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("trauma-organ-install-examine"));
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<InstallableOrganComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            !HasComp<BodyComponent>(user) ||
            _body.GetCategory(ent.Owner) is not {} category)
            return;

        if (_body.GetOrgan(user, category) != null)
        {
            _popup.PopupEntity(Loc.GetString("trauma-organ-install-occupied", ("organ", ProtoMan.Index(category).Name)),
                user, user, PopupType.SmallCaution);
            return;
        }

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.Delay,
            new InstallOrganDoAfterEvent(),
            ent,
            used: ent,
            target: user)
        {
            BreakOnDamage = true, // it's brain surgery basically :)
            BreakOnMove = true
        });
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<InstallableOrganComponent> ent, ref InstallOrganDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || !_body.InsertOrgan(user, ent.Owner))
            return;

        _popup.PopupEntity(Loc.GetString("trauma-organ-install-success", ("organ", ent.Owner)),
            user, user, PopupType.Medium);
    }
}

[Serializable, NetSerializable]
public sealed partial class InstallOrganDoAfterEvent : SimpleDoAfterEvent;
