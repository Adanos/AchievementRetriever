using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Threading.Tasks;
using AchievementRetriever.IO;
using AchievementRetriever.JsonParsers;
using AchievementRetriever.Managers;
using AchievementRetriever.Models;
using AchievementRetriever.Models.FromApi.Gog;
using AchievementRetriever.Models.FromApi.Steam;

namespace AchievementRetriever
{
    public class Program
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
                achievementManager.SaveAchievementsToFile(suffix: Constants.Updated);
            }     
        }
        
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<HttpClient>();
                    services.AddSingleton<AchievementManager>();
                    services.AddSingleton<SteamAchievementParser>();
                    services.AddSingleton<GogAchievementParser>();

                    services.Configure<SteamAchievementConfiguration>(
                        context.Configuration.GetSection(nameof(SteamAchievementConfiguration))
                    );
                    services.Configure<GogAchievementConfiguration>(
                        context.Configuration.GetSection(nameof(GogAchievementConfiguration))
                    );
                    services.Configure<AchievementSourceConfiguration>(
                        context.Configuration.GetSection(nameof(AchievementSourceConfiguration))
                    );
                    services.AddTransient<SteamAchievementsRetrieving>();
                    services.AddTransient<GogAchievementsRetrieving>();
                    services.AddSingleton<IAchievementParserDispatcher, AchievementParserDispatcher>();
                    services.AddSingleton<IAchievementsRetrievingFactory, AchievementsRetrievingFactory>();
                    services.AddSingleton<IFileService, FileService>();
                });
    }
}
