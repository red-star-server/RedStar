#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Content.Shared._RedStar.Beekeeping.Systems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// The weights of a mutagen roll move with the queen's instability: bad traits come likelier, good ones rarer.
/// </summary>
[TestOf(typeof(BeeQueenComponent))]
public sealed class QueenInstabilityTest : GameTest
{
    private static readonly ProtoId<BeeTraitPrototype> Productive = "Productive";
    private static readonly ProtoId<BeeTraitPrototype> Lazy = "Lazy";

    [Test]
    public async Task RollWeights()
    {
        var proto = Server.ProtoMan;
        await Server.WaitAssertion(() =>
        {
            var good = proto.Index(Productive);
            var bad = proto.Index(Lazy);
            Assert.Multiple(() =>
            {
                Assert.That(good.Bad, Is.False);
                Assert.That(bad.Bad);

                // A fresh queen: the plain weights.
                Assert.That(SharedBeehiveSystem.RollWeight(good, false, 0), Is.EqualTo(good.Weight));
                Assert.That(SharedBeehiveSystem.RollWeight(bad, false, 0), Is.EqualTo(bad.Weight));

                // Five rolls in: a bad trait is twice as likely, a good one half as likely, a good one is lost 2x as often.
                Assert.That(SharedBeehiveSystem.RollWeight(bad, false, 5), Is.EqualTo(bad.Weight * 2f).Within(0.001f));
                Assert.That(SharedBeehiveSystem.RollWeight(good, false, 5), Is.EqualTo(good.Weight * 0.5f).Within(0.001f));
                Assert.That(SharedBeehiveSystem.RollWeight(good, true, 5), Is.EqualTo(good.LossWeight * 2f).Within(0.001f));

                // Losing a bad trait does not change; gaining a good one never drops under 0.3.
                Assert.That(SharedBeehiveSystem.RollWeight(bad, true, 5), Is.EqualTo(bad.LossWeight));
                Assert.That(SharedBeehiveSystem.RollWeight(good, false, 50), Is.EqualTo(good.Weight * 0.3f).Within(0.001f));
            });
        });
    }

    /// <summary>
    /// The two new traits add to what the hive does when it is hit.
    /// </summary>
    [Test]
    public async Task AngryTraits()
    {
        var hive = Server.System<Content.Server._RedStar.Beekeeping.Systems.BeehiveSystem>();
        await Server.WaitAssertion(() =>
        {
            var hot = hive.GetTotals(new ProtoId<BeeTraitPrototype>[] { "Hotheaded" });
            var guarded = hive.GetTotals(new ProtoId<BeeTraitPrototype>[] { "GuardedScout" });
            var plain = hive.GetTotals(System.Array.Empty<ProtoId<BeeTraitPrototype>>());
            Assert.Multiple(() =>
            {
                Assert.That(hot.AngryPerHit, Is.EqualTo(1));
                Assert.That(hot.AngryDamageStep, Is.EqualTo(0.5f));
                Assert.That(hot.GuardedScout, Is.False);
                Assert.That(guarded.GuardedScout);
                Assert.That(plain.AngryPerHit, Is.Zero);
                Assert.That(plain.AngryDamageStep, Is.EqualTo(1f));
            });
        });
    }
}
