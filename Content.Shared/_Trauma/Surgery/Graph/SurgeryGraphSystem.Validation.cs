using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

public sealed partial class SurgeryGraphSystem
{
    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<SurgeryProcedurePrototype>() || args.WasModified<SurgeryStagePrototype>())
            ValidatePrototypes();
    }

    private void ValidatePrototypes()
    {
        if (!_proto.HasIndex(DefaultStage))
            Log.Error($"Surgery: default stage '{DefaultStage}' is not defined.");

        foreach (var procedure in _proto.EnumeratePrototypes<SurgeryProcedurePrototype>())
        {
            if (procedure.Steps.Count == 0)
                Log.Error($"Surgery procedure '{procedure.ID}' has no steps.");

            foreach (var from in procedure.From)
            {
                if (!_proto.HasIndex(from))
                    Log.Error($"Surgery procedure '{procedure.ID}' references unknown stage '{from}'.");
            }

            if (procedure.To is { } to && !_proto.HasIndex(to))
                Log.Error($"Surgery procedure '{procedure.ID}' references unknown stage '{to}'.");
        }
    }
}
