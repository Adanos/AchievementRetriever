using System.Threading.Tasks;
using AchievementRetriever.Models.FromApi;

namespace AchievementRetriever;

public interface IAchievementsRetrieving
{
    Task<AchievementsResponse> GetAllAchievementsAsync();
    string GetFilePathToSaveResult();
    bool? GetFlagIsAchieved();
}