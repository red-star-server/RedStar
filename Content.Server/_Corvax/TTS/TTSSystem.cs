using System.Linq;
using System.Threading.Tasks;
using Content.Server.Communications;
using Content.Server.Power.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared._Corvax.CCCVars;
using Content.Shared._Corvax.TTS;
using Content.Shared._Corvax.TTS.Components;
using Content.Shared._Corvax.TTS.Events;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Components;
using Content.Shared.Humanoid;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech.Muting;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private TTSManager _ttsManager = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private SharedTransformSystem _xforms = default!;
    [Dependency] private IRobustRandom _rng = default!;

    private readonly List<string> _sampleText = new()
    {
        // Neutral / Declarative
        "Съешь же ещё этих мягких французских булок, да выпей чаю.",
        "Инженеры закончили настройку сингулярности, теперь всё работает стабильно.",
        "Квартирмейстер подтвердил заказ на новую партию оборудования.",

        // Interrogative / Questions (rising intonation at the end)
        "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
        "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
        "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
        "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
        "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах?",

        // Exclamatory / Emotional (emphasis on UPPERCASE words)
        "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! ПОМОГИТЕ!!",
        "Учёные, тут странная аномалия в баре! Она уже съела МИМА!",
        "Возле эвакуационного шаттла РАЗГЕРМЕТИЗАЦИЯ! Инженеры, нам СРОЧНО нужна ваша помощь!",
        "Капитан, КЛОУН разбрасывает банановые кожурки под ноги офицерам!",

        // Mixed / Question + Exclamation
        "Ты серьёзно думаешь, что это хорошая идея?!",
        "Что ты делаешь?! Немедленно прекрати!",
        "Ты это видел?! Это было невероятно!",

        // Ellipsis / Pauses for Suspense
        "Я думаю... нам стоит пересмотреть этот план...",
        "Странно... я только что видел здесь кого-то... но никого нет...",
        "Командир... я должен вам кое-что сказать... это важно...",

        // Strong Emphasis (НЕТ / ДА / НЕ)
        "НЕТ! Я НЕ пойду в этот отсек! Это СЛИШКОМ опасно!",
        "ДА! Мы сделали это! ПОБЕДА!",
        "Я ТРЕБУЮ! Немедленно прекратить эксперименты!",

        // Short Radio / Command Style
        "Внимание всем! Переходим на аварийный режим работы!",
        "Приём! Требуется подкрепление в зоне мостика!",

        // Long Sentences / Breath Pauses
        "Я хочу чтобы вы знали, что эта станция лучшая во всём секторе, и каждый из вас вносит огромный вклад в наше общее дело, поэтому я горжусь вами.",

        // Lists / Enumeration
        "Что нам нужно сделать? Во-первых, проверить системы; во-вторых, подготовить отчёт; и в-третьих, доложить командованию.",
        "В ящике лежат: инструмент, медицинские наборы и противогазы.",

        // Calm / Reassuring
        "Не волнуйтесь, я контролирую ситуацию, всё будет хорошо.",
        "Сохраняйте спокойствие, мы уже на подходе к решению."
        // Да я подписал все на английском и чо? Вчіть мову
    };

    private static readonly ProtoId<TTSVoicePrototype> AnnouncementSpeaker = "Glados";

    private const SoundTraits NormalTraits = SoundTraits.RateFast | SoundTraits.PitchMedium;
    private const SoundTraits WhisperTraits = SoundTraits.RateSlow | SoundTraits.PitchVerylow | SoundTraits.VolumeXSoft;

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private const float AnnouncementDelay = 2.25f;
    private bool _isEnabled;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCCVars.TTSEnabled, OnTTSEnabledChanged, true);

        InitializeSanitize();
        RegisterRateLimits();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCCVars.TTSEnabled, OnTTSEnabledChanged);
    }

    private void OnTTSEnabledChanged(bool value)
    {
        _isEnabled = value;
    }

    [SubscribeLocalEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
        => _ttsManager.ResetCache();

    [SubscribeNetworkEvent]
    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled || !ProtoMan.TryIndex(ev.VoiceId, out var protoVoice))
            return;

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker, NormalTraits);
        if (soundData is null || !_isEnabled)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData, kind: TTSKind.Preview),
            Filter.SinglePlayer(args.SenderSession),
            recordReplay: false);
    }

    [SubscribeLocalEvent]
    private void OnConsoleAnnouncement(ref CommunicationConsoleAnnouncementEvent ev)
    {
        if (!_isEnabled || string.IsNullOrEmpty(ev.Text))
            return;

        var station = _stationSystem.GetOwningStation(ev.Uid);
        if (station == null)
            return;

        var stationUid = station.Value;

        if (!HasComp<StationDataComponent>(stationUid))
            return;

        TTSVoicePrototype? voicePrototype = null;

        if (ev.Sender is { } sender && !HasComp<MutedStatusEffectComponent>(sender))
            voicePrototype = ResolveVoice(sender);

        if (voicePrototype == null)
        {
            if (!ProtoMan.TryIndex(AnnouncementSpeaker, out var announcementVoice))
                return;

            voicePrototype = announcementVoice;
        }

        HandleConsoleAnnouncement(ev.Text, voicePrototype.Speaker, ev.Component.Sound, stationUid);
    }

    private async void HandleConsoleAnnouncement(string text, string speaker,
        SoundSpecifier sound, EntityUid station)
    {
        var soundData = await GenerateTTS(text, speaker, NormalTraits);
        if (soundData is null)
            return;

        var timeDelay = (float)_audio.GetAudioLength(_audio.ResolveSound(sound)).TotalSeconds + AnnouncementDelay;

        Timer.Spawn(TimeSpan.FromSeconds(timeDelay), () =>
        {
            if (!_isEnabled || TerminatingOrDeleted(station))
                return;

            var filter = GetStationFilter(station);
            if (filter == null)
                return;

            RaiseNetworkEvent(new PlayTTSEvent(soundData), filter,
                recordReplay: false);
        });
    }

    private Filter? GetStationFilter(Entity<StationDataComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return null;

        return _stationSystem.GetInStation(station.Comp);
    }

    private TTSVoicePrototype? ResolveVoice(EntityUid uid, TTSComponent? component = null)
    {
        if (component == null)
            TryComp(uid, out component);

        var voiceId = component?.VoicePrototypeId;

        if (voiceId == null && TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            voiceId = humanoid.TTSVoice;

        if (voiceId == null)
            return null;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId.Value);
        RaiseLocalEvent(uid, voiceEv);

        if (!ProtoMan.TryIndex(voiceEv.VoiceId, out var voicePrototype))
            return null;

        return voicePrototype;
    }

    [SubscribeLocalEvent(before: [typeof(RadioSystem), typeof(HeadsetSystem)])]
    private void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        var protoVoice = ResolveVoice(uid, component);
        if (protoVoice == null)
            return;

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.TTSMessage, protoVoice.Speaker, args.Channel);
            return;
        }

        HandleSay(uid, args.TTSMessage, protoVoice.Speaker, args.Channel);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker, RadioChannelPrototype? channel)
    {
        var soundData = await GenerateTTS(message, speaker, NormalTraits);
        if (soundData is null || TerminatingOrDeleted(uid))
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid)), Filter.Pvs(uid),
            recordReplay: false);

        if (channel != null)
            SendTTSToRadio(soundData, uid, channel, false);
    }

    private async void HandleWhisper(EntityUid uid, string message, string speaker,
        RadioChannelPrototype? channel)
    {
        var fullSoundData = await GenerateTTS(message, speaker, WhisperTraits);
        if (fullSoundData is null || TerminatingOrDeleted(uid))
            return;

        // I never saw the point of voicing just four or five letters in a long message, only to get a jumbled mess in response.
        // Response "~ ~~~~ ~~~ пыр-~ы~-~~~" this is the most useless waste of money I've ever seen.

        var fullTtsEvent = new PlayTTSEvent(fullSoundData, GetNetEntity(uid), true);

        // TODO: Check obstacles
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;
        var clearFilter = Filter.Empty();

        foreach (var session in receptions)
        {
            if (!session.AttachedEntity.HasValue)
                continue;

            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

            if (distance > SharedChatSystem.WhisperClearRange)
                continue;

            clearFilter.AddPlayer(session);
        }

        if (clearFilter.Recipients.Any())
            RaiseNetworkEvent(fullTtsEvent, clearFilter, recordReplay: false);

        if (channel != null)
            SendTTSToRadio(fullSoundData, uid, channel);
    }

    private void SendTTSToRadio(byte[] soundData, EntityUid sourceUid, RadioChannelPrototype channel,
        bool isWhisper = true)
    {
        var netSource = GetNetEntity(sourceUid);
        var ttsEvent = new PlayTTSEvent(soundData, netSource, isWhisper, TTSKind.Radio, channel.ID);
        var filter = Filter.Empty();

        var sourceMapId = Transform(sourceUid).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);

        var query = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();
        while (query.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels && !radio.Channels.Contains(channel.ID))
                continue;

            if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive)
                continue;

            var needServer = !channel.LongRange && !HasComp<TelecomExemptComponent>(receiver);
            if (needServer && !hasActiveServer)
                continue;

            var attemptEv = new RadioReceiveAttemptEvent(channel, sourceUid, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);

            if (attemptEv.Cancelled)
                continue;

            EntityUid? wearer = null;

            // Receiver could be, for example, a Borg
            if (TryComp(receiver, out ActorComponent? actor)
                && !HasComp<GhostComponent>(receiver)) // Save the ghosts ears
            {
                wearer = receiver;
            }
            // Wearer is the entity currently wearing the headset
            else if (TryComp<HeadsetComponent>(receiver, out var headset))
            {
                if (!headset.Enabled || !headset.IsEquipped)
                    continue;

                wearer = transform.ParentUid;
            }

            if (wearer == null)
                continue;

            if (!TryComp(wearer.Value, out actor))
                continue;

            var session = actor.PlayerSession;

            if (session.AttachedEntity == sourceUid)
                continue;

            filter.AddPlayer(session);
        }

        if (!filter.Recipients.Any())
            return;

        RaiseNetworkEvent(ttsEvent, filter, recordReplay: false);
    }

    /// <inheritdoc cref="TelecomServerComponent"/>
    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();

        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId && power.Powered && keys.Channels.Contains(channelId))
                return true;
        }

        return false;
    }

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, SoundTraits traits)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "")
            return null;

        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var textSsml = ToSsmlText(textSanitized, traits);
        return await _ttsManager.ConvertTextToSpeech(speaker, textSsml);
    }
}
