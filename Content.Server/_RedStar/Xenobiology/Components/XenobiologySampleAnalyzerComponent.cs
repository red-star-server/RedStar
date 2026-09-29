using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Shared.Audio;

namespace Content.Server._RedStar.Xenobiology.Components;

[RegisterComponent]
public sealed partial class XenobiologySampleAnalyzerComponent : Component
{
    [DataField]
    public string SampleSlot = "sample";

    [DataField]
    public SoundSpecifier CompletionSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    public XenobiologyAnalysisResult? Result;

    public XenobiologySampleAnalyzerUiState? LastState;
}
