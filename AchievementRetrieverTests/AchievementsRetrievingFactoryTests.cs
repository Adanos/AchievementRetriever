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

namespace AchievementRetrieverTests;

public class AchievementsRetrievingFactoryTests
{
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void SetUp()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"SteamAchievementConfiguration:ApplicationId", "fake-key"},
            {"SteamAchievementConfiguration:AddressApi", "https://fake-url.com"},
            {"SteamAchievementConfiguration:AuthentificationKey", "fake-auth-key"},
            {"SteamAchievementConfiguration:SteamId", "1234567890"},
            {"SteamAchievementConfiguration:Language", "en"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddTransient<HttpClient>();
        services.AddTransient<IAchievementParserDispatcher, AchievementParserDispatcher>();
        services.AddTransient<AchievementSourceConfiguration>();
        services.AddTransient<GogAchievementConfiguration>();
        services.AddTransient<SteamAchievementConfiguration>();
        services.AddTransient<SteamAchievementsRetrieving>();
        services.AddTransient<GogAchievementsRetrieving>();
        services.AddSingleton<IConfiguration>(configuration);
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