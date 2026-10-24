using Content.Client.Overlays;
using Content.Shared._RedStar.Records;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._RedStar.Records;

public sealed partial class ShowTriageIconsSystem : EquipmentHudSystem<ShowTriageIconsComponent>
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnGetStatusIcons(Entity<MedicalTriageStatusComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        var id = ent.Comp.Priority switch
        {
            MedicalTriagePriority.Minor => "MedicalTriageIconMinor",
            MedicalTriagePriority.Delayed => "MedicalTriageIconDelayed",
            MedicalTriagePriority.Immediate => "MedicalTriageIconImmediate",
            MedicalTriagePriority.Expectant => "MedicalTriageIconExpectant",
            _ => null
        };

        if (id != null && _prototypes.TryIndex<StatusIconPrototype>(id, out var icon))
            args.StatusIcons.Add(icon);
    }
}
