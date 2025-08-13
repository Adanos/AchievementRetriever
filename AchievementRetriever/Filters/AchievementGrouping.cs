using System.Collections.Generic;
using System.Linq;
using AchievementRetriever.Models;

namespace AchievementRetriever.Filters
{
    public class AchievementGrouping(IList<GameAchievement> achievements)
    {
        private IList<GameAchievement> Achievements { get; } = achievements;

        public IList<GameAchievement> GetUnlockedAchievements()
        {
            return Achievements.Where(achievement => achievement.IsUnlocked == true).ToList();
        }

        public IList<GameAchievement> GetLockedAchievements()
        {
            return Achievements.Where(achievement => achievement.IsUnlocked != true).ToList();
        }
    }
}
