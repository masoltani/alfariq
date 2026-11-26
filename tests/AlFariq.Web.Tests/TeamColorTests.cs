using System.Xml.Linq;
using AlFariq.Domain;

namespace AlFariq.Web.Tests;

public class TeamColorTests
{
    [Fact]
    public void Every_team_colour_has_a_label_and_a_style()
    {
        var defined = XDocument
            .Load(Path.Combine(RepoPaths.UiRoot, "Resources", "Strings.resx"))
            .Root!
            .Elements("data")
            .Select(e => (string)e.Attribute("name")!)
            .ToList();
        var css = File.ReadAllText(Path.Combine(RepoPaths.UiRoot, "wwwroot", "css", "app.css"));

        Assert.All(TeamColors.All, color =>
        {
            // Même convention que TeamLabels (AlFariq.UI).
            Assert.Contains("Color_" + color, defined);
            Assert.Contains($".team-color--{color} {{", css);
        });
        Assert.All(Enum.GetValues<TeamSide>(), side => Assert.Contains(TeamColors.DefaultFor(side), TeamColors.All));
        Assert.NotEqual(TeamColors.DefaultFor(TeamSide.A), TeamColors.DefaultFor(TeamSide.B));
    }
}
