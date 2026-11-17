using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidCharacterAppearance
{
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
                    layerOwners[layer] = organ;
            }
        }

        foreach (var (organ, sets) in markings)
        {
            Dictionary<HumanoidVisualLayers, List<Marking>>? source = null;

            foreach (var (layer, layerMarkings) in sets)
            {
                if (!layerOwners.TryGetValue(layer, out var owner) || owner == organ)
                    continue;

                source ??= validatedMarkings[organ].ShallowClone();
                source.Remove(layer);

                var destination = validatedMarkings.GetValueOrDefault(owner)?.ShallowClone() ?? new();
                var moved = destination.GetValueOrDefault(layer)?.ShallowClone() ?? [];
                moved.AddRange(layerMarkings);
                destination[layer] = moved;
                validatedMarkings[owner] = destination;
            }

            if (source is not null)
                validatedMarkings[organ] = source;
        }
    }
}
