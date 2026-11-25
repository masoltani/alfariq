using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AlFariq.Web.Tests;

public class LocalizationTests
{
    private static readonly Regex KeyPattern = new(@"L\[""([A-Za-z0-9_]+)""\s*[\],]", RegexOptions.Compiled);

    [Fact]
    public void Every_key_used_in_razor_files_exists_in_the_resource_file()
    {
        var defined = XDocument
            .Load(Path.Combine(RepoPaths.UiRoot, "Resources", "Strings.resx"))
            .Root!
            .Elements("data")
            .Select(e => (string)e.Attribute("name")!)
            .ToHashSet();

        var used = Directory
            .EnumerateFiles(RepoPaths.UiRoot, "*.razor", SearchOption.AllDirectories)
            .SelectMany(file => KeyPattern.Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value))
            .ToHashSet();

        Assert.Empty(used.Except(defined));
    }
}
