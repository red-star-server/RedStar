using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Common.Wounds;

namespace Content.Server._Trauma.PartStatus;

// collecting a body parts information together
// ik its another bs level of abstraction but i think it helps for now..
public sealed class PartStatus(
    BodyPartType partType,
    BodyPartSymmetry partSymmetry,
    string partName,
    WoundableSeverity partSeverity,
    Dictionary<string, WoundSeverity> damageSeverities,
    BoneSeverity boneSeverity,
    bool bleeding)
{
    public BodyPartType PartType = partType;

    public BodyPartSymmetry PartSymmetry = partSymmetry;

    public string PartName = partName;

    public WoundableSeverity PartSeverity = partSeverity;

    public Dictionary<string, WoundSeverity> DamageSeverities = damageSeverities;

    public BoneSeverity BoneSeverity = boneSeverity;

    public bool Bleeding = bleeding;
}
