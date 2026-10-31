using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Events;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Systems;

/// <summary>
/// What a tool or wax does to a frame. Wax is fully predicted: it only touches networked fields, the stack and the
/// appearance. Cutting is split: the progress bar starts here at once, and the result (combs and wax with the payload
/// from the queen, which only the server knows) is the server's, in <c>BeehiveSystem.OnFrameCut</c>.
/// </summary>
public abstract partial class SharedBeehiveSystem
{
    [Dependency] private SharedDoAfterSystem _frameDoAfter = default!;
    [Dependency] private SharedToolSystem _frameTool = default!;
    [Dependency] private SharedStackSystem _frameStack = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;

    /// <summary>
    /// Tool quality that cuts the combs off a frame.
    /// </summary>
    private static readonly ProtoId<ToolQualityPrototype> SlicingQuality = "Slicing";

    /// <summary>
    /// Tool quality that dismantles a frame through its construction graph.
    /// </summary>
    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    /// <summary>
    /// Systems that must see a tool on a frame after this one: a tool that can both slice and pry should cut here,
    /// not dismantle.
    /// </summary>
    protected virtual Type[] FrameInteractBefore => Array.Empty<Type>();

    private void InitializeFrames()
    {
        SubscribeLocalEvent<BeeFrameComponent, InteractUsingEvent>(OnFrameInteractUsing, before: FrameInteractBefore);
    }

    private void OnFrameInteractUsing(Entity<BeeFrameComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<StackComponent>(args.Used, out var wax) && wax.StackTypeId == ent.Comp.WaxStack)
        {
            args.Handled = true;
            UpgradeFrame(ent, (args.Used, wax), args.User);
            return;
        }

        if (!_frameTool.HasQuality(args.Used, SlicingQuality))
        {
            // The graph spawns the planks at the frame's own position, which in a hand is the holder:
            // dismantle only frames lying on the floor.
            if (_frameTool.HasQuality(args.Used, PryingQuality) && _containers.IsEntityInContainer(ent))
            {
                args.Handled = true;
                _popupShared.PopupEntity(Loc.GetString("beekeeping-frame-pry-in-hand"), ent, args.User);
            }

            return;
        }

        args.Handled = true;

        if (ent.Comp.Combs <= 0)
        {
            _popupShared.PopupEntity(Loc.GetString("beekeeping-frame-cut-empty"), ent, args.User);
            return;
        }

        _frameDoAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.CutDelay,
            new BeeFrameCutDoAfterEvent(), ent, target: ent, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    /// <summary>
    /// One piece of wax adds room for one more comb. A full frame then has room again and goes back to filling.
    /// </summary>
    private void UpgradeFrame(Entity<BeeFrameComponent> frame, Entity<StackComponent> wax, EntityUid user)
    {
        if (frame.Comp.MaxCombs >= frame.Comp.MaxCombsLimit)
        {
            _popupShared.PopupEntity(Loc.GetString("beekeeping-frame-upgrade-max", ("max", frame.Comp.MaxCombs)), frame, user);
            return;
        }

        if (!_frameStack.TryUse(wax.AsNullable(), 1))
            return;

        frame.Comp.MaxCombs++;
        Dirty(frame);
        UpdateFrameFill(frame);
        _popupShared.PopupEntity(Loc.GetString("beekeeping-frame-upgraded", ("max", frame.Comp.MaxCombs)), frame, user);
        UpdateFrameHive(frame);
    }

    /// <summary>
    /// The frame's sprite follows how full it is. The honey's color is set by the server, which knows the payload.
    /// </summary>
    protected void UpdateFrameFill(Entity<BeeFrameComponent> frame)
    {
        Appearance.SetData(frame, BeeFrameVisuals.Fill, GetFill(frame.Comp));
    }

    /// <summary>
    /// A frame that is in a hive changed how full it is: the hive's picture follows.
    /// </summary>
    protected void UpdateFrameHive(Entity<BeeFrameComponent> frame)
    {
        if (_containers.TryGetContainingContainer((frame.Owner, null, null), out var container) &&
            TryComp<BeehiveComponent>(container.Owner, out var hive))
        {
            UpdateHiveAppearance((container.Owner, hive));
        }
    }

    public void UpdateHiveAppearance(Entity<BeehiveComponent> hive)
    {
        GetFrames(hive, _appearanceFrames);
        Appearance.SetData(hive, BeehiveVisuals.Frames, _appearanceFrames.Count);
        Appearance.SetData(hive, BeehiveVisuals.Frame1, FillAt(0));
        Appearance.SetData(hive, BeehiveVisuals.Frame2, FillAt(1));
        Appearance.SetData(hive, BeehiveVisuals.Frame3, FillAt(2));
        Appearance.SetData(hive, BeehiveVisuals.Frame4, FillAt(3));
    }

    private readonly List<Entity<BeeFrameComponent>> _appearanceFrames = new();

    private BeeFrameFill FillAt(int index)
    {
        return index < _appearanceFrames.Count ? GetFill(_appearanceFrames[index].Comp) : BeeFrameFill.None;
    }
}
