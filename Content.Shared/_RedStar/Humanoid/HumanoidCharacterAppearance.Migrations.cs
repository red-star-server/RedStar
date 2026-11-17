using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidCharacterAppearance
{
    private static readonly ISawmill Sawmill = Logger.GetSawmill("humanoid");

    private static void MigrateMarkingsToLayerOwners(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> validatedMarkings,
        Dictionary<ProtoId<OrganCategoryPrototype>, EntProtoId<OrganComponent>> organs,
        MarkingManager markingManager)
    {
        var layerOwners = new Dictionary<HumanoidVisualLayers, ProtoId<OrganCategoryPrototype>>();
        foreach (var (organ, organProtoId) in organs)
        {
            if (!markingManager.TryGetMarkingData(organProtoId, out var organData))
                continue;

            foreach (var layer in organData.Value.Layers)
            {
                if (!layerOwners.TryGetValue(layer, out var owner) || (owner == "Torso" && organ != "Torso"))
                {
                    layerOwners[layer] = organ;
                    continue;
                }

                if (owner != "Torso" && organ != "Torso")
                    Sawmill.Warning($"Visual layer {layer} is claimed by both {owner} and {organ}; keeping {owner}.");
            }
        }

        var working = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        foreach (var (organ, sets) in markings)
        {
            foreach (var (layer, layerMarkings) in sets)
            {
                if (!layerOwners.TryGetValue(layer, out var owner) || owner == organ)
                    continue;

                Working(organ).Remove(layer);

                var destination = Working(owner);
                var moved = destination.GetValueOrDefault(layer)?.ShallowClone() ?? [];
                moved.AddRange(layerMarkings);
                destination[layer] = moved;
            }
        }

        foreach (var (organ, sets) in working)
        {
            validatedMarkings[organ] = sets;
        }

        return;

        Dictionary<HumanoidVisualLayers, List<Marking>> Working(ProtoId<OrganCategoryPrototype> organ)
        {
            if (working.TryGetValue(organ, out var sets))
                return sets;

            sets = validatedMarkings.GetValueOrDefault(organ)?.ShallowClone() ?? new();
            working[organ] = sets;

            return sets;
        }
    }
}
