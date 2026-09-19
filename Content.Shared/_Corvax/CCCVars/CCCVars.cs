using Content.Shared._Corvax.TTS.Enums;
using Robust.Shared.Configuration;

namespace Content.Shared._Corvax.CCCVars;

/// <summary>
/// Corvax modules console variables
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming
public sealed class CCCVars
{
    /// <summary>
    /// Send station goal on round start or not.
    /// </summary>
    public static readonly CVarDef<bool> StationGoal =
        CVarDef.Create("game.station_goal", true, CVar.SERVERONLY);

    /**
     * TTS (Text-To-Speech)
     */
    /// <summary>
    /// URL of the TTS server API.
    /// </summary>
    public static readonly CVarDef<bool> TTSEnabled =
        CVarDef.Create("tts.enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// URL of the TTS server API.
    /// </summary>
    public static readonly CVarDef<string> TTSApiUrl =
        CVarDef.Create("tts.api_url", "", CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Auth token of the TTS server API.
    /// </summary>
    public static readonly CVarDef<string> TTSApiToken =
        CVarDef.Create("tts.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Amount of seconds before timeout for API
    /// </summary>
    public static readonly CVarDef<int> TTSApiTimeout =
        CVarDef.Create("tts.api_timeout", 5, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Volume of TTS radio messages
    /// </summary>
    public static readonly CVarDef<float> TTSRadioVolume =
        CVarDef.Create("tts.radio_volume", 1.2f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Default volume setting of TTS sound
    /// </summary>
    public static readonly CVarDef<float> TTSVolume =
        CVarDef.Create("tts.volume", 1.2f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// TTS voice effect preset. 0 = None.
    /// </summary>
    public static readonly CVarDef<int> TTSVoiceEffect =
        CVarDef.Create("tts.voice_effect", 0, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Bitmask of enabled radio channels for TTS. <see cref="RadioChannelFlag"/>
    /// </summary>
    public static readonly CVarDef<int> TTSRadioFilter =
        CVarDef.Create("tts.radio_filter", (int)RadioChannelFlag.AllExceptCommon,
            CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Count of in-memory cached tts voice lines.
    /// </summary>
    public static readonly CVarDef<int> TTSMaxCache =
        CVarDef.Create("tts.max_cache", 250, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum number of concurrent requests to the TTS service.
    /// </summary>
    public static readonly CVarDef<int> TTSMaxConcurrentRequests =
        CVarDef.Create("tts.max_concurrent_requests", 32, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum number of TTS requests waiting for an available request slot.
    /// </summary>
    public static readonly CVarDef<int> TTSMaxQueuedRequests =
        CVarDef.Create("tts.max_queued_requests", 256, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Number of consecutive failures required to open the TTS circuit breaker.
    /// Zero disables the circuit breaker.
    /// </summary>
    public static readonly CVarDef<int> TTSCircuitBreakerFailures =
        CVarDef.Create("tts.circuit_breaker_failures", 20, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// TTS circuit breaker cooldown in seconds before allowing a test request.
    /// </summary>
    public static readonly CVarDef<float> TTSCircuitBreakerCooldown =
        CVarDef.Create("tts.circuit_breaker_cooldown", 15f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Tts rate limit values are accounted in periods of this size (seconds).
    /// After the period has passed, the count resets.
    /// </summary>
    public static readonly CVarDef<float> TTSRateLimitPeriod =
        CVarDef.Create("tts.rate_limit_period", 2f, CVar.SERVERONLY);

    /// <summary>
    /// How many tts preview messages are allowed in a single rate limit period.
    /// </summary>
    public static readonly CVarDef<int> TTSRateLimitCount =
        CVarDef.Create("tts.rate_limit_count", 3, CVar.SERVERONLY);
}
