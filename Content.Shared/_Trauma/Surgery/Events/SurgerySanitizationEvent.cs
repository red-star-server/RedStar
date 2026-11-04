namespace Content.Shared._Trauma.Surgery;

[ByRefEvent]
public record struct SurgerySanitizationEvent(bool Handled = false);
