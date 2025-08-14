using System.Collections.Generic;

namespace AchievementRetriever.Models
{
    public class Achievement
    {
        public bool Achieved { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Achievement(GameAchievement achievement)
        {
            Achieved = achievement.IsUnlocked.GetValueOrDefault();
            Name = achievement.Name;
            Description = achievement.Description;
        }

        public ISet<string> Countries { get; set; } = new HashSet<string>();
        public bool IsRequiredDlc { get; set; }
        public IList<string> AllRequiredDlcNames { get; set; } = [];
        public IList<string> OneRequiredOfDlcNames { get; set; } = [];
    }
}
