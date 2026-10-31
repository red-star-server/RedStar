using Content.Shared._Trauma.Common.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared.Body.Systems;

public sealed partial class BloodstreamSystem
{
    [Dependency] private IConfigurationManager _medicalCfg = default!;
    private float _bloodlossMultiplier = 4f;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_medicalCfg, TraumaCVars.BleedMultiplier, value => _bloodlossMultiplier = value, true);
    }
}
