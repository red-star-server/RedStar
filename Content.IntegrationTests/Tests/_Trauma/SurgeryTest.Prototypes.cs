using System.Collections.Generic;
using System.Linq;
using Content.Shared._Trauma.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed partial class SurgeryTest
{
    [Test]
    public async Task SurgeryGraphPrototypesAreConsistent()
    {
        await Server.WaitAssertion(() =>
        {
            var proto = Server.ProtoMan;
            var procedures = proto.EnumeratePrototypes<SurgeryProcedurePrototype>().ToList();
            var reachable = new HashSet<ProtoId<SurgeryStagePrototype>> { SurgeryGraphSystem.DefaultStage };

            Assert.That(proto.HasIndex(SurgeryGraphSystem.DefaultStage), Is.True, "Default surgery stage is missing");

            foreach (var procedure in procedures)
            {
                Assert.That(procedure.From, Is.Not.Empty, $"{procedure.ID} has no source stages");
                Assert.That(procedure.Steps, Is.Not.Empty, $"{procedure.ID} has no steps");

                foreach (var from in procedure.From)
                {
                    Assert.That(proto.HasIndex(from), Is.True, $"{procedure.ID} references unknown stage {from}");
                }

                if (procedure.To is { } to)
                {
                    Assert.That(proto.HasIndex(to), Is.True, $"{procedure.ID} references unknown stage {to}");
                }

                foreach (var category in procedure.Categories)
                {
                    Assert.That(proto.HasIndex(category), Is.True, $"{procedure.ID} references unknown organ category {category}");
                }

                for (var i = 0; i < procedure.Steps.Count; i++)
                {
                    var step = procedure.Steps[i];
                    var name = $"{procedure.ID}[{i}]";

                    Assert.That(step.AnyItem || step.ProcedureOrgan || step.Organ != null || step.Quality != null,
                        Is.True, $"{name} declares no tool matcher");
                    Assert.That(!step.ProcedureOrgan || procedure.OrganMode != SurgeryOrganMode.None,
                        Is.True, $"{name} matches the procedure organ but the procedure has no organ mode");
                    Assert.That(!step.Repeatable || step.RepeatWhile != null,
                        Is.True, $"{name} is repeatable without a repeat condition");

                    if (step.Organ is { } organ)
                        Assert.That(proto.HasIndex(organ), Is.True, $"{name} references unknown organ category {organ}");
                }
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var procedure in procedures)
                {
                    if (!procedure.From.Any(reachable.Contains) ||
                        procedure.To is not { } to)
                        continue;

                    if (reachable.Add(to))
                        changed = true;
                }
            }

            foreach (var stage in proto.EnumeratePrototypes<SurgeryStagePrototype>())
            {
                var id = new ProtoId<SurgeryStagePrototype>(stage.ID);
                Assert.That(reachable, Does.Contain(id), $"Stage {id} is not reachable");
                Assert.That(procedures.Any(p => p.From.Contains(id)), Is.True, $"Stage {id} has no procedures leaving it");
            }
        });
    }
}
