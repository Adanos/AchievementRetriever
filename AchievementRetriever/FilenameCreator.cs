using AchievementRetriever.Models;

namespace AchievementRetriever
{
    public class FilenameCreator(AchievementSource source, bool? isAchievedFlag, string filePathToSaveResult)
    {
        public string CreateFilename(string gameName, string extension = Constants.CsvExtension, string suffix = "")
        {
            string achievements = Constants.Achievements;
            if (isAchievedFlag == true)
                achievements = Constants.UnlockedAchievements;
            else if (isAchievedFlag == false)
                achievements = Constants.LockedAchievements;
            return $"{source} {achievements} {gameName} {suffix}{extension}";
        }

        public string CreateFullPath(string gameName, string extension = Constants.CsvExtension, string suffix = "")
        {
            return filePathToSaveResult + CreateFilename(gameName, extension, suffix);
        }
    }
}
