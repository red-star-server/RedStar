using Robust.Shared.Utility;

namespace Content.Shared.Entry;

public sealed partial class EntryPoint
{
    private void InitializeTraumaPrototypes()
    {
        _prototypeManager.PartialDirectory(new ResPath("/Prototypes/_RedStar/Partials/Medical"), 1);
    }
}
