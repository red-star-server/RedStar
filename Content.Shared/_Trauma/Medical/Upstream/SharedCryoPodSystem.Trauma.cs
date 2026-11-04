using Content.Shared.Bed.Sleep;
using Content.Shared.Medical.HealthAnalyzer;

namespace Content.Shared.Medical.Cryogenics;

public abstract partial class SharedCryoPodSystem
{
    [Dependency] private SleepingSystem _sleeping = default!;

    private void ResetAnalyzedPart(Entity<CryoPodComponent> pod)
    {
        if (!TryComp<HealthAnalyzerComponent>(pod, out var analyzer))
            return;

        analyzer.CurrentBodyPart = null;
        Dirty(pod.Owner, analyzer);
    }
}
