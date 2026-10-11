using Content.Client.Overlays;
using Content.Shared._RedStar.Records;
using Content.Shared.Access.Systems;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Robust.Client.Player;

namespace Content.Client._RedStar.Records;

public sealed partial class ShowTriageIconsSystem : EquipmentHudSystem<ShowTriageIconsComponent>
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedIdCardSystem _idCards = default!;

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

        var claim = ent.Comp.DoctorName == null
            ? "MedicalTriageIconUnclaimed"
            : _player.LocalEntity is { } local &&
              _idCards.TryFindIdCard(local, out var card) &&
              card.Comp.FullName == ent.Comp.DoctorName
                ? "MedicalTriageIconClaimedYours"
                : "MedicalTriageIconClaimedOthers";
        if (_prototypes.TryIndex<StatusIconPrototype>(claim, out var claimIcon))
            args.StatusIcons.Add(claimIcon);
    }
}
