using Robust.Shared.Audio;

namespace Content.Shared._RedStar.Xenobiology.Components;

[RegisterComponent]
public sealed partial class XenobiologyScannerComponent : Component
{
    [DataField]
    public SoundSpecifier ScanSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");
}
