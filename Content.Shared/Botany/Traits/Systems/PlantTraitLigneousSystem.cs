using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components; // RS14
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitLigneousComponent"/>
public sealed partial class PlantTraitLigneousSystem : EntitySystem
{
    [Dependency] private PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantTraySystem _plantTray = default!; // RS14
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    [Dependency] private EntityQuery<PlantHolderComponent> _holderQuery;
    private readonly HashSet<EntityUid> _toolHarvests = []; // RS14

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<PlantTraitLigneousComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_holderQuery.TryComp(ent.Owner, out var holder))
            return;

        TryHarvestWithTool(ent.Owner, ent.Comp, holder, ref args); // RS14
    }

    // RS14-start: tools can target either the plant or its hydroponic tray.
    [SubscribeLocalEvent]
    private void OnTrayInteractUsing(Entity<PlantTrayComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_plantTray.TryGetAlivePlant(ent.AsNullable(), out var plant) ||
            !TryComp<PlantTraitLigneousComponent>(plant, out var ligneous) ||
            !_holderQuery.TryComp(plant, out var holder))
            return;

        TryHarvestWithTool(plant.Value, ligneous, holder, ref args);
    }

    private void TryHarvestWithTool(EntityUid plant, PlantTraitLigneousComponent ligneous,
        PlantHolderComponent holder, ref InteractUsingEvent args)
    {

        if (!holder.ReadyForHarvest)
            return;

        if (_plantHolder.IsDead(plant))
        {
            _popup.PopupCursor(Loc.GetString("plant-component-dead-plant-message"), args.User);
            return;
        }

        var harvestToolQuality = ligneous.HarvestToolQuality;
        if (!HasComp<SharpComponent>(args.Used) &&
            (!harvestToolQuality.HasValue || !_tool.HasQuality(args.Used, harvestToolQuality.Value)))
            return;

        _toolHarvests.Add(plant);
        try
        {
            args.Handled = _plantHarvest.TryHandleHarvest(plant, args.User);
        }
        finally
        {
            _toolHarvests.Remove(plant);
        }
    }
    // RS14-end

    [SubscribeLocalEvent(before: [typeof(PlantHarvestSystem)])]
    private void OnHarvestAttempt(Entity<PlantTraitLigneousComponent> ent, ref PlantHarvestAttemptEvent args)
    {
        if (_toolHarvests.Contains(ent.Owner)) // RS14
            return;

        _popup.PopupCursor(Loc.GetString("plant-component-ligneous-cant-harvest-message"), args.User);
        args.Cancelled = true;
    }
}
