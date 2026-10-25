using Robust.Client.GameObjects;
using Content.Client.Items;
using Content.Shared._Trauma.ItemSwitch;

namespace Content.Client._Trauma.ItemSwitch;

public sealed partial class ItemSwitchSystem : SharedItemSwitchSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<ItemSwitchComponent>(ent => new ItemSwitchStatusControl(ent));
    }

    [SubscribeLocalEvent]
    private void OnChanged(Entity<ItemSwitchComponent> ent, ref AfterAutoHandleStateEvent args) => UpdateVisuals(ent, ent.Comp.State);

    protected override void UpdateVisuals(Entity<ItemSwitchComponent> ent, string key)
    {
        base.UpdateVisuals(ent, key);
        if (ent.Comp.States.TryGetValue(key, out var state) &&
            state.Sprite is {} sprite)
            _sprite.LayerSetSprite(ent.Owner, 0, sprite);
    }
}
