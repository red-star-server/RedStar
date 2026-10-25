using System.Linq;
using Content.Shared._Trauma.Common.Body;
using Content.Shared.Localizations;
using Robust.Shared.Utility;

namespace Content.Shared.Armor;

public abstract partial class SharedArmorSystem
{
    private FormattedMessage GetArmorExamine(ArmorComponent component)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("armor-examine"));

        if (!component.Modifiers.Coefficients.Any() && !component.Modifiers.FlatReductions.Any())
            return msg;

        var coverage = component.ArmorCoverage;
        var armorModifiers = component.Modifiers;

        if (!component.ArmourCoverageHidden)
        {
            // <Trauma>
            var coveredParts = coverage.Where(coveragePart => coveragePart != BodyPartType.Other).ToList();
            List<string> coverageText = [];
            foreach (var part in coveredParts)
            {
                coverageText.Add(Loc.GetString("armor-coverage-type-" + part.ToString().ToLower()));
            }

            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("armor-coverage-value", ("type", ContentLocalizationManager.FormatList(coverageText))));
            // </Trauma>
        }

        if (component.ArmourModifiersHidden) return msg;
        foreach (var coefficientArmor in armorModifiers.Coefficients)
        {
            msg.PushNewline();
            // TODO: probably make these prototype fields or have a test that they all exist
            var armorType = Loc.GetString("armor-damage-type-" + coefficientArmor.Key.Id.ToLower());
            msg.AddMarkupOrThrow(Loc.GetString("armor-coefficient-value-trauma", // Trauma - better locale string
                ("type", armorType),
                ("value", MathF.Abs(1f - coefficientArmor.Value) * 100), ("protect", coefficientArmor.Value < 1f) // Trauma - better values
            ));
        }

        foreach (var flatArmor in armorModifiers.FlatReductions)
        {
            msg.PushNewline();

            var armorType = Loc.GetString("armor-damage-type-" + flatArmor.Key.Id.ToLower());
            msg.AddMarkupOrThrow(Loc.GetString("armor-reduction-value",
                ("type", armorType),
                ("value", flatArmor.Value)
            ));
        }

        return msg;
    }
}
