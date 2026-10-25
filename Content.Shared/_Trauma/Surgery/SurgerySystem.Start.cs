using Robust.Shared.Utility;

using Content.Shared._Trauma.Common.CCVar;
using Content.Shared._Trauma.Surgery.Tools;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;

namespace Content.Shared._Trauma.Surgery;

public sealed partial class SurgerySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private EntityQuery<SurgeryTargetComponent> _targetQuery;

    private bool _noSelfOperate;

    private void InitializeStart()
    {
        // cvar is yes var is no, invert it
        Subs.CVar(_cfg, SurgeryCVars.CanOperateOnSelf, x => _noSelfOperate = !x, true);
    }

    private void AttemptStartSurgery(Entity<SurgeryToolComponent> ent, EntityUid user, EntityUid target)
    {
        if (!IsLyingDown(target, user))
            return;

        if (_noSelfOperate && user == target)
        {
            _popup.PopupEntity(Loc.GetString("surgery-error-self-surgery"), user, user);
            return;
        }

        _ui.OpenUi(target, SurgeryUIKey.Key, user);
    }

    [SubscribeLocalEvent]
    private void OnUtilityVerb(Entity<SurgeryToolComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        var target = args.Target;
        if (!args.CanInteract ||
            !args.CanAccess ||
            !_targetQuery.HasComp(target))
            return;

        var user = args.User;

        var verb = new UtilityVerb()
        {
            Act = () => AttemptStartSurgery(ent, user, target),
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Specific/Medical/Surgery/scalpel.rsi"), "scalpel"),
            Text = Loc.GetString("surgery-verb-text"),
            Message = Loc.GetString("surgery-verb-message"),
            DoContactInteraction = true
        };

        args.Verbs.Add(verb);
    }
}
