using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Events;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RedStar.Genetics.Systems;

public abstract partial class GeneticAnalyzerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<GeneticAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<GenomeComponent>(target, out var genome) || !genome.GenomeGenerated)
        {
            _popup.PopupEntity(Loc.GetString("genetic-analyzer-no-genome"), ent, args.User);
            return;
        }

        args.Handled = true;
        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ScanDelay,
                new GeneticAnalyzerDoAfterEvent(), ent, target: target, used: ent)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnDamage = true,
            }) || target == args.User)
            return;

        _popup.PopupEntity(Loc.GetString("genetic-analyzer-scan-target",
            ("user", Identity.Entity(args.User, EntityManager))), target, target, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<GeneticAnalyzerComponent> ent, ref GeneticAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target ||
            !TryComp<GenomeComponent>(target, out var genome) || !genome.GenomeGenerated)
            return;

        args.Handled = true;
        _audio.PlayPredicted(ent.Comp.ScanningEndSound, ent, args.User);
        CompleteScan(ent, target, args.User);
    }

    /// <summary>
    /// Starts the server's live report after the predicted scan completes successfully.
    /// </summary>
    protected virtual void CompleteScan(Entity<GeneticAnalyzerComponent> analyzer, EntityUid target, EntityUid user) { }
}
