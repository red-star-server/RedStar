using Content.Shared._RedStar.Xenobiology;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;

namespace Content.Server._RedStar.Xenobiology;

[RegisterComponent]
public sealed partial class XenobiologySampleAnalyzerComponent : Component
{
    [DataField]
    public string SampleSlot = "sample";

    [DataField]
    public TimeSpan AnalysisTime = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier CompletionSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [ViewVariables]
    public DoAfterId? AnalysisDoAfter;

    /// <summary>
    /// Identifies the current attempt, including instant do-afters and delayed cancellation events.
    /// </summary>
    public int AnalysisGeneration;

    [ViewVariables]
    public EntityUid? AnalysisServer;

    [ViewVariables]
    public EntityUid? AnalysisSample;

    [ViewVariables]
    public TimeSpan? AnalysisStart;

    [ViewVariables]
    public TimeSpan? AnalysisEnd;

    public XenobiologyAnalysisResult? Result;

    public XenobiologySampleAnalyzerUiState? LastState;
}
