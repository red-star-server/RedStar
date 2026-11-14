using Content.Shared._Trauma.Common.CCVar;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Shared._Trauma.Surgery;

public sealed partial class SurgerySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private bool _noSelfOperate;

    private void InitializeStart()
    {
        Subs.CVar(_cfg, SurgeryCVars.CanOperateOnSelf, x => _noSelfOperate = !x, true);
    }

    private void AttemptStartSurgery(EntityUid user, EntityUid target)
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
    private void OnUtilityVerb(Entity<ToolComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !_targetQuery.HasComp(args.Target))
            return;

        var user = args.User;
        var target = args.Target;
        args.Verbs.Add(new UtilityVerb
        {
            Act = () => AttemptStartSurgery(user, target),
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Specific/Medical/Surgery/scalpel.rsi"), "scalpel"),
            Text = Loc.GetString("surgery-verb-text"),
            Message = Loc.GetString("surgery-verb-message"),
            DoContactInteraction = true
        });
    }
}
