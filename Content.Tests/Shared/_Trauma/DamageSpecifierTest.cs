using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using NUnit.Framework;

namespace Content.Tests.Shared._Trauma;

[TestFixture]
public sealed class DamageSpecifierTest
{
    [Test]
    public void DamageArithmeticPreservesIndependentMetadata()
    {
        var damage = new DamageSpecifier
        {
            DamageDict = { ["Blunt"] = 10 },
            ArmorPenetration = 0.25f,
            PartDamageVariation = 2f,
            Flags = DamageSpecifier.DamageFlags.PreciseHit,
            WoundSeverityMultipliers = { ["Blunt"] = 2 },
            WoundTypeOverrides = { ["Blunt"] = "WoundBlunt" },
        };
        var scaled = damage * 2;
        Assert.That(scaled.DamageDict["Blunt"], Is.EqualTo((FixedPoint2) 20));
        Assert.That(scaled.ArmorPenetration, Is.EqualTo(damage.ArmorPenetration));
        Assert.That(scaled.PartDamageVariation, Is.EqualTo(damage.PartDamageVariation));
        Assert.That(scaled.Flags, Is.EqualTo(damage.Flags));
        Assert.That(scaled.WoundSeverityMultipliers, Is.EqualTo(damage.WoundSeverityMultipliers));
        scaled.WoundSeverityMultipliers["Blunt"] = 3;
        Assert.That(damage.WoundSeverityMultipliers["Blunt"], Is.EqualTo((FixedPoint2) 2));
    }
}
