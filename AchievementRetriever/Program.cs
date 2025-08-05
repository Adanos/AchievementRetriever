﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Threading.Tasks;
using AchievementRetriever.JsonParsers;
using AchievementRetriever.Managers;
using AchievementRetriever.Models;
using AchievementRetriever.Models.FromApi.Gog;
using AchievementRetriever.Models.FromApi.Steam;

namespace AchievementRetriever
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            
            var achievementManager = services.GetRequiredService<AchievementManager>();
            
            await achievementManager.CreateAchievements();
            
            if (achievementManager.Achievements != null)
            {
                AchievementMatrixCreator achievementMatrixCreator = new AchievementMatrixCreator(achievementManager.Achievements);
                achievementMatrixCreator.CreateMatrix();
                var updatedAchievements = achievementManager.Achievements;
                achievementManager.SaveAchievementsToFile("updated", updatedAchievements);
            }     
        }
        
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<HttpClient>();
                    services.AddSingleton<AchievementManager>();
                    services.AddSingleton<SteamAchievementParser>();
                    services.AddSingleton<GogAchievementParser>();
                    services.AddSingleton<AchievementSourceConfiguration>();  
                    services.AddSingleton<SteamAchievementConfiguration>(); 
                    services.AddSingleton<GogAchievementConfiguration>();   
                    services.AddSingleton<IAchievementParserDispatcher, AchievementParserDispatcher>();
                });
    }
}
