using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

public sealed partial class SurgeryGraphSystem
{
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<SurgeryProcedurePrototype>() || args.WasModified<SurgeryStagePrototype>())
            ValidatePrototypes();
    }

    /// <summary>
    /// Data lint for the stage/procedure graph. The prefix rule keeps procedure selection
    /// deterministic once tool-driven (world-targeting) dispatch lands: a procedure whose
    /// steps are a strict prefix of another's, with no distinguishing conditions or organ,
    /// would always complete first and make the longer one unreachable.
    /// </summary>
    private void ValidatePrototypes()
    {
        if (!_proto.HasIndex(DefaultStage))
            Log.Error($"Surgery: default stage '{DefaultStage}' is not defined.");

        var procedures = new List<SurgeryProcedurePrototype>();
        foreach (var procedure in _proto.EnumeratePrototypes<SurgeryProcedurePrototype>())
        {
            procedures.Add(procedure);

            if (procedure.Steps.Count == 0)
                Log.Error($"Surgery procedure '{procedure.ID}' has no steps.");

            foreach (var from in procedure.From)
            {
                if (!_proto.HasIndex(from))
                    Log.Error($"Surgery procedure '{procedure.ID}' references unknown stage '{from}'.");
            }

            if (!_proto.HasIndex(procedure.To))
                Log.Error($"Surgery procedure '{procedure.ID}' references unknown stage '{procedure.To}'.");
        }

        for (var i = 0; i < procedures.Count; i++)
        {
            for (var j = i + 1; j < procedures.Count; j++)
            {
                var a = procedures[i];
                var b = procedures[j];

                if (a.OrganMode != b.OrganMode || !SharesStage(a, b) || !SameConditions(a, b))
                    continue;

                var prefix = StrictStepPrefix(a, b) ? a : StrictStepPrefix(b, a) ? b : null;
                if (prefix is not null)
                {
                    var other = prefix == a ? b : a;
                    Log.Error($"Surgery procedure '{prefix.ID}' is a strict step prefix of '{other.ID}' with identical conditions; the longer procedure is unreachable.");
                }
            }
        }
    }

    private static bool SharesStage(SurgeryProcedurePrototype a, SurgeryProcedurePrototype b)
    {
        foreach (var from in a.From)
        {
            if (b.From.Contains(from))
                return true;
        }

        return false;
    }

    // Approximate condition-set equality: same condition types with the same inversion.
    // Semantic mutual exclusion cannot be checked statically, so identical sets are the conservative trigger.
    private static bool SameConditions(SurgeryProcedurePrototype a, SurgeryProcedurePrototype b)
    {
        if (a.Conditions.Count != b.Conditions.Count)
            return false;

        var types = new List<(string Type, bool Inverted)>();
        foreach (var condition in a.Conditions)
        {
            types.Add((condition.GetType().Name, condition.Inverted));
        }

        foreach (var condition in b.Conditions)
        {
            if (!types.Remove((condition.GetType().Name, condition.Inverted)))
                return false;
        }

        return types.Count == 0;
    }

    private static bool StrictStepPrefix(SurgeryProcedurePrototype shorter, SurgeryProcedurePrototype longer)
    {
        if (shorter.Steps.Count >= longer.Steps.Count)
            return false;

        for (var i = 0; i < shorter.Steps.Count; i++)
        {
            if (!SameStepSignature(shorter.Steps[i], longer.Steps[i]))
                return false;
        }

        return true;
    }

    private static bool SameStepSignature(SurgeryStepEntry a, SurgeryStepEntry b)
    {
        return a.Quality == b.Quality
            && a.Organ == b.Organ
            && a.ProcedureOrgan == b.ProcedureOrgan
            && a.AnyItem == b.AnyItem
            && a.Duration == b.Duration;
    }
}