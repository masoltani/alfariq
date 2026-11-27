namespace AlFariq.Application;

/// <summary>
/// Le texte à partager d'une composition (US-011), mis en forme pour WhatsApp : « *…* » y affiche du gras.
/// Il reste lisible tel quel dans une autre application.
/// </summary>
public static class LineupText
{
    public static string Compose(LineupCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var lines = new List<string>
        {
            $"{SportSymbol(card.SportCode)} *{Bold(card.Title)}*",
            $"🕒 {card.Details}",
        };
        if (!string.IsNullOrWhiteSpace(card.Location))
        {
            lines.Add($"📍 {card.Location}");
        }

        foreach (var team in card.Teams)
        {
            var formation = team.Formation is null ? "" : $" ({team.Formation})";
            lines.Add("");
            lines.Add($"{ColorSymbol(team.ColorCode)} *{Bold(team.Name)}*{formation}");

            if (team.Players.Count == 0 && team.Others.Count == 0)
            {
                lines.Add(card.EmptyTeam);
            }

            lines.AddRange(team.Players.Select(player => $"• {Describe(player)}"));
            if (team.OthersTitle is not null)
            {
                lines.Add($"{team.OthersTitle} {string.Join(", ", team.Others.Select(Describe))}");
            }
        }

        if (card.Unassigned.Count > 0)
        {
            lines.Add("");
            if (card.Teams.Count == 0)
            {
                lines.Add($"👥 *{Bold(card.UnassignedTitle)}*");
                lines.AddRange(card.Unassigned.Select(name => $"• {name}"));
            }
            else
            {
                lines.Add($"{card.UnassignedTitle} {string.Join(", ", card.Unassigned)}");
            }
        }

        return string.Join('\n', lines);
    }

    private static string Describe(CardPlayer player) =>
        player.Position is null ? player.Name : $"{player.Name} ({player.Position})";

    // Un astérisque saisi dans un nom couperait le gras.
    private static string Bold(string text) => text.Replace("*", "", StringComparison.Ordinal).Trim();

    private static string SportSymbol(string sportCode) => sportCode switch
    {
        "football" or "futsal" => "⚽",
        "basketball" => "🏀",
        "handball" => "🤾",
        "volleyball" => "🏐",
        _ => "🏆",
    };

    private static string ColorSymbol(string colorCode) => colorCode switch
    {
        "blue" => "🔵",
        "red" => "🔴",
        "green" => "🟢",
        "yellow" => "🟡",
        "orange" => "🟠",
        "purple" => "🟣",
        "white" => "⚪",
        "black" => "⚫",
        _ => "🔘",
    };
}
