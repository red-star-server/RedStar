using Content.Shared._Sirena.Humanoid;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.DetailExaminable;

public sealed partial class DetailExaminableSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetailExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<DetailExaminableComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var detailsRange = _examine.IsInDetailsRange(args.User, ent);

        var user = args.User;

        var verb = new ExamineVerb
        {
            Act = () =>
            {
                var markup = new FormattedMessage();
                // RS14-start
                if (!string.IsNullOrWhiteSpace(ent.Comp.Content))
                {
                    markup.AddMarkupPermissive(ent.Comp.Content);
                    markup.PushNewline();
                }

                AddErpStatus(markup, ent.Comp.ErpStatus);
                // RS14-end
                _examine.SendExamineTooltip(user, ent, markup, false, false);
            },
            Text = Loc.GetString("detail-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("detail-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    // RS14-start
    private void AddErpStatus(FormattedMessage message, ErpStatus status)
    {
        var (locId, color) = status switch
        {
            ErpStatus.Partial => ("humanoid-erp-status-partial", Color.Yellow),
            ErpStatus.Full => ("humanoid-erp-status-full", Color.LimeGreen),
            _ => ("humanoid-erp-status-no", Color.Red),
        };

        message.PushColor(color);
        message.AddText(Loc.GetString(locId));
    }
    // RS14-end
}
