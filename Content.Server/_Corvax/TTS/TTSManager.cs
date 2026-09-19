using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Corvax.CCCVars;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server._Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration
        {
            LabelNames = ["type"],
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10)
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache or from an already running request.");

    private static readonly Counter DroppedCount = Metrics.CreateCounter(
        "tts_dropped_count",
        "Amount of TTS requests dropped without hitting the API.");

    private static readonly Counter CircuitOpenCount = Metrics.CreateCounter(
        "tts_circuit_open_count",
        "Amount of times the TTS circuit breaker has been opened.");

    [Dependency] private IConfigurationManager _cfg = default!;

    private HttpClient _httpClient = default!;
    private ISawmill _sawmill = default!;

    private readonly object _lock = new();

    private readonly Dictionary<string, byte[]> _cache = new();
    private readonly Queue<string> _cacheOrder = new();
    private readonly Dictionary<string, Task<byte[]?>> _inFlight = new();
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();

    private int _activeRequests;
    private int _maxCachedCount;
    private int _maxConcurrent;
    private int _maxQueued;
    private int _breakerFailures;
    private float _breakerCooldown;

    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;
    private bool _isEnabled;

    private int _consecutiveFailures;
    private DateTime _circuitOpenedAt;
    private CircuitState _circuit = CircuitState.Closed;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");

        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 128,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(5)
        });

        _cfg.OnValueChanged(CCCVars.TTSEnabled, v =>
        {
            _isEnabled = v;

            if (!v)
                ResetCache();
        }, true);

        _cfg.OnValueChanged(CCCVars.TTSMaxCache, val =>
        {
            lock (_lock)
            {
                _maxCachedCount = Math.Max(0, val);
                TrimCacheLocked();
            }
        }, true);

        _cfg.OnValueChanged(CCCVars.TTSApiUrl, v =>
        {
            if (_apiUrl == v)
                return;

            _apiUrl = v;
            ResetCache();
        }, true);

        _cfg.OnValueChanged(CCCVars.TTSApiToken, v => _apiToken = v, true);
        _cfg.OnValueChanged(CCCVars.TTSMaxConcurrentRequests, v => _maxConcurrent = Math.Max(1, v), true);
        _cfg.OnValueChanged(CCCVars.TTSMaxQueuedRequests, v => _maxQueued = Math.Max(0, v), true);
        _cfg.OnValueChanged(CCCVars.TTSCircuitBreakerFailures, v =>
        {
            lock (_lock)
            {
                _breakerFailures = Math.Max(0, v);

                if (_breakerFailures == 0)
                {
                    _consecutiveFailures = 0;
                    _circuit = CircuitState.Closed;
                }
            }
        }, true);
        _cfg.OnValueChanged(CCCVars.TTSCircuitBreakerCooldown,
            v => _breakerCooldown = Math.Max(0f, v),
            true);
    }

    /// <summary>
    /// Generates audio with passed text by API.
    /// </summary>
    /// <param name="speaker">Identifier of speaker.</param>
    /// <param name="text">SSML formatted text.</param>
    /// <returns>OGG audio bytes or null if failed.</returns>
    public Task<byte[]?> ConvertTextToSpeech(string speaker, string text)
    {
        WantedCount.Inc();

        if (!_isEnabled)
        {
            DroppedCount.Inc();
            return Task.FromResult<byte[]?>(null);
        }

        var cacheKey = GenerateCacheKey(speaker, text);

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var data))
            {
                ReusedCount.Inc();
                _sawmill.Verbose($"Use cached sound for '{text}' speech by '{speaker}' speaker");
                return Task.FromResult<byte[]?>(data);
            }

            if (_inFlight.TryGetValue(cacheKey, out var running))
            {
                ReusedCount.Inc();
                _sawmill.Verbose($"Join running request for '{text}' speech by '{speaker}' speaker");
                return running;
            }

            var task = RequestAsync(speaker, text, cacheKey);

            if (!task.IsCompleted)
                _inFlight[cacheKey] = task;

            return task;
        }
    }

    private async Task<byte[]?> RequestAsync(string speaker, string text, string cacheKey)
    {
        try
        {
            if (IsCircuitBlocking())
            {
                DroppedCount.Inc();
                return null;
            }

            var timeout = TimeSpan.FromSeconds(_cfg.GetCVar(CCCVars.TTSApiTimeout));
            using var cts = new CancellationTokenSource(timeout);

            if (!await TryEnterAsync(cts.Token))
            {
                DroppedCount.Inc();
                _sawmill.Warning($"TTS request queue is full, dropped speech by '{speaker}' speaker");
                return null;
            }

            try
            {
                if (!TryPassCircuitBreaker())
                {
                    DroppedCount.Inc();
                    return null;
                }

                return await SendAsync(speaker, text, cacheKey, cts.Token);
            }
            finally
            {
                Release();
            }
        }
        finally
        {
            lock (_lock)
            {
                _inFlight.Remove(cacheKey);
            }
        }
    }

    private async Task<byte[]?> SendAsync(string speaker, string text, string cacheKey, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiUrl))
        {
            _sawmill.Error("TTS API URL is not configured");
            ReportFailure();
            return null;
        }

        _sawmill.Verbose($"Generate new audio for '{text}' speech by '{speaker}' speaker");

        var body = new GenerateVoiceRequest
        {
            ApiToken = _apiToken,
            Text = text,
            Speaker = speaker
        };

        var reqTime = DateTime.UtcNow;

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(_apiUrl, body, ct);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _sawmill.Warning("TTS request was rate limited");
                    ReportSuccess();
                    return null;
                }

                _sawmill.Error($"TTS request returned bad status code: {response.StatusCode}");
                ReportFailure();
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<GenerateVoiceResponse>(cancellationToken: ct);
            if (json.Results == null || json.Results.Count == 0)
            {
                _sawmill.Error("TTS API returned empty results");
                ReportFailure();
                return null;
            }

            var firstResult = json.Results[0];
            if (string.IsNullOrEmpty(firstResult.Audio))
            {
                _sawmill.Error("TTS API returned empty audio data");
                ReportFailure();
                return null;
            }

            var soundData = Convert.FromBase64String(firstResult.Audio);

            lock (_lock)
            {
                if (_maxCachedCount > 0 && _cache.TryAdd(cacheKey, soundData))
                {
                    _cacheOrder.Enqueue(cacheKey);
                    TrimCacheLocked();
                }
            }

            _sawmill.Debug(
                $"Generated new audio for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");

            RequestTimings
                .WithLabels("Success")
                .Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            ReportSuccess();

            return soundData;
        }
        catch (OperationCanceledException)
        {
            RequestTimings
                .WithLabels("Timeout")
                .Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            _sawmill.Error(
                $"Timeout of request generation new audio for '{text}' speech by '{speaker}' speaker");

            ReportFailure();
            return null;
        }
        catch (Exception e)
        {
            RequestTimings
                .WithLabels("Error")
                .Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            _sawmill.Error(
                $"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");

            ReportFailure();
            return null;
        }
    }

    public void ResetCache()
    {
        lock (_lock)
        {
            _cache.Clear();
            _cacheOrder.Clear();
        }
    }

    private void TrimCacheLocked()
    {
        while (_cache.Count > _maxCachedCount && _cacheOrder.TryDequeue(out var oldest))
        {
            _cache.Remove(oldest);
        }
    }

    private async Task<bool> TryEnterAsync(CancellationToken ct)
    {
        TaskCompletionSource<bool>? waiter;

        lock (_lock)
        {
            if (_activeRequests < _maxConcurrent)
            {
                _activeRequests++;
                return true;
            }

            if (_waiters.Count >= _maxQueued)
                return false;

            waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Enqueue(waiter);
        }

        await using var registration = ct.Register(() => waiter.TrySetResult(false));
        return await waiter.Task;
    }

    private void Release()
    {
        lock (_lock)
        {
            while (_waiters.TryDequeue(out var waiter))
            {
                if (waiter.TrySetResult(true))
                    return;
            }

            _activeRequests--;
        }
    }

    private enum CircuitState : byte
    {
        Closed,
        Open,
        HalfOpen
    }

    private bool IsCircuitBlocking()
    {
        lock (_lock)
        {
            if (_breakerFailures <= 0)
                return false;

            return _circuit switch
            {
                CircuitState.Open =>
                    DateTime.UtcNow - _circuitOpenedAt < TimeSpan.FromSeconds(_breakerCooldown),
                CircuitState.HalfOpen => true,
                _ => false
            };
        }
    }

    private bool TryPassCircuitBreaker()
    {
        lock (_lock)
        {
            if (_breakerFailures <= 0)
                return true;

            switch (_circuit)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    if (DateTime.UtcNow - _circuitOpenedAt < TimeSpan.FromSeconds(_breakerCooldown))
                        return false;

                    _circuit = CircuitState.HalfOpen;
                    _sawmill.Info("TTS circuit breaker is probing the service");
                    return true;

                case CircuitState.HalfOpen:
                    return false;

                default:
                    return true;
            }
        }
    }

    private void ReportSuccess()
    {
        var recovered = false;

        lock (_lock)
        {
            _consecutiveFailures = 0;

            if (_circuit != CircuitState.Closed)
            {
                _circuit = CircuitState.Closed;
                recovered = true;
            }
        }

        if (recovered)
            _sawmill.Info("TTS circuit breaker is closed, service responds again");
    }

    private void ReportFailure()
    {
        float cooldown;

        lock (_lock)
        {
            if (_breakerFailures <= 0)
                return;

            _consecutiveFailures++;

            if (_circuit != CircuitState.HalfOpen &&
                _consecutiveFailures < _breakerFailures)
            {
                return;
            }

            _circuit = CircuitState.Open;
            _circuitOpenedAt = DateTime.UtcNow;
            cooldown = _breakerCooldown;
        }

        CircuitOpenCount.Inc();
        _sawmill.Warning($"TTS service is unavailable, dropping requests for {cooldown} seconds");
    }

    private static string GenerateCacheKey(string speaker, string text)
    {
        var keyData = Encoding.UTF8.GetBytes($"{speaker}/{text}");
        return Convert.ToHexString(SHA256.HashData(keyData));
    }

    private struct GenerateVoiceRequest
    {
        public GenerateVoiceRequest()
        {
        }

        [JsonPropertyName("api_token")]
        public string ApiToken { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; } = "";

        [JsonPropertyName("ssml")]
        public bool SSML { get; private set; } = true;

        [JsonPropertyName("word_ts")]
        public bool WordTS { get; private set; } = false;

        [JsonPropertyName("put_accent")]
        public bool PutAccent { get; private set; } = true;

        [JsonPropertyName("put_yo")]
        public bool PutYo { get; private set; } = false;

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; private set; } = 24000;

        [JsonPropertyName("format")]
        public string Format { get; private set; } = "ogg";
    }

    private struct GenerateVoiceResponse
    {
        [JsonPropertyName("results")]
        public List<VoiceResult>? Results { get; set; }

        [JsonPropertyName("original_sha1")]
        public string? Hash { get; set; }
    }

    private struct VoiceResult
    {
        [JsonPropertyName("audio")]
        public string? Audio { get; set; }
    }
}
