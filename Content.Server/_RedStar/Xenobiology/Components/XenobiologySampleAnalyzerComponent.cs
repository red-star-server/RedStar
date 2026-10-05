using Robust.Shared.Audio;

namespace Content.Server._RedStar.Xenobiology.Components;

[RegisterComponent]
public sealed partial class XenobiologySampleAnalyzerComponent : Component
{
    [DataField]
    public string SampleSlot = "sample";

    [DataField]
    public TimeSpan AnalysisDuration = TimeSpan.FromSeconds(8);

    [DataField]
    public SoundSpecifier CompletionSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    public EntityUid? ProcessingSample;
    public EntityUid? ProcessingServer;
    public TimeSpan? AnalysisEndTime;
    public TimeSpan RemainingAnalysisTime;
    public TimeSpan NextUiUpdate;
    public TimeSpan? CompleteUntil;
    public int? LastReward;
}
