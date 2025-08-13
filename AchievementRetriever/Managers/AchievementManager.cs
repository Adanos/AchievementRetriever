using Microsoft.Extensions.Configuration;
using SimpleAchievementFileParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AchievementRetriever.IO;
using AchievementRetriever.Models;
using AchievementRetriever.Models.FromApi.Steam;
using AchievementRetriever.Models.FromGameStructure;
using AchievementRetriever.Filters;

namespace AchievementRetriever.Managers
{
    internal class AchievementManager
    {
        private readonly SteamAchievementConfiguration _steamAchievementConfiguration;
        private readonly EuropaUniversalisFilesStructureConfiguration _europaUniversalisFilesStructureConfiguration;
        private readonly IAchievementsRetrieving _achievementsRetrieving;
        private readonly FilenameCreator _filenameCreator;
        private IList<GameAchievement> AchievementsResponse { get; set; }
        private ISet<string> _dlcNames;
        
        public IList<Achievement> Achievements { get; private set; }

        public AchievementManager(IAchievementsRetrievingFactory achievementsRetrievingFactory, IConfiguration configuration)
        {
            _achievementsRetrieving = achievementsRetrievingFactory.GetAchievementsRetrieving();
            _europaUniversalisFilesStructureConfiguration = configuration.GetSection(nameof(EuropaUniversalisFilesStructureConfiguration)).Get<EuropaUniversalisFilesStructureConfiguration>();
            
            _steamAchievementConfiguration = configuration.GetSection(nameof(SteamAchievementConfiguration)).Get<SteamAchievementConfiguration>();
            _filenameCreator = new FilenameCreator(_steamAchievementConfiguration);  
        }

        public async Task CreateAchievements()
        {
            string pattern = _filenameCreator.CreateFilename(@"*");
            string[] files = Directory.GetFiles(_achievementsRetrieving.GetFilePathToSaveResult(), pattern, SearchOption.TopDirectoryOnly);

            if (files.Length > 0)
            {
                Achievements = ReadAchievementsFromFile(files);
            }
            else
            {
                try
                {
                    var results = await _achievementsRetrieving.GetAllAchievementsAsync();

                    if (results.Success)
                    {
                        AchievementsResponse = results.Achievements;
                        FilterAchievements();
                        MapAchievements();
                        SaveAchievementsToFile(results.Achievements.FirstOrDefault()?.GameName);
                    }
                    else
                    {
                        Console.WriteLine("Error, status code: {0}", results.StatusCode);
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or Exception) 
                {
                    Console.WriteLine("Error, message: {0}, inner exception {1}", ex.Message, ex.InnerException);
                }
            }
        }
        
        public void SaveAchievementsToFile(string gameName, IList<Achievement> achievements)
        {
            SaveFileManager saveFileManager = new SaveFileManager(_filenameCreator.CreateFullPath(gameName), _dlcNames, achievements);
            saveFileManager.SaveCsvFile();
        }

        private void MapAchievements()
        {
            string gameDirectory = _europaUniversalisFilesStructureConfiguration.GameDirectory;
            AchievementsDescriptionFileParser achievementsDescriptionFileParser = new(gameDirectory + _europaUniversalisFilesStructureConfiguration.AchievementsLocalisationPath);
            AchievementsStructureFileParser achievementsStructureFileParser = new(gameDirectory + _europaUniversalisFilesStructureConfiguration.AchievementsRequirementsPath);
            var descriptions = achievementsDescriptionFileParser.ParseFile();
            var requirements = achievementsStructureFileParser.ParseFile(descriptions);
            _dlcNames = achievementsStructureFileParser.DlcNames;

            Achievements = [];
            foreach (var achievement in AchievementsResponse)
            {
                var requiredDlcs = requirements.FirstOrDefault(x => x.Name == achievement.Name)
                    ?.VisibleRequirements?.HasAllDlc;
                var oneOfDlcRequired = requirements.FirstOrDefault(x => x.Name == achievement.Name)
                    ?.VisibleRequirements?.HasOneOfDlc?.SelectMany(x => x.Names);
                Achievements.Add(new Achievement(achievement)
                {
                    IsRequiredDlc = (requiredDlcs?.Any() ?? false) || (oneOfDlcRequired?.Any(x => x.Key == SimpleAchievementFileParser.Constants.TokenHasDlc) ?? false),
                    AllRequiredDlcNames = requiredDlcs,
                    OneRequiredOfDlcNames = oneOfDlcRequired?.Where(x => x.Key == SimpleAchievementFileParser.Constants.TokenHasDlc)?.Select(x => x.Value)?.ToList()
                });
            }
        }

        private void FilterAchievements()
        {
            AchievementGrouping achievementGrouping = new AchievementGrouping(AchievementsResponse);

            if (_steamAchievementConfiguration.IsAchieved == true)
                AchievementsResponse = achievementGrouping.GetUnlockedAchievements();
            else if (_steamAchievementConfiguration.IsAchieved == false)
                AchievementsResponse = achievementGrouping.GetLockedAchievements();
        }

        private IList<Achievement> ReadAchievementsFromFile(string[] files)
        {
            ReadFileManager readFileManager = new ReadFileManager();
            var achievements = readFileManager.ReadAchievementsFromFile(files[0]);
            _dlcNames = readFileManager.DlcNames.ToHashSet();
            return achievements;
        }

        private void SaveAchievementsToFile(string gameName)
        {
            SaveFileManager saveFileManager = new SaveFileManager(_filenameCreator.CreateFullPath(gameName), _dlcNames, Achievements);
            saveFileManager.SaveCsvFile();
        }
    }
}
