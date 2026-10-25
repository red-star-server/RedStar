using Content.Shared._Trauma.Traumas;
using Content.Shared.FixedPoint;
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
    [Dependency] private TraumaSystem _trauma = default!;

    public static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";

    [SubscribeLocalEvent]
    private void OnOrganEnabled(Entity<EyesComponent> ent, ref OrganEnabledEvent args)
    {
        if (!TryComp<InternalChildOrganComponent>(ent, out var organ) ||
            !TryComp(args.Body, out BlindableComponent? blindable))
            return;

        // Replace the body's eye damage with the enabled organ's current damage.
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

    [SubscribeLocalEvent]
    private void OnOrganIntegrityChanged(Entity<EyesComponent> ent, ref OrganIntegrityChangedEvent args)
    {
        if (args.NewIntegrity <= 0 ||
            _body.GetBody(ent.Owner) is not {} body ||
            !TryComp<InternalChildOrganComponent>(ent, out var organ) ||
            !TryComp<BlindableComponent>(body, out var blindable) ||
            organ.OrganIntegrity <= 0) // eyes should be disabled for blinding
            return;

        _blindable.SetEyeDamage((body, blindable), (int) (organ.IntegrityCap - organ.OrganIntegrity));
    }

    [SubscribeLocalEvent]
    private void OnEyesDamaged(Entity<EyesComponent> ent, ref BodyRelayedEvent<EyesDamagedEvent> args)
    {
        if (!TryComp<InternalChildOrganComponent>(ent, out var organ))
            return;

        const string identifier = "BlindableDamage";
        var previous = organ.IntegrityModifiers.GetValueOrDefault((identifier, args.Args.Body));
        var change = FixedPoint2.Max(previous + args.Args.Damage, 0) - previous;
        if (!_trauma.TryChangeOrganDamageModifier(ent.Owner, change, args.Args.Body, identifier))
            _trauma.TryCreateOrganDamageModifier(ent.Owner, change, args.Args.Body, identifier);
    }

    private void CheckMissingEyes(EntityUid body)
    {
        if (TerminatingOrDeleted(body))
            return;

        if (_body.GetOrgan(body, EyesCategory) is {} eyes && HasComp<EnabledOrganComponent>(eyes))
            return;

        // can't see without eyes
        if (TryComp<BlindableComponent>(body, out var blindable))
            _blindable.SetEyeDamage((body, blindable), blindable.MaxDamage);
    }
}
