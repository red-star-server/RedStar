namespace Content.Shared._Trauma.Common.Surgery.Tools;

// TODO: replace with tool qualities...
public abstract partial class BaseSurgeryToolComponent : Component
{
    [ViewVariables]
    public abstract string ToolName { get; }

    /// <summary>
    ///     Field intended for discardable or non-reusable tools.
    /// </summary>
    [DataField]
    public bool? Used;

    /// <summary>
    ///     Multiply the step's doafter by this value.
    ///     This is per-type so you can have something that's a good scalpel but a bad bonesaw.
    /// </summary>
    [DataField]
    public float Speed = 1f;
}
