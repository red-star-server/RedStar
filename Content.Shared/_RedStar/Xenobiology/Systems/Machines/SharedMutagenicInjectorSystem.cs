using Content.Shared.Climbing.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Robust.Shared.Containers;

namespace Content.Shared._RedStar.Xenobiology.Systems.Machines;

/// <summary>
/// Handles recipient insertion, ejection, and door visuals on both client and server.
/// </summary>
public abstract partial class SharedMutagenicInjectorSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] private ClimbSystem _climb = default!;

    /// <summary>
    /// Creates the body container and opens the door.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<CellMutagenicInjectorComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BodyContainer = _container.EnsureContainer<ContainerSlot>(ent, CellMutagenicInjectorComponent.BodyContainerId);
        UpdateDoorVisual(ent, false);
    }

    /// <summary>
    /// Shows green highlight if the dragged mob can be accepted.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCanDropTarget(Entity<CellMutagenicInjectorComponent> ent, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop = CanAccept(ent, args.Dragged);
    }

    /// <summary>
    /// True if the injector is empty and the dragged entity can receive cells.
    /// </summary>
    private bool CanAccept(Entity<CellMutagenicInjectorComponent> ent, EntityUid dragged)
    {
        return ent.Comp.BodyContainer.ContainedEntity is null && IsValidRecipient(dragged);
    }

    protected bool IsValidRecipient(EntityUid recipient)
    {
        return HasComp<MobStateComponent>(recipient) && HasComp<BloodstreamComponent>(recipient);
    }

    /// <summary>
    /// Inserts the animal into the body container (does NOT start injection).
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDragDrop(Entity<CellMutagenicInjectorComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!CanAccept(ent, args.Dragged))
            return;

        args.Handled = InsertBody(ent, args.Dragged);
    }

    /// <summary>
    /// Adds an Eject verb for the contained animal.
    /// </summary>
    [SubscribeLocalEvent]
    private void AddAlternativeVerbs(Entity<CellMutagenicInjectorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.BodyContainer.ContainedEntity is null)
            return;

        AlternativeVerb ejectVerb = new()
        {
            Act = () => EjectBody(ent),
            Category = VerbCategory.Eject,
            Text = Name(ent.Comp.BodyContainer.ContainedEntity.Value),
            Priority = 1
        };

        args.Verbs.Add(ejectVerb);
    }

    /// <summary>
    /// Puts the animal in the container and closes the door.
    /// </summary>
    private bool InsertBody(Entity<CellMutagenicInjectorComponent> ent, EntityUid toInsert)
    {
        if (ent.Comp.BodyContainer.ContainedEntity is not null)
            return false;

        var xform = Transform(toInsert);
        if (!_container.Insert((toInsert, xform), ent.Comp.BodyContainer))
            return false;

        UpdateDoorVisual(ent, true);
        return true;
    }

    /// <summary>
    /// Removes the animal and opens the door.
    /// </summary>
    public void EjectBody(Entity<CellMutagenicInjectorComponent> ent)
    {
        CancelInjection(ent);

        if (ent.Comp.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return;

        _container.Remove(contained, ent.Comp.BodyContainer);
        _climb.ForciblySetClimbing(contained, ent);
        UpdateDoorVisual(ent, false);
    }

    /// <summary>
    /// Toggles the door sprite between open and closed.
    /// </summary>
    private void UpdateDoorVisual(Entity<CellMutagenicInjectorComponent> ent, bool occupied)
    {
        Appearance.SetData(ent, MutagenicInjectorVisuals.DoorState, occupied);
    }

    /// <summary>
    /// Cancels the current machine operation before the subject leaves.
    /// </summary>
    protected virtual void CancelInjection(Entity<CellMutagenicInjectorComponent> ent) { }

    /// <summary>
    /// Ejects the animal when the machine is destroyed.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDestruction(Entity<CellMutagenicInjectorComponent> ent, ref DestructionEventArgs args)
    {
        EjectBody(ent);
    }
}
