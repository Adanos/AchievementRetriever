using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AchievementRetriever;
using AchievementRetriever.JsonParsers;
using AchievementRetriever.Models;
using AchievementRetriever.Models.FromApi.Gog;
using AchievementRetriever.Models.FromApi.Steam;
using Microsoft.Extensions.Options;
using Moq;

namespace AchievementRetrieverTests;

public class AchievementsRetrievingFactoryTests
{
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void SetUp()
    {
        var steamConfig = new SteamAchievementConfiguration
        {
            AuthenticationKey = "fake-key",
            AddressApi = "value"
        };
        
        var gogConfig = new GogAchievementConfiguration()
        {
            GameId = "fake-key",
            AddressApi = "value"
        };

        var parserDispatcherMock = new Mock<IAchievementParserDispatcher>();
        parserDispatcherMock.Setup(p => p.GetParser())
            .Returns(Mock.Of<IAchievementParser>());
        
        var steamOptionsMock = new Mock<IOptions<SteamAchievementConfiguration>>();
        steamOptionsMock.Setup(o => o.Value).Returns(steamConfig);
        var gogOptionsMock = new Mock<IOptions<GogAchievementConfiguration>>();
        gogOptionsMock.Setup(o => o.Value).Returns(gogConfig);

        var services = new ServiceCollection();
        services.AddTransient<HttpClient>();
        services.AddTransient<IAchievementParserDispatcher, AchievementParserDispatcher>();
        services.AddTransient<AchievementSourceConfiguration>();
        services.AddTransient<GogAchievementConfiguration>();
        services.AddTransient<SteamAchievementConfiguration>();
        services.AddSingleton(_ => new SteamAchievementsRetrieving(
            new HttpClient(),
            parserDispatcherMock.Object,
            steamOptionsMock.Object));
        services.AddSingleton(_ => new GogAchievementsRetrieving(
            new HttpClient(),
            parserDispatcherMock.Object,
            gogOptionsMock.Object));

        _serviceProvider = services.BuildServiceProvider();
    }
    
    [TestCase(AchievementSource.Steam, typeof(SteamAchievementsRetrieving))]
    [TestCase(AchievementSource.GoG, typeof(GogAchievementsRetrieving))]
    public void GetAchievementsRetrieving_ReturnsCorrectType_WhenSourceIsValid(AchievementSource source, Type expectedType)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "AchievementSourceConfiguration:Name", source.ToString() }
            })
            .Build();
        
        var factory = new AchievementsRetrievingFactory(config, _serviceProvider);
        
        var result = factory.GetAchievementsRetrieving();
        
        Assert.That(result, Is.InstanceOf(expectedType));
    }

    [Test]
    public void GetAchievementsRetrieving_ThrowsNotImplementedException_WhenSourceIsUnknown()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "AchievementSourceConfiguration:Name", ((AchievementSource)222).ToString() }
            })
            .Build();

        var factory = new AchievementsRetrievingFactory(config, _serviceProvider);

        Assert.Throws<NotImplementedException>(() => factory.GetAchievementsRetrieving());
    }
}