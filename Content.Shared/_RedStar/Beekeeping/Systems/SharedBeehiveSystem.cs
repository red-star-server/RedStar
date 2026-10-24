using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared._RedStar.Beekeeping.Systems;

/// <summary>
/// Shared part of beekeeping: reading the hive slots, the frames and their pictures, and the examine text of frames and
/// bees, whose fields are networked. The colony itself and its examine text are the server's.
/// </summary>
public abstract partial class SharedBeehiveSystem : EntitySystem
{
    [Dependency] protected ItemSlotsSystem ItemSlots = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeFrames();
    }

    #region Helpers

    /// <summary>
    /// The queen in the hive, if there is one.
    /// </summary>
    public EntityUid? GetQueen(Entity<BeehiveComponent> hive)
    {
        var item = ItemSlots.GetItemOrNull(hive.Owner, hive.Comp.QueenSlot);
        return item != null && HasComp<BeeQueenComponent>(item) ? item : null;
    }

    /// <summary>
    /// Inserted frames in slot order. The index in this list is the frame number on the sprite.
    /// </summary>
    public void GetFrames(Entity<BeehiveComponent> hive, List<Entity<BeeFrameComponent>> frames)
    {
        frames.Clear();
        foreach (var slot in hive.Comp.FrameSlots)
        {
            if (ItemSlots.GetItemOrNull(hive.Owner, slot) is { } item && TryComp<BeeFrameComponent>(item, out var frame))
                frames.Add((item, frame));
        }
    }

    public static BeeFrameFill GetFill(BeeFrameComponent frame)
    {
        if (frame.Combs <= 0)
            return BeeFrameFill.None;

        return frame.Combs >= frame.MaxCombs && !frame.Uncapped ? BeeFrameFill.Wax : BeeFrameFill.Honey;
    }

    /// <summary>
    /// Bees at home that scout on their own. Zero while the only scout is outside or lost.
    /// </summary>
    public static int ScoutsAtHome(BeehiveComponent hive)
    {
        return hive.ScoutState == BeeScoutState.Home ? Math.Min(Math.Max(hive.Scouts, 0), Math.Max(hive.Bees, 0)) : 0;
    }

    /// <summary>
    /// Bees at home that carry nectar and make combs.
    /// </summary>
    public static int WorkersAtHome(BeehiveComponent hive)
    {
        return Math.Max(0, hive.Bees - ScoutsAtHome(hive));
    }

    #endregion

    #region Examine

    [SubscribeLocalEvent]
    private void OnFrameExamined(Entity<BeeFrameComponent> ent, ref ExaminedEvent args)
    {
        var fill = GetFill(ent.Comp);
        args.PushMarkup(Loc.GetString("beekeeping-frame-examine",
            ("state", fill.ToString()), ("combs", ent.Comp.Combs), ("max", ent.Comp.MaxCombs)));
    }

    [SubscribeLocalEvent]
    private void OnBeeExamined(Entity<BeeItemComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        using (args.PushGroup(nameof(BeeItemComponent)))
        {
            switch (comp.Role)
            {
                case BeeRole.Scout:
                    args.PushMarkup(Loc.GetString("beekeeping-bee-examine-scout",
                        ("count", comp.MemoryCount), ("max", comp.MemoryLimit)));
                    break;
                case BeeRole.Pollinator:
                    args.PushMarkup(Loc.GetString("beekeeping-bee-examine-pollinator",
                        ("charges", comp.Charges)));
                    break;
            }

            if (comp.Traits.Count > 0)
                args.PushMarkup(Loc.GetString("beekeeping-bee-examine-traits", ("traits", TraitList(comp.Traits))));

            if (comp.ExpiresAt > TimeSpan.Zero && comp.ExpiresAt - Timing.CurTime <= comp.DyingWarning)
                args.PushMarkup(Loc.GetString("beekeeping-bee-examine-dying"));
        }
    }

    #endregion
}
