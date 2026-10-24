using System.Linq; // RS14
using Content.Shared.Body; // RS14
using Content.Shared._Trauma.Body; // RS14
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;

namespace Content.IntegrationTests.Tests.Damageable;

/// <summary>
/// Major part of why we need this test is to check that entities marked as 'Injurable' have 'Damageable' sister component,
/// because they work in pairs within current damage system. Please be careful when modifying test for special cases
/// of having 'Damageable' without proper 'Injurable'. In future updates, when there will be entities that
/// have damage model not relying on our simple 'Injurable' implementation, test must be improved to validate
/// that there at least one way of handling damage attached to entity.
/// </summary>
[TestFixture]
[TestOf(typeof(InjurableComponent))]
[TestOf(typeof(DamageableComponent))]
[TestOf(typeof(DamageableSystem))]
public sealed class DamageAllPrototypesTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damageableSystem = default!;

    [Test]
    [TestOf(typeof(DamageableSystem))]
    [Description("Ensures all Entity Prototypes with damageable can be damaged.")]
    public async Task TestInjurableComponentOwnersCanTakeDamage()
    {
        var map = await Pair.CreateTestMap();

        try
        {
            foreach (var injurable in GameDataScrounger.EntitiesWithComponent("Injurable"))
            {
                var entity = await SpawnAtPosition(injurable, map.GridCoords);

                try
                {
                    // Intentionally cannot take damage, ignore it.
                    if (SEntMan.HasComponent<GodmodeComponent>(entity))
                        continue;

                    var canBeDamaged = false;

                    foreach (var type in SProtoMan.EnumeratePrototypes<DamageTypePrototype>())
                    {
                        if (!_damageableSystem.CanBeDamagedBy(entity, type))
                            continue;

                        // RS14-start: body-level Injurable describes the old model; parts have their own containers.
                        var parts = SEntMan.System<BodyPartSystem>();
                        if (parts.GetRootPart(entity) != null && !SEntMan.System<BodySystem>()
                                .GetExternalOrgans(entity).Any(part => _damageableSystem.CanBeDamagedBy(part.Owner, type)))
                            continue;
                        // RS14-end
                        canBeDamaged = true;

                        await Server.WaitAssertion(() =>
                        {
                            // RS14-start: vital damage is split between torso and head; one epsilon rounds to zero.
                            var testDamage = FixedPoint2.Epsilon * 2;
                            var damage = new DamageSpecifier(type, testDamage);
                            // RS14-end
                            var previousDamage = _damageableSystem.GetTotalDamage(entity);
                            _damageableSystem.ChangeDamage(entity, damage, ignoreResistances: true);
                            Assert.That(
                                _damageableSystem.GetTotalDamage(entity),
                                Is.EqualTo(testDamage + previousDamage), // RS14
                                $"{injurable} should take {type.ID} damage.");

                            _damageableSystem.ClearAllDamage(entity);
                        });
                    }

                    // Ensure that this entity can actually be damaged.
                    Assert.That(canBeDamaged, Is.True, $"{injurable} cannot be damaged by any damage type.");
                }
                finally
                {
                    await Server.WaitPost(() => SEntMan.DeleteEntity(entity));
                }
            }
        }
        finally
        {
            await Server.WaitPost(() => SEntMan.DeleteEntity(map.MapUid));
        }
    }
}
