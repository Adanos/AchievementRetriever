using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using AchievementRetriever.JsonParsers;

namespace AchievementRetrieverTests.JsonParsers;

public class GogAchievementParserTests
{
    [Test]
    public void CanParse_WhenValidJsonArrayProvided_ReturnsTrue()
    {
        var json = "[{\"achievement\": {\"name\": \"Achievement 1\", \"description\": \"Description 1\"}}]";
        var root = JsonDocument.Parse(json).RootElement;

        var parser = new GogAchievementParser();
        var result = parser.CanParse(root);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanParse_WhenInvalidJsonArrayProvided_ReturnsFalse()
    {
        var json = "[{\"invalidProperty\": {\"name\": \"Achievement 1\", \"description\": \"Description 1\"}}]";
        var root = JsonDocument.Parse(json).RootElement;

        var parser = new GogAchievementParser();
        var result = parser.CanParse(root);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Parse_WhenValidJsonArrayProvided_ReturnsAchievements()
    {
        var json = "window.profilesData.achievements=[{\"achievement\": {\"name\": \"Achievement 1\", \"description\": \"Description 1\"}, \"stats\":{\"11\":{\"isUnlocked\": true}}}];";

        var parser = new GogAchievementParser();
        var result = parser.Parse(json);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Achievement 1"));
        Assert.That(result[0].Description, Is.EqualTo("Description 1"));
        Assert.That(result[0].IsUnlocked, Is.True);
    }

    [Test]
    public void Parse_WhenStatsAreMissing_ReturnsAchievementsWithNullUnlocked()
    {
        var json = "window.profilesData.achievements=[{\"achievement\":{\"name\":\"Achievement 1\",\"description\":\"Description 1\",\"isUnlocked\":false}}];";

        var parser = new GogAchievementParser();
        var result = parser.Parse(json);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Achievement 1"));
        Assert.That(result[0].Description, Is.EqualTo("Description 1"));
        Assert.That(result[0].IsUnlocked, Is.Null);
    }

    [Test]
    public void Parse_WhenAchievementPropertyIsMissing_ThrowsKeyNotFoundException()
    {
        var json = "window.profilesData.achievements=[{\"invalidProperty\":{\"name\":\"name\",\"description\":\"desc\",\"isUnlocked\":false}}];";

        var parser = new GogAchievementParser();

        Assert.Throws<KeyNotFoundException>(() => parser.Parse(json));
    }
    
    [Test]
    public void Parse_WhenAchievementPropertyIsMissing_ThrowsNotFoundException()
    {
        var json = "[{\"achievement\": {\"name\": \"Achievement 1\", \"description\": \"Description 1\"}}]";

        var parser = new GogAchievementParser();

        Assert.Throws<Exception>(() => parser.Parse(json));
    }
    
    [Test]
    public void ParseJsonFromHtmlTests_ParseFileWithTwoDescription_ReturnObject()
    {
        var path = Path.Combine("HtmlTestCase", "GogAchievementsTestCase.txt");
        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fileStream);
        string jsonFromHtml = reader.ReadToEnd();

        var parser = new GogAchievementParser();
        var result = parser.Parse(jsonFromHtml).ToList();
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.First().Name, Is.EqualTo("Doge Coins"));
        Assert.That(result.First().Description, Is.EqualTo("Starting as Venice, become the best."));
        Assert.That(result.First().IsUnlocked, Is.True);
        Assert.That(result.Last().Name, Is.EqualTo("New achievement"));
        Assert.That(result.Last().Description, Is.EqualTo("Starting as any Mayan country, conquer the world"));
        Assert.That(result.Last().IsUnlocked, Is.False);
    }
}