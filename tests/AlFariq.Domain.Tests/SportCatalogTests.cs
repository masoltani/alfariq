using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

public class SportCatalogTests
{
    [Fact]
    public void Football_is_the_default_sport()
    {
        Assert.Equal("football", SportCatalog.Default.Code);
    }

    [Fact]
    public void Sport_and_position_codes_are_unique_and_positions_are_prefixed_by_their_sport()
    {
        var sports = SportCatalog.All;
        var positions = sports.SelectMany(s => s.PositionCodes).ToList();

        Assert.Equal(sports.Count, sports.Select(s => s.Code).Distinct().Count());
        Assert.Equal(positions.Count, positions.Distinct().Count());
        Assert.All(sports, sport => Assert.All(
            sport.PositionCodes,
            position => Assert.StartsWith(sport.Code + ".", position)));
    }

    [Fact]
    public void Every_sport_proposes_valid_formats()
    {
        Assert.All(SportCatalog.All, sport =>
        {
            Assert.NotEmpty(sport.Formats);
            Assert.All(sport.Formats, format => Assert.InRange(format, Match.MinPlayersPerSide, Match.MaxPlayersPerSide));
            Assert.Equal(sport.Formats[0], sport.DefaultPlayersPerSide);
        });
        Assert.Equal(new[] { 5, 7, 11 }, SportCatalog.Default.Formats);
    }

    [Fact]
    public void Find_returns_null_for_an_unknown_code()
    {
        Assert.Null(SportCatalog.Find("inconnu"));
        Assert.Null(SportCatalog.Find(null));
    }
}
