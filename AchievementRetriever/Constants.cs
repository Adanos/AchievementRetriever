namespace AchievementRetriever
{
    public static class Constants
    {
        public const string HeaderJsonType = "application/json",
            ApplicationIdWithQuestionMarkParam = "?appid",
            AuthenticationKeyParam = "key",
            SteamIdParam = "steamid",
            LanguageParam = "l",
            HeaderOfCsvFile = "Achievement;Description;Country;Is dlc required?;",
            Separator = ";",
            CountriesSeparator = ",",
            Updated = "updated",
            CsvExtension = ".csv",
            Achievements = "Achievements",
            UnlockedAchievements = "UnlockedAchievements",
            LockedAchievements = "LockedAchievements",
            And = "AND",
            Or = "OR";
    }
}
