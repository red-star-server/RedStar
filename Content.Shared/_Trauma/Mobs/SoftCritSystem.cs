using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.CCVar;
using Content.Shared._Trauma.Medical;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Events;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Robust.Shared.Configuration;

namespace Content.Shared._Trauma.Mobs;

/// <summary>
/// Handles interactions with softcrit mobs.
/// </summary>
public sealed partial class SoftCritSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedCPRSystem _cpr = default!;

    /// <summary>
    /// Speed modifier for softcrit mobs, on top of being forced to crawl.
    /// </summary>
    public float SoftCritSpeed = 0.5f;

    /// <summary>
    /// Inhaled gas modifier for softcrit mobs, makes it harder to breathe.
    /// This means you can't just crawl around forever if you aren't bleeding out.
    /// </summary>
    public float InhaleVolumeModifier = 0.3f;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(TraumaCVars.SoftCritMoveSpeed, x => SoftCritSpeed = x, true);
        _cfg.OnValueChanged(TraumaCVars.SoftCritInhaleModifier, x => InhaleVolumeModifier = x, true);
    }

    [SubscribeLocalEvent(before: [typeof(VocalSystem)])]
    private void OnEmoteAction(Entity<SoftCritMobComponent> ent, ref EmoteActionEvent args)
    {
        args.Handled = true; // shush
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<SoftCritMobComponent> ent, ref ComponentStartup args)
        => _movement.RefreshMovementSpeedModifiers(ent.Owner);

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SoftCritMobComponent> ent, ref ComponentShutdown args)
        => _movement.RefreshMovementSpeedModifiers(ent.Owner);

    [SubscribeLocalEvent]
    private void OnAttemptStopPulling(Entity<MobStateComponent> ent, ref AttemptStopPullingEvent args)
    {
        // too weak to resist being pulled away into maints if you aren't alive
        if (ent.Comp.CurrentState != MobState.Alive && ent.Owner == args.User)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnSpeechTypeOverride(Entity<SoftCritMobComponent> ent, ref SpeechTypeOverrideEvent args)
    {
        // Soft critical state limits speech.
        if (args.DesiredType == InGameICChatType.Speak)
            args.DesiredType = InGameICChatType.Whisper;
    }

    [SubscribeLocalEvent]
    private void OnRefreshSpeed(Entity<SoftCritMobComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(SoftCritSpeed);
    }

    [SubscribeLocalEvent]
    private void OnStandUpAttempt(Entity<SoftCritMobComponent> ent, ref StandUpAttemptEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnModifyInhaledVolume(Entity<SoftCritMobComponent> ent, ref ModifyInhaledVolumeEvent args)
    {
        // don't reduce volume if someone else is helping you breathe
        // ideally there would be code in respirator to check if it's forced to breathe vs lungs working alone
        if (!_cpr.IsCPRActive(ent))
            args.Volume *= InhaleVolumeModifier;
    }

    [SubscribeLocalEvent]
    private void OnUnbuckleAttempt(Entity<SoftCritMobComponent> ent, ref UnbuckleAttemptEvent args)
    {
        // can't unbuckle yourself if you are in softcrit
        args.Cancelled |= args.Buckle.Owner == args.User;
    }

    [SubscribeLocalEvent]
    private void OnRadioSendAttempt(Entity<SoftCritMobComponent> ent, ref RadioSendAttemptEvent args)
    {
        args.Cancelled = true; // no yapping on radio chuddy
    }
}
