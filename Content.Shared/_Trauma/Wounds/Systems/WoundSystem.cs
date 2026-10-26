using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Traumas;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Wounds;

public sealed partial class WoundSystem : EntitySystem
{
    [Dependency] private EntityQuery<BleedInflicterComponent> _bleedQuery;
    [Dependency] private EntityQuery<WoundComponent> _query;
    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery;

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [Dependency] private DamageableSystem _damageable = default!;

    // I'm the one.... who throws........
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private TraumaSystem _trauma = default!;

    private CompName _woundName;

    public override void Initialize()
    {
        base.Initialize();

        _woundName = Factory.CompName<WoundComponent>();
    }
}
