using Microsoft.Extensions.Configuration;
using SimpleAchievementFileParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AchievementRetriever.IO;
using AchievementRetriever.Models;
using AchievementRetriever.Models.FromGameStructure;
using AchievementRetriever.Filters;

namespace AchievementRetriever.Managers
{
    public class AchievementManager
    {
        private string GameName { get; set; }
        private readonly EuropaUniversalisFilesStructureConfiguration _europaUniversalisFilesStructureConfiguration;
        private readonly IAchievementsRetrieving _achievementsRetrieving;
        private readonly FilenameCreator _filenameCreator;
        private readonly IFileService _fileService;
        private IList<GameAchievement> AchievementsResponse { get; set; }
        private ISet<string> _dlcNames;

        public IList<Achievement> Achievements { get; private set; }

        public AchievementManager(IAchievementsRetrievingFactory achievementsRetrievingFactory, IFileService fileService, IConfiguration configuration)
        {
            _achievementsRetrieving = achievementsRetrievingFactory.GetAchievementsRetrieving();
            _fileService = fileService;
            _europaUniversalisFilesStructureConfiguration = configuration.GetSection(nameof(EuropaUniversalisFilesStructureConfiguration)).Get<EuropaUniversalisFilesStructureConfiguration>();
            var source = configuration.GetSection(nameof(AchievementSourceConfiguration)).Get<AchievementSourceConfiguration>().Name;
            _filenameCreator = new FilenameCreator(source, _achievementsRetrieving.GetFlagIsAchieved(), _achievementsRetrieving.GetFilePathToSaveResult());  
        }

        public async Task CreateAchievements()
        {
            string pattern = _filenameCreator.CreateFilename(@"*");
            var files = _fileService.TryGetFilesFromConfig(_achievementsRetrieving.GetFilePathToSaveResult(), pattern);
            
            if (files.Success)
            {
                Achievements = ReadAchievementsFromFile(files.Result);
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
                    OneRequiredOfDlcNames = oneOfDlcRequired?.Where(x => x.Key == SimpleAchievementFileParser.Constants.TokenHasDlc).Select(x => x.Value).ToList()
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

        private IList<Achievement> ReadAchievementsFromFile(IEnumerable<string> files)
        {
            ReadFileManager readFileManager = new ReadFileManager();
            var achievements = readFileManager.ReadAchievementsFromFile(files.FirstOrDefault());
            _dlcNames = readFileManager.DlcNames.ToHashSet();
            return achievements;
        }
    }
}
