using Content.Server.Popups;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class CeruleanExtractSystem : EntitySystem
{
    private static readonly EntProtoId CeruleanAdultPrototype = "XenobiologySlimeCerulean";

    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private EntityQuery<SlimeLifecycleComponent> _lifecycleQuery;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<CeruleanExtractComponent> extract, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target ||
            !_lifecycleQuery.TryComp(target, out var lifecycle) ||
            lifecycle.Stage != SlimeStage.Adult ||
            lifecycle.AdultPrototype == CeruleanAdultPrototype ||
            !_mobState.IsAlive(target) ||
            HasComp<SlimeExtractYieldEnhancedComponent>(target))
            return;

        AddComp<SlimeExtractYieldEnhancedComponent>(target);
        QueueDel(extract.Owner);
        _popup.PopupEntity(Loc.GetString("xenobiology-cerulean-enhanced"), target, args.User);
        args.Handled = true;
    }
}
