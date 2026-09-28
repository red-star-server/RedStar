namespace Content.Server._RedStar.Xenobiology;

[RegisterComponent]
public sealed partial class XenobiologySampleAnalyzerComponent : Component
{
    [DataField]
    public string SampleSlot = "sample";

    [DataField]
    public TimeSpan AnalysisTime = TimeSpan.FromSeconds(2);
}
