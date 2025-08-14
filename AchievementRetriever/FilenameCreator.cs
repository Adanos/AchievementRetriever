namespace AchievementRetriever
{
    class FilenameCreator(bool? isAchievedFlag, string filePathToSaveResult)
    {
        public string CreateFilename(string gameName, string extension = Constants.CsvExtension)
        {
            string achievements = Constants.Achievements;
            if (isAchievedFlag == true)
                achievements = Constants.UnlockedAchievements;
            else if (isAchievedFlag == false)
                achievements = Constants.LockedAchievements;
            return achievements + gameName + extension;
        }

        public string CreateFullPath(string gameName, string extension = Constants.CsvExtension)
        {
            return filePathToSaveResult + CreateFilename(gameName, extension);
        }
    }
}
