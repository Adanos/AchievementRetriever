using System;
using AchievementRetriever.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AchievementRetriever.JsonParsers;

public class AchievementParserDispatcher(
    IOptions<AchievementSourceConfiguration> configOptions,
    IServiceProvider serviceProvider)
    : IAchievementParserDispatcher
{
    private readonly AchievementSourceConfiguration _config = configOptions.Value;
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public IAchievementParser GetParser()
    {
        return _config.Name switch
        {
            AchievementSource.Steam => _serviceProvider.GetRequiredService<SteamAchievementParser>(),
            AchievementSource.GoG  => _serviceProvider.GetRequiredService<GogAchievementParser>(),
            _ => throw new InvalidOperationException($"Unsupported achievement source: {_config.Name}")
        };
    }
}
