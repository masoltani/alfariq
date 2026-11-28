namespace AlFariq.Web.Tests;

public class HomePageTests
{
    private static string Page(string name) =>
        File.ReadAllText(Path.Combine(RepoPaths.UiRoot, "Pages", name));

    [Theory]
    [InlineData("matchs/nouveau", "MatchEdit.razor")]
    [InlineData("joueurs", "Players.razor")]
    [InlineData("profil", "MyProfile.razor")]
    [InlineData("statistiques", "StatsOverview.razor")]
    public void Home_links_to_a_page_that_exists(string link, string pageFile)
    {
        Assert.Contains($"href=\"{link}\"", Page("Home.razor"));
        Assert.Contains($"@page \"/{link}\"", Page(pageFile));
    }
}
