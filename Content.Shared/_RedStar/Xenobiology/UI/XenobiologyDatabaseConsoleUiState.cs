using Content.Shared._RedStar.Xenobiology.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum XenobiologyDatabaseConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct XenobiologyDatabaseEntry(EntProtoId<XenobiologySampleComponent> Sample, int ResearchValue);

[Serializable, NetSerializable]
public sealed class XenobiologyDatabaseConsoleUiState(bool connected, XenobiologyDatabaseEntry[] entries) : BoundUserInterfaceState
{
    public bool Connected { get; } = connected;
    public XenobiologyDatabaseEntry[] Entries { get; } = entries;
}
