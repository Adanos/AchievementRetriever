using AchievementRetriever;
using AchievementRetriever.Models;
using NUnit.Framework;

namespace AchievementRetrieverTests;

[TestFixture]
public class FilenameCreatorTests
{
    private const string FilePath = "/test/path/";

    [TestCase(AchievementSource.Steam, null, "HalfLife", "Steam Achievements HalfLife.csv")]
    [TestCase(AchievementSource.GoG, true, "Witcher3", "GoG UnlockedAchievements Witcher3.csv")]
    [TestCase(AchievementSource.Steam, false, "Portal2", "Steam LockedAchievements Portal2.csv")]
    public void CreateFilename_IsAchievedFlagNull_ReturnsAchievements(AchievementSource source, bool? isAchievedFlag, string gameName, string expected)
    {
        var creator = new FilenameCreator(source, isAchievedFlag, FilePath);

        var result = creator.CreateFilename(gameName);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CreateFilename_CustomExtension_ReturnsCustomExtension()
    {
        var creator = new FilenameCreator(AchievementSource.Steam, null, FilePath);

        var result = creator.CreateFilename("HalfLife", ".json");

        Assert.That(result, Is.EqualTo("Steam Achievements HalfLife.json"));
    }

    [Test]
    public void CreateFullPath_ReturnsExpectedPathWithFilename()
    {
        var creator = new FilenameCreator(AchievementSource.Steam, null, FilePath);

        var result = creator.CreateFullPath("HalfLife");

        Assert.That(result, Is.EqualTo("/test/path/Steam Achievements HalfLife.csv"));
    }
    
    [TestCase(null, "Steam Achievements HalfLife.csv")]
    [TestCase("", "Steam Achievements HalfLife.csv")]
    [TestCase("_updated", "Steam Achievements HalfLife_updated.csv")]
    [TestCase("_mySuffix", "Steam Achievements HalfLife_mySuffix.csv")]
    public void CreateFilename_WithSuffix_AppendsSuffixBeforeExtension(string suffix, string expectedResult)
    {
        var creator = new FilenameCreator(AchievementSource.Steam, null, FilePath);
        
        var filename = creator.CreateFilename("HalfLife", ".csv", suffix);

        Assert.That(filename, Is.EqualTo(expectedResult));
    }
}