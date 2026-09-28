namespace Content.Server._RedStar.Xenobiology;

[RegisterComponent]
public sealed partial class XenobiologySampleAnalyzerComponent : Component
{
    [DataField]
    public string SampleSlot = "sample";

    [DataField]
    public float AnalysisTime = 2f;
}
