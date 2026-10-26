using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Surgery.Tools;
using Content.Shared.Body;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Trauma.Surgery.Tools;

/// <summary>
///     Examining a surgical or ghetto tool shows everything it can be used for.
/// </summary>
public sealed partial class SurgeryToolExamineSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;

    [SubscribeLocalEvent]
    private void OnExaminedBoneGel(Entity<BoneGelComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedBoneSaw(Entity<BoneSawComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedCautery(Entity<CauteryComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedHemostat(Entity<HemostatComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedRetractor(Entity<RetractorComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedScalpel(Entity<ScalpelComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedDrill(Entity<DrillComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedTending(Entity<TendingComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedTweezers(Entity<TweezersComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedBoneSetter(Entity<BoneSetterComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedBodyPart(Entity<BodyPartComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedInternalChildOrgan(Entity<InternalChildOrganComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnExaminedStitches(Entity<StitchesComponent> ent, ref SurgeryToolExaminedEvent args)
        => OnExamined(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<SurgeryToolComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var msg = FormattedMessage.FromMarkupOrThrow(Loc.GetString("surgery-tool-header"));
        msg.PushNewline();
        var ev = new SurgeryToolExaminedEvent(msg);
        RaiseLocalEvent(ent, ref ev);

        _examine.AddDetailedExamineVerb(args, ent.Comp, ev.Message,
            Loc.GetString("surgery-tool-examinable-verb-text"), "/Textures/_RedStar/Medical/Objects/Specific/Medical/Surgery/scalpel.rsi/scalpel.png",
            Loc.GetString("surgery-tool-examinable-verb-message"));
    }

    public void OnExamined(EntityUid uid, BaseSurgeryToolComponent comp, ref SurgeryToolExaminedEvent args)
    {
        var msg = args.Message;
        var color = comp.Speed switch
        {
            < 1f => "red",
            > 1f => "green",
            _ => "white"
        };
        var key = "surgery-tool-" + (comp.Used == true ? "used" : "unlimited");
        var speed = comp.Speed.ToString("N2"); // 2 decimal places to not get trolled by float
        msg.AddMarkupOrThrow(Loc.GetString(key, ("tool", comp.ToolName), ("speed", speed), ("color", color)));
        msg.PushNewline();
    }
}

[ByRefEvent]
public record struct SurgeryToolExaminedEvent(FormattedMessage Message);
