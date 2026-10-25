using Content.Shared._Trauma.Common.Medical.HealthAnalyzer;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Medical.HealthAnalyzer;

public abstract partial class HealthAnalyzerSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;

    [SubscribeLocalEvent]
    private void OnPartSelected(Entity<HealthAnalyzerComponent> analyzer, ref HealthAnalyzerPartMessage args)
    {
        if (analyzer.Comp.ScannedEntity is not {} target || Deleted(target))
            return;

        if (args.Category is not {} category)
            BeginAnalyzingEntity(analyzer, target);
        else if (_body.GetOrgan(target, category) is {} organ)
            BeginAnalyzingEntity(analyzer, target, organ);
    }

    private void PopulateTraumaState(EntityUid? target, HealthAnalyzerUiState state)
    {
        if (target is not {} body || Deleted(body))
            return;

        state.Body = _wound.GetWoundableStatesOnBody(body);
        state.VitalDamage = _threshold.CheckVitalDamage(body);
        if (state.Body.Count == 0)
            return;

        state.Bleeding.Clear();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (part.Comp.Bleeds > 0 && _body.GetCategory(part.Owner) is {} category)
                state.Bleeding.Add(category);
        }
    }
}
