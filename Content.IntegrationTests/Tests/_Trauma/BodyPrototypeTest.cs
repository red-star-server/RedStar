using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
[TestOf(typeof(BodyPartComponent))]
public sealed class BodyPrototypeTest : GameTest
{
    [SidedDependency(Side.Server)] private IComponentFactory _componentFactory = default!;
    [SidedDependency(Side.Server)] private IPrototypeManager _prototype = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void BodyPartsDoNotHaveTheirOwnCategoryAsSlot()
    {
        using var scope = Assert.EnterMultipleScope();

        foreach (var prototype in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract || Pair.IsTestEntityPrototype(prototype.ID))
                continue;

            if (!prototype.TryComp<BodyPartComponent>(out var part, _componentFactory)
                || !prototype.TryComp<OrganComponent>(out var organ, _componentFactory))
                continue;

            Assert.That(part.Slots, Does.Not.Contain(organ.Category),
                $"{prototype.ID} contains its own organ category {organ.Category} in its slots.");
        }
    }
}
