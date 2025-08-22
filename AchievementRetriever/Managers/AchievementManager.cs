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
using AchievementRetriever.Models.FromGameStructure;
using AchievementRetriever.Filters;

namespace AchievementRetriever.Managers
{
    internal class AchievementManager
    {
        public string GameName { get; set; }
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
            var source = configuration.GetSection(nameof(AchievementSourceConfiguration)).Get<AchievementSourceConfiguration>().Name;
            _filenameCreator = new FilenameCreator(source, _achievementsRetrieving.GetFlagIsAchieved(), _achievementsRetrieving.GetFilePathToSaveResult());  
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
                        GameName = results.Achievements.FirstOrDefault()?.GameName;
                        SaveAchievementsToFile();
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

        

        public void SaveAchievementsToFile(string suffix = "")
        {
            SaveFileManager saveFileManager = new SaveFileManager(_filenameCreator.CreateFullPath(GameName, suffix: suffix), _dlcNames, Achievements);
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

            if (_achievementsRetrieving.GetFlagIsAchieved() == true)
                AchievementsResponse = achievementGrouping.GetUnlockedAchievements();
            else if (_achievementsRetrieving.GetFlagIsAchieved() == false)
                AchievementsResponse = achievementGrouping.GetLockedAchievements();
        }

        private IList<Achievement> ReadAchievementsFromFile(string[] files)
        {
            ReadFileManager readFileManager = new ReadFileManager();
            var achievements = readFileManager.ReadAchievementsFromFile(files[0]);
            _dlcNames = readFileManager.DlcNames.ToHashSet();
            return achievements;
        }
    }
}
