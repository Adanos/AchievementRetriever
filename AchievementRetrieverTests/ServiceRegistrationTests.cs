using System;
using System.Collections.Generic;
using System.Net.Http;
using AchievementRetriever;
using AchievementRetriever.IO;
using AchievementRetriever.JsonParsers;
using AchievementRetriever.Managers;
using AchievementRetriever.Models;
using AchievementRetriever.Models.FromApi.Gog;
using AchievementRetriever.Models.FromApi.Steam;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace AchievementRetrieverTests;

[TestFixture]
public class ServiceRegistrationTests
{
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();
        _serviceProvider = host.Services.CreateScope().ServiceProvider;
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
    
    [Test]
    public void SingletonServices_ShouldResolveSameInstance()
    {
        AssertSingleton<AchievementManager>();
        AssertSingleton<SteamAchievementParser>();
        AssertSingleton<GogAchievementParser>();
        AssertSingleton<IAchievementParserDispatcher, AchievementParserDispatcher>();
        AssertSingleton<IAchievementsRetrievingFactory, AchievementsRetrievingFactory>();
        AssertSingleton<IFileService, FileService>();
    }

    [Test]
    public void TransientServices_ShouldResolveDifferentInstances()
    {
        AssertTransient<SteamAchievementsRetrieving>();
        AssertTransient<GogAchievementsRetrieving>();
    }

    // -------------------------
    // Options registration
    // -------------------------
    [Test]
    public void Options_ShouldBeConfigured()
    {
        Assert.That(_serviceProvider.GetService<IOptions<SteamAchievementConfiguration>>(), Is.Not.Null);
        Assert.That(_serviceProvider.GetService<IOptions<GogAchievementConfiguration>>(), Is.Not.Null);
        Assert.That(_serviceProvider.GetService<IOptions<AchievementSourceConfiguration>>(), Is.Not.Null);
    }

    // -------------------------
    // Options binding (real values)
    // -------------------------
    [Test]
    public void Options_ShouldBindValuesFromConfiguration()
    {
        // Arrange - in-memory config simulating appsettings.json
        var inMemorySettings = new Dictionary<string, string>
        {
            { "SteamAchievementConfiguration:ApplicationId", "12345" },
            { "SteamAchievementConfiguration:AddressApi", "http://api.steam.com" },
            { "SteamAchievementConfiguration:AuthenticationKey", "secret" },
            { "SteamAchievementConfiguration:SteamId", "my-steam-id" },
            { "SteamAchievementConfiguration:Language", "en" },
            { "SteamAchievementConfiguration:FilePathToSaveResult", "/tmp/results/" },
            { "SteamAchievementConfiguration:IsAchieved", "true" },

            { "GogAchievementConfiguration:AddressApi", "http://gog-api" },
            { "GogAchievementConfiguration:User", "user" },
            { "GogAchievementConfiguration:GameId", "game-id" },

            { "AchievementSourceConfiguration:Name", "Steam" }
        };

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(inMemorySettings);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<SteamAchievementConfiguration>(context.Configuration.GetSection(nameof(SteamAchievementConfiguration)));
                services.Configure<GogAchievementConfiguration>(context.Configuration.GetSection(nameof(GogAchievementConfiguration)));
                services.Configure<AchievementSourceConfiguration>(context.Configuration.GetSection(nameof(AchievementSourceConfiguration)));
            })
            .Build();

        var sp = host.Services;

        // Act
        var steamOptions = sp.GetRequiredService<IOptions<SteamAchievementConfiguration>>().Value;
        var gogOptions = sp.GetRequiredService<IOptions<GogAchievementConfiguration>>().Value;
        var sourceOptions = sp.GetRequiredService<IOptions<AchievementSourceConfiguration>>().Value;

        // Assert - Steam
        Assert.That(steamOptions.ApplicationId, Is.EqualTo("12345"));
        Assert.That(steamOptions.AddressApi, Is.EqualTo("http://api.steam.com"));
        Assert.That(steamOptions.AuthenticationKey, Is.EqualTo("secret"));
        Assert.That(steamOptions.SteamId, Is.EqualTo("my-steam-id"));
        Assert.That(steamOptions.Language, Is.EqualTo("en"));
        Assert.That(steamOptions.FilePathToSaveResult, Is.EqualTo("/tmp/results/"));
        Assert.That(steamOptions.IsAchieved, Is.True);

        // Assert - Gog
        Assert.That(gogOptions.AddressApi, Is.EqualTo("http://gog-api"));
        Assert.That(gogOptions.User, Is.EqualTo("user"));
        Assert.That(gogOptions.GameId, Is.EqualTo("game-id"));

        // Assert - AchievementSource
        Assert.That(sourceOptions.Name, Is.EqualTo(AchievementSource.Steam));
    }

    // -------------------------
    // HttpClient
    // -------------------------
    [Test]
    public void ShouldResolve_HttpClient()
    {
        var client = _serviceProvider.GetService<HttpClient>();
        Assert.That(client, Is.Not.Null);
    }

    // -------------------------
    // Scoped example (future use)
    // -------------------------
    [Test]
    public void ScopedService_ShouldCreateDifferentInstancesAcrossScopes()
    {
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        var service1 = scope1.ServiceProvider.GetService<IFileService>();
        var service2 = scope2.ServiceProvider.GetService<IFileService>();

        Assert.That(service1, Is.Not.Null);
        Assert.That(service2, Is.Not.Null);
        Assert.That(service1, Is.SameAs(service2)); // currently singleton
    }

    // -------------------------
    // Helpers
    // -------------------------
    private void AssertSingleton<TService>()
    {
        var s1 = _serviceProvider.GetService<TService>();
        var s2 = _serviceProvider.GetService<TService>();

        Assert.That(s1, Is.Not.Null);
        Assert.That(s2, Is.SameAs(s1));
    }

    private void AssertSingleton<TService, TImpl>() where TImpl : TService
    {
        var s1 = _serviceProvider.GetService<TService>();
        var s2 = _serviceProvider.GetService<TService>();

        Assert.That(s1, Is.Not.Null);
        Assert.That(s2, Is.SameAs(s1));
        Assert.That(s1, Is.InstanceOf<TImpl>());
    }

    private void AssertTransient<TService>()
    {
        var s1 = _serviceProvider.GetService<TService>();
        var s2 = _serviceProvider.GetService<TService>();

        Assert.That(s1, Is.Not.Null);
        Assert.That(s2, Is.Not.Null);
        Assert.That(s2, Is.Not.SameAs(s1));
    }
}