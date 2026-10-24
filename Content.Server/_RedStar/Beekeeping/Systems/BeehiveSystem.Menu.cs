using System.Globalization;
using Content.Shared.ActionBlocker;
using Content.Shared.Verbs;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// The hive's own context menu instead of the generic "Eject" of its item slots: "Take out" lists the
/// frames (each shown by its live sprite, so the fill is visible), "Catch" lists the queen, the scout
/// and a worker. Bees in the hive are a counter, so their icons are hidden look-alikes kept inside the hive.
/// </summary>
public sealed partial class BeehiveSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;

    public static readonly VerbCategory FramesCategory =
        new("beekeeping-verb-category-frames", "/Textures/Interface/VerbIcons/eject.svg.192dpi.png");

    /// <summary>
    /// The category icon is a fixed picture: categories only take a texture, not a live entity.
    /// </summary>
    public static readonly VerbCategory BeesCategory =
        new("beekeeping-verb-category-bees", "/Textures/_RedStar/Objects/Specific/Beekeeping/verb-icons.rsi/queen.png");

    /// <summary>
    /// Frames sit above the bees in the menu. The bees use priorities 1 to 3, so the frames start well above them.
    /// </summary>
    public const int FramePriority = 10;

    /// <summary>
    /// Puts the icon look-alikes into the hive once. They live and die with the hive and never leave it.
    /// </summary>
    private void EnsureBeeIcons(Entity<BeehiveComponent> hive)
    {
        EnsureIcon(hive, hive.Comp.ScoutIconContainer, hive.Comp.ScoutIconPrototype);
        EnsureIcon(hive, hive.Comp.PollinatorIconContainer, hive.Comp.PollinatorIconPrototype);
    }

    private void EnsureIcon(EntityUid hive, string containerId, EntProtoId proto)
    {
        var slot = _container.EnsureContainer<ContainerSlot>(hive, containerId);
        if (slot.ContainedEntity != null)
            return;

        var icon = Spawn(proto, Transform(hive).Coordinates);
        if (!_container.Insert(icon, slot))
            Del(icon);
    }

    public EntityUid? GetIcon(EntityUid hive, string containerId)
    {
        return _container.TryGetContainer(hive, containerId, out var container) && container is ContainerSlot slot
            ? slot.ContainedEntity
            : null;
    }

    [SubscribeLocalEvent]
    private void OnHiveGetVerbs(Entity<BeehiveComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        var comp = ent.Comp;

        _queen.AddJellyVerb(ent, ref args);

        // A wild hive has no frames to take out and no bees to catch.
        if (comp.Wild)
            return;

        // Frames come first, in slot order, so the alt-click takes out a frame rather than a bee.
        for (var i = 0; i < comp.FrameSlots.Count; i++)
        {
            if (!ItemSlots.TryGetSlot(ent.Owner, comp.FrameSlots[i], out var slot) || slot.Item is not { } frame)
                continue;

            if (!ItemSlots.CanEject(ent, slot, user) || !_actionBlocker.CanPickup(user, frame))
                continue;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString(slot.Name),
                IconEntity = GetNetEntity(frame),
                Category = FramesCategory,
                Priority = FramePriority - i,
                Act = () => ItemSlots.TryEjectToHands(ent, slot, user, excludeUserAudio: true),
            });
        }

        // The queen, then the bees.
        if (ItemSlots.TryGetSlot(ent.Owner, comp.QueenSlot, out var queenSlot) &&
            queenSlot.Item is { } queen &&
            ItemSlots.CanEject(ent, queenSlot, user) &&
            _actionBlocker.CanPickup(user, queen))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Capitalize(Name(queen)),
                IconEntity = GetNetEntity(queen),
                Category = BeesCategory,
                Priority = 3,
                Act = () => StartTakeQueen(ent, user),
            });
        }

        if (comp.Bees <= 0 && comp.OutBees <= 0)
            return;

        AddTakeBeeVerb(ent, ref args, BeeRole.Scout, comp.ScoutPrototype, comp.ScoutIconContainer, 2);
        AddTakeBeeVerb(ent, ref args, BeeRole.Pollinator, comp.PollinatorPrototype, comp.PollinatorIconContainer, 1);
    }

    private void AddTakeBeeVerb(Entity<BeehiveComponent> hive,
        ref GetVerbsEvent<AlternativeVerb> args,
        BeeRole role,
        EntProtoId proto,
        string iconContainer,
        int priority)
    {
        var user = args.User;
        var blocker = TakeBeeBlocker(hive.Comp, role);
        var icon = GetIcon(hive, iconContainer);

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Capitalize(_proto.TryIndex(proto, out var entity) ? entity.Name : proto.Id),
            IconEntity = icon != null ? GetNetEntity(icon.Value) : null,
            Category = BeesCategory,
            Priority = priority,
            Disabled = blocker != null,
            Message = blocker != null ? Loc.GetString(blocker) : null,
            Act = () => StartTakeBee(hive, user, role),
        });
    }

    private static string Capitalize(string text)
    {
        return string.IsNullOrEmpty(text)
            ? text
            : char.ToUpper(text[0], CultureInfo.CurrentCulture) + text[1..];
    }
}
