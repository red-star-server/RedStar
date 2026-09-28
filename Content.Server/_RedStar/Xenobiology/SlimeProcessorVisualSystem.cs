using Content.Shared._RedStar.Xenobiology;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class SlimeProcessorVisualSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnProcessingStarted(Entity<ActiveSlimeProcessorComponent> ent, ref ComponentStartup args)
    {
        if (HasComp<AppearanceComponent>(ent))
            _appearance.SetData(ent.Owner, SlimeProcessorVisuals.Processing, true);
    }

    [SubscribeLocalEvent]
    private void OnProcessingStopped(Entity<ActiveSlimeProcessorComponent> ent, ref ComponentShutdown args)
    {
        if (HasComp<AppearanceComponent>(ent))
            _appearance.SetData(ent.Owner, SlimeProcessorVisuals.Processing, false);
    }
}
