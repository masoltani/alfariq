using System.Xml.Linq;
using AlFariq.Domain;

namespace AlFariq.Web.Tests;

public class SportTranslationTests
{
    [Fact]
    public void Every_sport_and_position_of_the_catalog_has_a_label()
    {
        var defined = XDocument
            .Load(Path.Combine(RepoPaths.UiRoot, "Resources", "Strings.resx"))
            .Root!
            .Elements("data")
            .Select(e => (string)e.Attribute("name")!)
            .ToHashSet();

        // Même convention que SportLabels (AlFariq.UI).
        var expected = SportCatalog.All
            .SelectMany(sport => sport.PositionCodes
                .Select(position => "Position_" + position.Replace('.', '_'))
                .Prepend("Sport_" + sport.Code));

        Assert.Empty(expected.Except(defined));
    }
}
