using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Clothing;
using Content.Shared.Body;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Shared._Trauma.Body;

public sealed partial class BodyEquipmentSystem : EntitySystem
{
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<InventoryComponent> _inventoryQuery;

    /// <summary>
    /// Inventory slots that need each body-part type to be present to be worn.
    /// </summary>
    public static SlotFlags GetRequiringSlots(BodyPartType type) => type switch
    {
        BodyPartType.Head => SlotFlags.EYES | SlotFlags.EARS | SlotFlags.HEAD | SlotFlags.MASK,
        BodyPartType.Torso => SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING | SlotFlags.UNDERWEART | SlotFlags.UNDERWEARB,
        BodyPartType.Hand => SlotFlags.GLOVES | SlotFlags.HANDACCESSORY,
        BodyPartType.Foot => SlotFlags.FEET | SlotFlags.SOCKS,
        _ => SlotFlags.NONE
    };

    /// <summary>
    /// Inventory slots whose contents block access to each body-part type.
    /// </summary>
    public static SlotFlags GetCoveringSlots(BodyPartType type) => type switch
    {
        BodyPartType.Head => SlotFlags.HEAD,
        BodyPartType.Torso => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
        BodyPartType.Arm => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
        BodyPartType.Hand => SlotFlags.GLOVES,
        BodyPartType.Leg => SlotFlags.OUTERCLOTHING | SlotFlags.LEGS,
        BodyPartType.Foot => SlotFlags.FEET,
        _ => SlotFlags.NONE
    };

    private static readonly BodyPartType[] RequiringTypes =
    [
        BodyPartType.Head,
        BodyPartType.Torso,
        BodyPartType.Hand,
        BodyPartType.Foot
    ];

    [SubscribeLocalEvent]
    private void OnEquippingTargetAttempt(Entity<BodyEquipmentComponent> ent, ref IsEquippingTargetAttemptEvent args)
    {
        if (GetRequiredPart(ent.Owner, args.Slot) is not {} part ||
            HasBodyPart(ent.Owner, part))
            return;

        var ident = Identity.Entity(ent, EntityManager);
        var partName = part.ToString().ToLower();
        _popup.PopupEntity(Loc.GetString("equip-part-missing-error",
            ("target", ident), ("part", partName)), args.EquipTarget, args.User);
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnCheckEquipmentPart(Entity<BodyEquipmentComponent> ent, ref CheckEquipmentPartEvent args)
    {
        if (GetRequiredPart(ent.Owner, args.Slot) is not {} part)
        {
            args.Handled = true;
            return;
        }

        if (HasBodyPart(ent, part))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnOrganRemovedFrom(Entity<BodyEquipmentComponent> ent, ref OrganRemovedFromEvent args)
    {
        DropPartItems(ent.Owner, args.Organ.Owner);
    }

    private bool HasBodyPart(EntityUid body, BodyPartType part)
        => _part.FindBodyPart(body, part) != null;

    /// <summary>
    /// Resolves the body-part type required by an inventory slot of the body, if any.
    /// </summary>
    public BodyPartType? GetRequiredPart(EntityUid body, string slot)
    {
        if (!_inventory.TryGetSlot(body, slot, out var definition))
            return null;

        foreach (var type in RequiringTypes)
        {
            if ((GetRequiringSlots(type) & definition.SlotFlags) != SlotFlags.NONE)
                return type;
        }

        return null;
    }

    /// <summary>
    /// Drop items worn by a body part, unless there is another of the same type of part.
    /// </summary>
    public void DropPartItems(Entity<InventoryComponent?> ent, Entity<BodyPartComponent?> part)
    {
        // don't drop for mobs being deleted, gibbing etc would handle it themselves
        if (TerminatingOrDeleted(ent) || !_inventoryQuery.Resolve(ent, ref ent.Comp) ||
            _part.GetPartType(part) is not { } type ||
            // do nothing if there's e.g. a second foot to wear shoes with. there's no individual shoe items.
            _part.GetBodyParts(ent.Owner, type).Count != 0)
            return;

        var slots = GetRequiringSlots(type);
        if (slots == SlotFlags.NONE ||
            !_inventory.TryGetContainerSlotEnumerator(ent.Owner, out var enumerator, slots))
            return;

        while (enumerator.MoveNext(out var slot))
        {
            _inventory.DropSlotContents(ent, slot.ID);
        }
    }
}
