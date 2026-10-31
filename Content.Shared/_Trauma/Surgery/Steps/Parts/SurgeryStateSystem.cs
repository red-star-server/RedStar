namespace Content.Shared._Trauma.Surgery.Steps.Parts;

public sealed class SurgeryStateSystem : EntitySystem
{
    public void SetStep(EntityUid uid, SurgeryStepFlags step, bool completed = true)
    {
        var state = EnsureComp<SurgeryStateComponent>(uid);

        if (completed)
            state.CompletedSteps |= step;
        else
            state.CompletedSteps &= ~step;

        Dirty(uid, state);
    }

    public void ClearStep(EntityUid uid, SurgeryStepFlags step)
    {
        SetStep(uid, step, completed: false);
    }

    public bool HasStep(EntityUid uid, SurgeryStepFlags step)
    {
        return TryComp<SurgeryStateComponent>(uid, out var state) &&
               (state.CompletedSteps & step) == step;
    }

    public void ClearAllSteps(EntityUid uid)
    {
        if (!TryComp<SurgeryStateComponent>(uid, out var state))
            return;

        state.CompletedSteps = SurgeryStepFlags.None;
        Dirty(uid, state);
    }
}
