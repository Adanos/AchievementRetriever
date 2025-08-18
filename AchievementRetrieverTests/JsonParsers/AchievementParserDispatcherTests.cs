using System;
using System.Text.Json;
using NUnit.Framework;
using AchievementRetriever.JsonParsers;
using AchievementRetriever.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AchievementRetrieverTests.JsonParsers;

public class AchievementParserDispatcherTests
{
    private ServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        services.AddSingleton<SteamAchievementParser>();
        services.AddSingleton<GogAchievementParser>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private AchievementParserDispatcher CreateDispatcher(AchievementSource source)
    {
        var options = Options.Create(new AchievementSourceConfiguration { Name = source });
        return new AchievementParserDispatcher(options, _serviceProvider);
    }
    
    [TestCase(AchievementSource.Steam)]
    [TestCase(AchievementSource.GoG)]
    public void UseAchievementParser_WhenValidSourceProvided_ShouldCreateWithoutErrors(AchievementSource source)
    {
        var dispatcher = CreateDispatcher(source);

        Assert.That(dispatcher, Is.Not.Null);
    }

    [Test]
    public void UseAchievementParser_WhenInvalidSourceProvided_ThrowsInvalidOperationException()
    {
        var dispatcher = CreateDispatcher((AchievementSource)999);
        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.GetParser());
        Assert.That(ex.Message, Does.Contain("Unsupported achievement source: 999"));
    }

    [Test]
    public void UseAchievementParser_WhenInvalidJsonProvided_ThrowsJsonException()
    {
        var dispatcher = CreateDispatcher(AchievementSource.Steam);
        var invalidJson = "{Invalid JSON}";

        var ex = Assert.Catch(() => dispatcher.GetParser().Parse(invalidJson));
        Assert.That(ex, Is.InstanceOf<JsonException>());
    }
}