using Content.Shared.Inventory;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;

namespace Content.Shared.PowerCell;

/// <summary>
/// Trauma - provide <see cref="FindBattery"/> that uses <see cref="FindBatteryEvent"/>.
/// </summary>
public sealed partial class PowerCellSystem
{
    [Dependency] private InventorySystem _inventory = default!;

    /// <summary>
    /// Updates the cell draw rate and refreshes the connected battery charge rate.
    /// </summary>
    public void SetDrawRate(Entity<PowerCellDrawComponent?> ent, float rate)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.DrawRate == rate)
            return;

        ent.Comp.DrawRate = rate;
        Dirty(ent, ent.Comp);
        if (TryGetBatteryFromSlot(ent.Owner, out var battery))
            _battery.RefreshChargeRate(battery.Value.AsNullable());
    }

    private Entity<BatteryComponent>? FindBattery(EntityUid uid)
    {
        var ev = new FindBatteryEvent();
        RaiseLocalEvent(uid, ref ev);
        // only relay to equipment if no battery was found in the entity itself.
        if (ev.FoundBattery == null && TryComp<InventoryComponent>(uid, out var inventory))
            _inventory.RelayEvent((uid, inventory), ref ev);

        return ev.FoundBattery;
    }
}

/// <summary>
/// Raised on an entity to find a battery to charge etc.
/// It gets raised on the entity itself then, if no battery was found, relayed to equipped items.
/// </summary>
/// <remarks>
/// This can't be in common since it needs Content.Shared.Inventory and Content.Shared.Power.Components
/// </remarks>
[ByRefEvent]
public record struct FindBatteryEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    public Entity<BatteryComponent>? FoundBattery;
}
