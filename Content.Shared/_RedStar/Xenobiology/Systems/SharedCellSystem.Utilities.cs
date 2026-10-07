using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Systems;

public abstract partial class SharedCellSystem
{
    public string GetCellTraitsString(IEnumerable<ProtoId<CellTraitPrototype>> traits)
    {
        var lines = new List<string>();

        foreach (var traitId in traits)
        {
            if (!_prototype.TryIndex(traitId, out var trait))
                continue;

            var color = trait.Color.A == 0
                ? Color.White
                : trait.Color;
            lines.Add(Loc.GetString("cell-sequencer-menu-cell-trait-message",
                ("name", Loc.GetString(trait.Name)),
                ("color", color.ToHex())));
        }

        return string.Join("\n", lines);
    }
}
