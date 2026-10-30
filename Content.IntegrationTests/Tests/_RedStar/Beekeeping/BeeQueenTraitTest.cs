#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared._RedStar.Beekeeping.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// What a queen does with what is injected into her: mutagen rolls traits and is used up, royal jelly adds slots up
/// to the maximum, anything else is payload only with a sting.
/// </summary>
[TestOf(typeof(BeeQueenComponent))]
public sealed class BeeQueenTraitTest : GameTest
{
    [Test]
    public async Task MutagenJellyAndPayload()
    {
        var server = Server;
        var solutions = server.System<SharedSolutionContainerSystem>();
        EntityUid queen = default;

        await server.WaitPost(() =>
        {
            queen = server.EntMan.SpawnEntity("BeeQueen", MapCoordinates.Nullspace);
            var comp = server.EntMan.GetComponent<BeeQueenComponent>(queen);
            Assert.That(comp.Traits, Is.EquivalentTo(new[] { "PestControl" }));

            Assert.That(solutions.TryGetSolution(queen, comp.Solution, out var soln, out _));
            solutions.TryAddReagent(soln!.Value, comp.Mutagen.Id, comp.MutagenPerRoll * 3, out _);
        });
        await RunTicksSync(5);

        // One injection at a time: the queen's solution is small, and each one is used up before the next.
        foreach (var (reagent, amount) in new[] { ("RoyalJelly", 20), ("Water", 5) })
        {
            await server.WaitPost(() =>
            {
                var comp = server.EntMan.GetComponent<BeeQueenComponent>(queen);
                Assert.That(solutions.TryGetSolution(queen, comp.Solution, out var soln, out _));
                Assert.That(solutions.TryAddReagent(soln!.Value, reagent, amount, out _));
            });
            await RunTicksSync(5);
        }

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeeQueenComponent>(queen);
            solutions.TryGetSolution(queen, comp.Solution, out _, out var solution);

            Assert.Multiple(() =>
            {
                // Used up at once, nothing left over towards the next roll or slot.
                Assert.That(solution!.GetTotalPrototypeQuantity(comp.Mutagen).Float(), Is.Zero);
                Assert.That(solution.GetTotalPrototypeQuantity(comp.Jelly).Float(), Is.Zero);
                Assert.That(comp.MutagenProgress.Float(), Is.Zero);

                // One slot per portion of jelly, never past the maximum.
                Assert.That(comp.Slots, Is.EqualTo(comp.MaxSlots));

                // Water stays as payload only if the rolls gave her a sting.
                var sting = comp.Traits.Contains("Sting");
                Assert.That(solution.Volume.Float(), Is.EqualTo(sting ? 5f : 0f));
            });
        });
    }
}
