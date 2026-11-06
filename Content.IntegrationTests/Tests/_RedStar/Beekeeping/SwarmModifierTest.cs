#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// The swarm quirks add up to a clean 100 and the ones that take a share of the colony stay sensible.
/// </summary>
[TestOf(typeof(SwarmModifierPrototype))]
public sealed class SwarmModifierTest : GameTest
{
    private static readonly ProtoId<SwarmModifierPrototype> Plain = "Plain";

    [Test]
    public async Task WeightsAndShares()
    {
        var proto = Server.ProtoMan;
        await Server.WaitAssertion(() =>
        {
            var modifiers = proto.EnumeratePrototypes<SwarmModifierPrototype>().ToList();
            Assert.That(modifiers.Sum(m => m.Weight), Is.EqualTo(100f).Within(0.01f));
            Assert.That(modifiers.Select(m => m.ColonyShare), Has.All.InRange(0.1f, 0.9f));
            Assert.That(proto.Index(Plain).Weight, Is.EqualTo(modifiers.Max(m => m.Weight)), "A plain swarm is the most common.");
        });
    }
}
