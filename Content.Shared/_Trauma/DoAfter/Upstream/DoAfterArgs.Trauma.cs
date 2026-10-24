namespace Content.Shared.DoAfter;

public sealed partial class DoAfterArgs
{
    /// <summary>
    /// Whether the delay multiplier event should be raised.
    /// </summary>
    [DataField]
    public bool MultiplyDelay = true;
}
