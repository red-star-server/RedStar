using System.Globalization;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared._RedStar.Paperwork;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server._RedStar.Paperwork;

/// <summary>
/// Handles rendering paperwork form templates into printable document content.
/// </summary>
public sealed partial class PaperworkSystem : EntitySystem
{
    private const int LoreYearOffset = 1000;

    private const string DocumentNamePlaceholder = "{{DOCUMENT.NAME}}";
    private const string StationNamePlaceholder = "{{STATION.NAME}}";
    private const string ShiftTimePlaceholder = "{{HOUR.MINUTE.SECOND}}";
    private const string ShiftDatePlaceholder = "{{DAY.MONTH.YEAR}}";

    [Dependency] private IResourceManager _resourceManager = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private StationSystem _station = default!;

    /// <summary>
    /// Renders a paperwork form template using the context of the provided entity.
    /// </summary>
    public string RenderForm(EntityUid source, PaperworkFormPrototype form)
    {
        using var reader = _resourceManager.ContentFileReadText(form.Template);
        var text = reader.ReadToEnd();

        var stationName = _station.GetOwningStation(source) is { } station
            ? Name(station)
            : string.Empty;

        return ApplyBaseSubstitutions(
            text,
            FormattedMessage.EscapeText(Loc.GetString(form.Name)),
            FormattedMessage.EscapeText(stationName),
            _gameTicker.RoundDuration(),
            DateTime.UtcNow);
    }

    private static string ApplyBaseSubstitutions(
        string text,
        string documentName,
        string stationName,
        TimeSpan roundDuration,
        DateTime utcNow)
    {
        return text
            .Replace(DocumentNamePlaceholder, documentName, StringComparison.Ordinal)
            .Replace(StationNamePlaceholder, stationName, StringComparison.Ordinal)
            .Replace(
                ShiftTimePlaceholder,
                roundDuration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(ShiftDatePlaceholder, FormatLoreDate(utcNow), StringComparison.Ordinal);
    }

    private static string FormatLoreDate(DateTime date)
    {
        return date
            .AddYears(LoreYearOffset)
            .ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    }
}
