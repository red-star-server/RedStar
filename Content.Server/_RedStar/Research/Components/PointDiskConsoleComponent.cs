using Content.Server._RedStar.Research.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Research.Components;

[RegisterComponent]
[Access(typeof(PointDiskConsoleSystem))]
public sealed partial class PointDiskConsoleComponent : Component
{
    /// <summary>
    /// Allowed point disks. Their ResearchDisk component defines both cost and value.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Disks = [];

    [DataField]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier? PrintSound;
}
