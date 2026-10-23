using Robust.Shared.Prototypes;

using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared._Trauma.Body;

public sealed partial class EyesSystem : EntitySystem
{
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private BodySystem _body = default!;

    public static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";

    [SubscribeLocalEvent]
    private void OnOrganEnabled(Entity<EyesComponent> ent, ref OrganEnabledEvent args)
    {
        if (!TryComp<InternalChildOrganComponent>(ent, out var organ) ||
            !TryComp(args.Body, out BlindableComponent? blindable))
            return;

        // We add the current eye damage since in any context, the organ being enabled means that it was
        // either removed or disabled, so the BlindableComponent must have some prior damage already.
        var adjustment = (int)(organ.IntegrityCap - organ.OrganIntegrity);
        _blindable.SetEyeDamage((args.Body, blindable), adjustment);
    }

    [SubscribeLocalEvent]
    private void OnOrganDisabled(Entity<EyesComponent> ent, ref OrganDisabledEvent args)
    {
        if (TerminatingOrDeleted(args.Body))
            return; // can't see anyway if you are being deleted

        CheckMissingEyes(args.Body);
    }

    private void CheckMissingEyes(EntityUid body)
    {
        if (TerminatingOrDeleted(body))
            return;

        if (_body.GetOrgan(body, EyesCategory) != null)
            return;

        // can't see without eyes
        if (TryComp<BlindableComponent>(body, out var blindable))
            _blindable.SetEyeDamage((body, blindable), blindable.MaxDamage);
    }
}
