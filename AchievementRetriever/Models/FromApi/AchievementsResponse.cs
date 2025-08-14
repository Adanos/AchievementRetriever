using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace AchievementRetriever.Models.FromApi;

public class AchievementsResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; internal set; }
    [JsonPropertyName("gameName")]
    public string GameName { get; set; }
    [JsonPropertyName("achievements")]
    public IList<GameAchievement> Achievements { get; set; }
}