using System.Collections.Generic;
using System.IO;
using System.Linq;
using AchievementRetriever.Models;

namespace AchievementRetriever.IO
{
    public class ReadFileManager
    {
        public string[] DlcNames { get; private set; }

        public IList<Achievement> ReadAchievementsFromFile(string fileName)
        {
            IList<Achievement> achievements = [];
            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            string line = reader.ReadLine();
            IDictionary<int, string> dlcNameDictionary = CreateDlcNameDictionary(line);

            while ((line = reader.ReadLine()) != null)
            {
                string[] words = line.Split(Constants.Separator);
                bool.TryParse(words[3], out bool isRequiredDlc);
                IList<string> markedDlc = words[4..];
                var indexesOfAnd = Enumerable.Range(0, markedDlc.Count).Where(i => markedDlc[i] == Constants.And);
                var indexesOfOr = Enumerable.Range(0, markedDlc.Count).Where(i => markedDlc[i] == Constants.Or);

                achievements.Add(new Achievement(new GameAchievement() { Name = words[0], Description = words[1]})
                {
                    Countries = words[2].Split(Constants.CountriesSeparator).ToHashSet(),
                    IsRequiredDlc = isRequiredDlc,
                    AllRequiredDlcNames = dlcNameDictionary.Where(x => indexesOfAnd.Contains(x.Key)).Select(x => x.Value).ToList(),
                    OneRequiredOfDlcNames = dlcNameDictionary.Where(x => indexesOfOr.Contains(x.Key)).Select(x => x.Value).ToList(),
                });
            }

            return achievements;
        }

        private IDictionary<int, string> CreateDlcNameDictionary(string line)
        {
            IDictionary<int, string> dlcNameDictionary = new Dictionary<int, string>();
            DlcNames = line.Replace(Constants.HeaderOfCsvFile, "").Split(Constants.Separator);
            for (int i = 0; i < DlcNames.Length; i++)
            {
                dlcNameDictionary.Add(i, DlcNames[i]);
            }

            return dlcNameDictionary;
        }
    }
}
