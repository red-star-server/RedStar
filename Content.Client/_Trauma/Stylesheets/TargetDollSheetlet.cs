using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Trauma.Stylesheets;

[CommonSheetlet]
public sealed class TargetDollSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<TextureButton>().Class("TargetDollButtonHead")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/head_hover.png")),

            E<TextureButton>().Class("TargetDollButtonChest")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/torso_hover.png")),

            E<TextureButton>().Class("TargetDollButtonGroin")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/groin_hover.png")),

            E<TextureButton>().Class("TargetDollButtonLeftArm")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/leftarm_hover.png")),

            E<TextureButton>().Class("TargetDollButtonLeftHand")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/lefthand_hover.png")),

            E<TextureButton>().Class("TargetDollButtonRightArm")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/rightarm_hover.png")),

            E<TextureButton>().Class("TargetDollButtonRightHand")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/righthand_hover.png")),

            E<TextureButton>().Class("TargetDollButtonLeftLeg")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/leftleg_hover.png")),

            E<TextureButton>().Class("TargetDollButtonLeftFoot")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/leftfoot_hover.png")),

            E<TextureButton>().Class("TargetDollButtonRightLeg")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/rightleg_hover.png")),

            E<TextureButton>().Class("TargetDollButtonRightFoot")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/rightfoot_hover.png")),

            E<TextureButton>().Class("TargetDollButtonEyes")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/eyes_hover.png")),

            E<TextureButton>().Class("TargetDollButtonMouth")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/_RedStar/Medical/Interface/Targeting/Doll/mouth_hover.png")),
        ];
    }
}
