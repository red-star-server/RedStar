using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Clothing;

public sealed partial class ClothingGrantingSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnCompEquipClothingGrantComponent(Entity<ClothingGrantComponentComponent> ent, ref GotEquippedEvent args)
        => OnCompEquip(ent.Owner, ent.Comp, args);

    [SubscribeLocalEvent]
    private void OnCompUnequipClothingGrantComponent(Entity<ClothingGrantComponentComponent> ent, ref GotUnequippedEvent args)
        => OnCompUnequip(ent.Owner, ent.Comp, args);

    [SubscribeLocalEvent]
    private void OnTagEquipClothingGrantTag(Entity<ClothingGrantTagComponent> ent, ref GotEquippedEvent args)
        => OnTagEquip(ent.Owner, ent.Comp, args);

    [SubscribeLocalEvent]
    private void OnTagUnequipClothingGrantTag(Entity<ClothingGrantTagComponent> ent, ref GotUnequippedEvent args)
        => OnTagUnequip(ent.Owner, ent.Comp, args);

    private void OnCompEquip(EntityUid uid, ClothingGrantComponentComponent component, GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryComp<ClothingComponent>(uid, out var clothing)) return;

        if (!clothing.Slots.HasFlag(args.SlotFlags)) return;

        var target = args.EquipTarget;
        component.Active.Clear();
        foreach (var name in component.Components.Keys)
        {
            var type = Factory.GetRegistration(name).Type;
            if (!HasComp(target, type))
                component.Active.Add(name);
        }
        EntityManager.AddComponents(target, component.Components);
    }

    private void OnCompUnequip(EntityUid uid, ClothingGrantComponentComponent component, GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var target = args.EquipTarget;
        foreach (var name in component.Active)
        {
            var type = Factory.GetRegistration(name).Type;
            RemComp(target, type);
        }
        component.Active.Clear();
    }


    private void OnTagEquip(EntityUid uid, ClothingGrantTagComponent component, GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryComp<ClothingComponent>(uid, out var clothing))
            return;

        if (!clothing.Slots.HasFlag(args.SlotFlags))
            return;

        var target = args.EquipTarget;
        var tags = EnsureComp<TagComponent>(target);
        var tag = component.Tag;
        component.IsActive = !_tag.HasTag(tags, tag);
        if (component.IsActive)
            _tag.AddTag((target, tags), tag);
    }

    private void OnTagUnequip(EntityUid uid, ClothingGrantTagComponent component, GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!component.IsActive)
            return;

        _tag.RemoveTag(args.EquipTarget, component.Tag);
        component.IsActive = false;
    }
}
