using System.Text.Json.Serialization;

namespace AchievementRetriever.Models;

public class GameAchievement
{
    [JsonPropertyName("gameName")]
    public string GameName { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("isUnlocked")]
    public bool? IsUnlocked { get; set; }
}