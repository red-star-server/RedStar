using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._RedStar.Xenobiology;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RedStar.Xenobiology.UI;

public sealed partial class SlimeScanDetailsControl : BoxContainer
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    public SlimeScanDetailsControl()
    {
        IoCManager.InjectDependencies(this);
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 6;
    }

    public void Populate(SlimeScanData? scan, string? placeholder = null)
    {
        RemoveAllChildren();
        if (scan is not { } data)
        {
            if (placeholder != null)
                AddText(placeholder);
            return;
        }

        AddText(Loc.GetString("slime-scanner-target", ("name", data.TargetName)));
        AddChild(new Separator { Margin = new Thickness(0, 2) });

        var none = Loc.GetString("slime-scanner-none");
        AddText(Loc.GetString("slime-scanner-growth", ("value", data.Growth.ToString("P0"))));
        AddChild(new ProgressBar
        {
            MinValue = 0f,
            MaxValue = 1f,
            Value = Math.Clamp(data.Growth, 0f, 1f),
            HorizontalExpand = true,
            MinHeight = 16
        });

        var fields = new[]
        {
            Loc.GetString("slime-scanner-hunger", ("value", data.Hunger?.ToString() ?? none)),
            Loc.GetString("slime-scanner-temperament", ("value", TemperamentName(data.Temperament))),
            Loc.GetString("slime-scanner-crowding", ("value", CrowdingName(data.Crowding))),
            Loc.GetString("slime-scanner-mutation-chance", ("value", data.MutationChance.ToString("P0"))),
            Loc.GetString("slime-scanner-potential-mutations", ("value", data.PotentialMutations.Length == 0
                ? none
                : string.Join(", ", data.PotentialMutations.Select(NameOf))))
        };

        foreach (var field in fields)
        {
            AddText(field);
        }

        if (data.ExtractYieldEnhanced)
            AddText(Loc.GetString("slime-scanner-extract-yield-enhanced"));
    }

    private void AddText(string text)
    {
        var label = new RichTextLabel { HorizontalExpand = true };
        label.SetMessage(text);
        AddChild(label);
    }

    private string NameOf(EntProtoId id) => _prototypes.TryIndex(id, out var prototype) ? prototype.Name : id.Id;

    private static string TemperamentName(SlimeTemperament temperament) => Loc.GetString(temperament switch
    {
        SlimeTemperament.Calm => "slime-scanner-temperament-calm",
        SlimeTemperament.Restless => "slime-scanner-temperament-restless",
        SlimeTemperament.Aggressive => "slime-scanner-temperament-aggressive",
        _ => "slime-scanner-none"
    });

    private static string CrowdingName(SlimeCrowding crowding) => Loc.GetString(crowding switch
    {
        SlimeCrowding.Low => "slime-scanner-crowding-low",
        SlimeCrowding.Crowded => "slime-scanner-crowding-crowded",
        SlimeCrowding.Severe => "slime-scanner-crowding-severe",
        _ => "slime-scanner-none"
    });
}
