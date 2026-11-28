using System.Xml.Linq;
using AlFariq.Domain;

namespace AlFariq.Web.Tests;

public class MatchStatusTranslationTests
{
    [Fact]
    public void Every_match_status_has_a_label()
    {
        var defined = XDocument
            .Load(Path.Combine(RepoPaths.UiRoot, "Resources", "Strings.resx"))
            .Root!
            .Elements("data")
            .Select(e => (string)e.Attribute("name")!)
            .ToHashSet();

        // Même convention que MatchLabels.StatusKey (AlFariq.UI).
        var expected = Enum.GetNames<MatchStatus>().Select(status => "Match_Status" + status);

        Assert.Empty(expected.Except(defined));
    }
}
