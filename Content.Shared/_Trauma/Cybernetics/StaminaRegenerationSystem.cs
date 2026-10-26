using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Network;

namespace Content.Shared._Trauma.Cybernetics;

// Wizards has no ToggleStaminaDrain API; apply the donor's rate through its stamina API.
public sealed partial class StaminaRegenerationSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private INetManager _net = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<StaminaRegenerationComponent, StaminaComponent>();
        while (query.MoveNext(out var uid, out var regeneration, out var stamina))
        {
            if (stamina.Critical || stamina.StaminaDamage <= 0f && regeneration.RegenerationRate <= 0f)
                continue;

            _stamina.TakeStaminaDamage(uid, regeneration.RegenerationRate * frameTime,
                stamina, visual: false, ignoreResist: true);
        }
    }
}
