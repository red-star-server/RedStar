using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Targeting;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Body;

/// <summary>
/// Trauma - organ enabled logic, extra helpers for working with body stuff
/// </summary>
public sealed partial class BodySystem
{
    [Dependency] private CommonBodyCacheSystem _cache = default!;
    [Dependency] private CommonBodyPartSystem _part = default!;
    [Dependency] private OrganRelationSystem _relation = default!;
    [Dependency] private EntityQuery<InternalChildOrganComponent> _internalQuery;

    /// <summary>
    /// Body parts' organ categories.
    /// </summary>
    public static readonly ProtoId<OrganCategoryPrototype>[] BodyParts =
    [
        "Head",
        "Torso",
        "ArmLeft",
        "LegLeft",
        "HandLeft",
        "FootLeft",
        "ArmRight",
        "LegRight",
        "HandRight",
        "FootRight"
    ];

    /// <summary>
    /// Map between a body part type and the organ categories with that type, regardless of symmetry.
    /// </summary>
    public static readonly Dictionary<BodyPartType, ProtoId<OrganCategoryPrototype>[]> PartTypeOrgans = new()
    {
        {BodyPartType.Head, [ "Head" ]},
        {BodyPartType.Torso, [ "Torso" ]},
        {BodyPartType.Arm, [ "ArmLeft", "ArmRight" ]},
        {BodyPartType.Hand, [ "HandLeft", "HandRight" ]},
        {BodyPartType.Leg, [ "LegLeft", "LegRight" ]},
        {BodyPartType.Foot, [ "FootLeft", "FootRight" ]},
        {BodyPartType.Tail, [ "Tail" ]},
        {BodyPartType.Wings, [ "Wings" ]}
    };

    /// <summary>
    /// Vital body parts' organ categories.
    /// </summary>
    public static readonly ProtoId<OrganCategoryPrototype>[] VitalParts =
    [
        "Head",
        "Torso"
    ];
    // TODO: vital internal organs???

    private readonly HashSet<ProtoId<OrganCategoryPrototype>> _coveredParts = new();

    /// <summary>
    /// Tries to enable a given organ, letting systems run logic.
    /// Returns true if it is valid and now enabled.
    /// </summary>
    public bool EnableOrgan(Entity<OrganComponent?> organ, EntityUid? bodyUid = null, bool logMissing = true)
    {
        // allow the user to pass in a body incase it's null here
        if (!_organQuery.Resolve(organ, ref organ.Comp, logMissing) || (bodyUid ?? organ.Comp.Body) is not {} body)
            return false;

        if (HasComp<EnabledOrganComponent>(organ))
            return true; // already enabled

        var attemptEv = new OrganEnableAttemptEvent(body);
        RaiseLocalEvent(organ, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        EnsureComp<EnabledOrganComponent>(organ);

        // let other systems do their logic now
        var ev = new OrganEnabledEvent(body);
        RaiseLocalEvent(organ, ref ev);
        return true;
    }

    /// <summary>
    /// Disabled a given organ, letting systems run logic.
    /// Returns true if it is valid and now disabled.
    /// </summary>
    public bool DisableOrgan(Entity<OrganComponent?> organ, EntityUid? bodyUid = null)
    {
        if (!_organQuery.Resolve(organ, ref organ.Comp) || (bodyUid ?? organ.Comp.Body) is not {} body)
            return false;

        if (!TryComp<EnabledOrganComponent>(organ, out var enabled))
            return true; // already disabled

        // no attempt event that wouldn't make any sense

        RemComp(organ, enabled);

        var ev = new OrganDisabledEvent(body);
        RaiseLocalEvent(organ, ref ev);
        return true;
    }

    /// <summary>
    /// Gets the body's organs which have the requested component.
    /// </summary>
    public List<Entity<T>> GetOrgans<T>(Entity<BodyComponent?> body, bool logMissing = false) where T : Component
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, logMissing))
            return [];

        var organs = new List<Entity<T>>();
        foreach (var organ in EnumerateOrgans<T>(body))
        {
            organs.Add((organ.Owner, organ.Comp2));
        }
        return organs;
    }

    /// <summary>
    /// Get all organs in a body, both internal and external.
    /// </summary>
    public List<Entity<OrganComponent>> GetOrgans(Entity<BodyComponent?> body, bool logMissing = false) => GetOrgans<OrganComponent>(body, logMissing);

    /// <summary>
    /// Get all internal organs in a body.
    /// </summary>
    public List<Entity<InternalChildOrganComponent>> GetInternalOrgans(Entity<BodyComponent?> body, bool logMissing = false) => GetOrgans<InternalChildOrganComponent>(body, logMissing);

    /// <summary>
    /// Get all external organs in a body.
    /// </summary>
    public List<Entity<OrganComponent>> GetExternalOrgans(Entity<BodyComponent?> body, bool logMissing = false)
    {
        var organs = GetOrgans(body, logMissing);
        organs.RemoveAll(organ => _internalQuery.HasComp(organ));
        return organs;
    }

    /// <summary>
    /// Get a list of vital bodyparts, which contribute to vital damage.
    /// </summary>
    public List<EntityUid> GetVitalParts(EntityUid body)
    {
        // doing this to use cache instead of looping every organ and checking them
        var vital = new List<EntityUid>(VitalParts.Length);
        foreach (var category in VitalParts)
        {
            if (_cache.GetOrgan(body, category) is {} organ)
                vital.Add(organ);
        }

        return vital;
    }

    /// <summary>
    /// Gets the fraction of bodyparts that are vital.
    /// For a torso or torso+head this is 1, for invalid bodies this is 0.
    /// Non-bodies will return 1 for damage scaling etc.
    /// </summary>
    public float GetVitalBodyPartRatio(Entity<BodyComponent?> body)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) || body.Comp.Organs?.ContainedEntities is not {} organs)
            return 1f;

        // TODO SHITMED: change vital to just be a bool on OrganCategoryPrototype
        int total = 0;
        int vital = 0;
        foreach (var organ in organs)
        {
            if (GetCategory(organ) is not {} category || !BodyParts.Contains(category))
                continue;

            total++;
            if (VitalParts.Contains(category))
                vital++;
        }

        return vital == 0
            ? 0f // no dividing by zero incase a body somehow has no parts?!
            : (float) total / vital;
    }

    /// <summary>
    /// Get the number of vital parts for an entity, falls back to 1 for non-mobs.
    /// </summary>
    public int GetVitalParts(Entity<BodyComponent?> body)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) || body.Comp.Organs?.ContainedEntities is not {} organs)
            return 1;

        int vital = 0;
        foreach (var organ in organs)
        {
            if (GetCategory(organ) is {} category && VitalParts.Contains(category))
                vital++;
        }

        return vital;
    }

    /// <summary>
    /// Converts Enums from BodyPartType to their Targeting system equivalent.
    /// </summary>
    public TargetBodyPart GetTargetBodyPart(BodyPartType type, BodyPartSymmetry symmetry)
    {
        return (type, symmetry) switch
        {
            (BodyPartType.Head, _) => TargetBodyPart.Head,
            (BodyPartType.Torso, _) => TargetBodyPart.Chest,
            (BodyPartType.Arm, BodyPartSymmetry.Left) => TargetBodyPart.LeftArm,
            (BodyPartType.Arm, BodyPartSymmetry.Right) => TargetBodyPart.RightArm,
            (BodyPartType.Hand, BodyPartSymmetry.Left) => TargetBodyPart.LeftHand,
            (BodyPartType.Hand, BodyPartSymmetry.Right) => TargetBodyPart.RightHand,
            (BodyPartType.Leg, BodyPartSymmetry.Left) => TargetBodyPart.LeftLeg,
            (BodyPartType.Leg, BodyPartSymmetry.Right) => TargetBodyPart.RightLeg,
            (BodyPartType.Foot, BodyPartSymmetry.Left) => TargetBodyPart.LeftFoot,
            (BodyPartType.Foot, BodyPartSymmetry.Right) => TargetBodyPart.RightFoot,
            (BodyPartType.Tail, _) => TargetBodyPart.Tail,
            (BodyPartType.Wings, _) => TargetBodyPart.Wings,
            _ => TargetBodyPart.Chest
        };
    }

    /// <summary>
    /// Converts Enums from Targeting system to their BodyPartType equivalent.
    /// </summary>
    public (BodyPartType, BodyPartSymmetry) ConvertTargetBodyPart(TargetBodyPart? targetPart)
    {
        return targetPart switch
        {
            TargetBodyPart.Head => (BodyPartType.Head, BodyPartSymmetry.None),
            TargetBodyPart.Chest => (BodyPartType.Torso, BodyPartSymmetry.None),
            TargetBodyPart.Groin => (BodyPartType.Torso, BodyPartSymmetry.None),
            TargetBodyPart.LeftArm => (BodyPartType.Arm, BodyPartSymmetry.Left),
            TargetBodyPart.LeftHand => (BodyPartType.Hand, BodyPartSymmetry.Left),
            TargetBodyPart.RightArm => (BodyPartType.Arm, BodyPartSymmetry.Right),
            TargetBodyPart.RightHand => (BodyPartType.Hand, BodyPartSymmetry.Right),
            TargetBodyPart.LeftLeg => (BodyPartType.Leg, BodyPartSymmetry.Left),
            TargetBodyPart.LeftFoot => (BodyPartType.Foot, BodyPartSymmetry.Left),
            TargetBodyPart.RightLeg => (BodyPartType.Leg, BodyPartSymmetry.Right),
            TargetBodyPart.RightFoot => (BodyPartType.Foot, BodyPartSymmetry.Right),
            TargetBodyPart.Tail => (BodyPartType.Tail, BodyPartSymmetry.None),
            TargetBodyPart.Wings => (BodyPartType.Wings, BodyPartSymmetry.None),
            _ => (BodyPartType.Torso, BodyPartSymmetry.None)
        };
    }

    /// <summary>
    /// Returns an entity's organ category, or null if it isn't an organ.
    /// </summary>
    public ProtoId<OrganCategoryPrototype>? GetCategory(Entity<OrganComponent?> organ) => _organQuery.Resolve(organ, ref organ.Comp) ? organ.Comp.Category : null;

    /// <summary>
    /// Gets an organ in a certain slot of the body, or null if it's missing.
    /// Helper to use body cache system.
    /// </summary>
    public EntityUid? GetOrgan(EntityUid body, ProtoId<OrganCategoryPrototype> category) => _cache.GetOrgan(body, category);

    /// <summary>
    /// Gets the body of an organ, returning null if it isn't an organ or is detached.
    /// </summary>
    public EntityUid? GetBody(EntityUid organ) => _organQuery.CompOrNull(organ)?.Body;

    /// <summary>
    /// Tries to insert an organ into a body.
    /// Returns true if it is now in the body.
    /// </summary>
    public bool InsertOrgan(Entity<BodyComponent?> body, Entity<OrganComponent?> organ)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) ||
            !_organQuery.Resolve(organ, ref organ.Comp) ||
            body.Comp.Organs is not {} container)
            return false;

        if (container.Contains(organ))
            return true; // it was already inserted

        var ev = new OrganInsertAttemptEvent(body, organ);
        RaiseLocalEvent(body, ref ev);
        if (!ev.Cancelled)
            RaiseLocalEvent(organ, ref ev);
        if (ev.Cancelled)
            return false;

        return _container.Insert(organ.Owner, container);
    }

    /// <summary>
    /// Tries to remove an organ from a body.
    /// Returns true if it is no longer in the body.
    /// </summary>
    public bool RemoveOrgan(Entity<BodyComponent?> body, Entity<OrganComponent?> organ)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) ||
            !_organQuery.Resolve(organ, ref organ.Comp) ||
            body.Comp.Organs is not {} container)
            return false;

        if (!container.Contains(organ))
            return true; // it was already removed

        var ev = new OrganRemoveAttemptEvent(body, organ);
        RaiseLocalEvent(body, ref ev);
        if (!ev.Cancelled)
            RaiseLocalEvent(organ, ref ev);
        if (ev.Cancelled)
            return false;

        return _container.Remove(organ.Owner, container);
    }

    public bool ReplaceOrgan(Entity<BodyComponent?> body, Entity<OrganComponent?> organ)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) ||
            !_organQuery.Resolve(organ, ref organ.Comp) ||
            organ.Comp.Category is not {} category)
            return false;

        // if an organ is already there try to remove it
        // it will be dropped on the floor
        if (GetOrgan(body, category) is {} old && !RemoveOrgan(body, old))
            return false;

        return InsertOrgan(body, organ);
    }

    /// <summary>
    /// Tries to decapitate a mob, returning true if it succeeded.
    /// </summary>
    public bool TryDecapitate(EntityUid uid, EntityUid? user = null)
    {
        var ev = new DecapitateEvent(user);
        RaiseLocalEvent(uid, ref ev);
        return ev.Handled;
    }

    // no marking api anymore lol have to write one myself
    #region Markings

    /// <summary>
    /// Adds a marking to an organ with a given category, not allowing duplicates on the same organ.
    /// </summary>
    public bool AddOrganMarking(
        Entity<BodyComponent?> body,
        [ForbidLiteral] ProtoId<OrganCategoryPrototype> category,
        [ForbidLiteral] ProtoId<MarkingPrototype> marking,
        Color? color = null,
        bool force = false)
    {
        if (GetOrgan(body, category) is not {} organ)
            return false; // no organ found

        return AddOrganMarking(organ, marking, color, force);
    }

    /// <summary>
    /// Adds a marking to a given organ, not allowing duplicates on the same organ.
    /// </summary>
    public bool AddOrganMarking(
        Entity<VisualOrganMarkingsComponent?> organ,
        [ForbidLiteral] ProtoId<MarkingPrototype> marking,
        Color? color = null,
        bool force = false)
    {
        if (!Resolve(organ, ref organ.Comp))
            return false; // organ doesn't support markings

        var markingData = organ.Comp.MarkingData;
        var proto = ProtoMan.Index(marking);
        var layer = proto.BodyPart;
        if (!force)
        {
            if (proto.GroupWhitelist?.Contains(markingData.Group) == false)
                return false; // marking isn't whitelisted for this species/group

            if (!markingData.Layers.Contains(layer))
                return false; // this organ doesn't support the needed marking layer
        }

        var markings = organ.Comp.Markings;
        // ensure there's a list of markings for the given layer
        if (!markings.TryGetValue(layer, out var list))
        {
            list = [];
            markings[layer] = list;
        }

        // check for duplicates first
        foreach (var data in list)
        {
            if (data.MarkingId == marking)
                return false; // duplicate found, skip adding it
        }

        // good to go
        list.Add(new Marking(marking, color != null
            ? Enumerable.Repeat(color.Value, proto.Sprites.Count)
            : []));
        Dirty(organ, organ.Comp);
        return true;
    }

    #endregion

    /// <summary>
    /// Returns true if a bodypart has no child parts.
    /// </summary>
    public bool IsExtremity(EntityUid part)
    {
        foreach (var child in _relation.AllChildren(part))
        {
            if (!_internalQuery.HasComp(child))
                return false;
        }

        return true;
    }

    private bool IsDetached(EntityUid uid) => (MetaData(uid).Flags & MetaDataFlags.Detached) != 0;
}
