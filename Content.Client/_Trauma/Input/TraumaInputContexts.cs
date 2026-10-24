using Content.Shared._Trauma.Common.Input;
using Robust.Shared.Input;

namespace Content.Client._Trauma.Input;

public static class TraumaInputContexts
{
    public static void SetupContexts(IInputContextContainer contexts)
    {
        var human = contexts.GetContext("human");
        human.AddFunction(TraumaKeyFunctions.TargetingMod);
        human.AddFunction(TraumaKeyFunctions.TargetHead);
        human.AddFunction(TraumaKeyFunctions.TargetChest);
        human.AddFunction(TraumaKeyFunctions.TargetGroin);
        human.AddFunction(TraumaKeyFunctions.TargetLeftArm);
        human.AddFunction(TraumaKeyFunctions.TargetLeftHand);
        human.AddFunction(TraumaKeyFunctions.TargetRightArm);
        human.AddFunction(TraumaKeyFunctions.TargetRightHand);
        human.AddFunction(TraumaKeyFunctions.TargetLeftLeg);
        human.AddFunction(TraumaKeyFunctions.TargetLeftFoot);
        human.AddFunction(TraumaKeyFunctions.TargetRightLeg);
        human.AddFunction(TraumaKeyFunctions.TargetRightFoot);
    }
}
