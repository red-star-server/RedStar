using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Common.Damage;

[Serializable, NetSerializable]
public enum SplitDamageBehavior : byte
{
    None,
    Split,
    SplitEnsureAllOrganic,
    SplitEnsureAllDamaged,
    SplitEnsureAllDamagedAndOrganic,
    SplitEnsureAll
}
