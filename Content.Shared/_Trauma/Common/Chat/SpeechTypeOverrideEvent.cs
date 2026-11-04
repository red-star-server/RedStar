namespace Content.Shared.Chat;

/// <summary>Raised before sending speech to allow its type to be overridden.</summary>
[ByRefEvent]
public record struct SpeechTypeOverrideEvent(InGameICChatType DesiredType);
