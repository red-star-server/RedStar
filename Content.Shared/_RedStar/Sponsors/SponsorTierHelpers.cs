using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Sponsors;

public static class SponsorTierHelpers
{
    public static bool HasLoadout(
        IPrototypeManager prototypes,
        SponsorData? data,
        ProtoId<LoadoutPrototype> loadout)
    {
        foreach (var tier in EnumerateTiers(prototypes, data))
        {
            if (tier.Loadouts.Contains(loadout))
                return true;
        }

        return false;
    }

    public static bool HasPriorityJoin(IPrototypeManager prototypes, SponsorData? data)
    {
        foreach (var tier in EnumerateTiers(prototypes, data))
        {
            if (tier.PriorityJoin)
                return true;
        }

        return false;
    }

    public static bool HasOocColor(IPrototypeManager prototypes, SponsorData? data)
    {
        foreach (var tier in EnumerateTiers(prototypes, data))
        {
            if (tier.OocColor)
                return true;
        }

        return false;
    }

    public static bool HasGhostColor(IPrototypeManager prototypes, SponsorData? data)
    {
        foreach (var tier in EnumerateTiers(prototypes, data))
        {
            if (tier.GhostColor)
                return true;
        }

        return false;
    }

    private static IEnumerable<SponsorTierPrototype> EnumerateTiers(
        IPrototypeManager prototypes,
        SponsorData? data)
    {
        if (data == null)
            yield break;

        var current = data.Tier;
        var visited = new HashSet<string>();

        while (prototypes.TryIndex<SponsorTierPrototype>(current, out var tier))
        {
            if (!visited.Add(tier.ID))
                yield break;

            yield return tier;

            if (tier.Parent is not { } parent)
                yield break;

            current = parent.Id;
        }
    }
}
