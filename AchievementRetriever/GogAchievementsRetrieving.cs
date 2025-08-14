using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using AchievementRetriever.JsonParsers;
using AchievementRetriever.Models.FromApi;
using AchievementRetriever.Models.FromApi.Gog;
using Microsoft.Extensions.Options;

namespace AchievementRetriever
{
    public class GogAchievementsRetrieving : IAchievementsRetrieving
    {
        private readonly HttpClient _httpClient;
        private readonly IAchievementParser _achievementParser;
        private readonly GogAchievementConfiguration _gogAchievementConfiguration;

        public GogAchievementsRetrieving(HttpClient httpClient, IAchievementParserDispatcher achievementParserDispatcher, IOptions<GogAchievementConfiguration> gogConfigOptions)
        {
            _httpClient = httpClient;
            _achievementParser = achievementParserDispatcher.GetParser();
            _gogAchievementConfiguration = gogConfigOptions.Value;
        }

        public async Task<AchievementsResponse> GetAllAchievementsAsync()
        {
            if (string.IsNullOrWhiteSpace(_gogAchievementConfiguration.AddressApi) || string.IsNullOrWhiteSpace(_gogAchievementConfiguration.User))
            {
                throw new InvalidOperationException("Invalid configuration for Gog achievements API.");
            }

            var response = new AchievementsResponse();

            try
            {
                var baseAddress = _gogAchievementConfiguration.AddressApi
                    .Replace("{User}", Uri.EscapeDataString(_gogAchievementConfiguration.User))
                    .Replace("{GameId}", Uri.EscapeDataString(_gogAchievementConfiguration.GameId));
                
                _httpClient.BaseAddress = new Uri(baseAddress);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.HeaderJsonType));

                var achievementsResponse = await _httpClient.GetAsync(_httpClient.BaseAddress);

                response.StatusCode = achievementsResponse.StatusCode;

                if (achievementsResponse.IsSuccessStatusCode)
                {
                    var content = await achievementsResponse.Content.ReadAsStringAsync();
                    response.Achievements = _achievementParser.Parse(content);
                    response.GameName = response.Achievements.FirstOrDefault()?.GameName;
                    response.Success = true;
                }
            }
            catch (UriFormatException ex)
            {
                response.Success = false;
                response.ErrorMessage = $"Request failed: {ex.Message}";
                throw;
            }
            catch (HttpRequestException ex)
            {
                response.Success = false;
                response.ErrorMessage = $"Request failed: {ex.Message}";
                throw;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = $"An error occurred: {ex.Message}";
                throw;
            }

            return response;
        }

        public string GetFilePathToSaveResult()
        {
            return _gogAchievementConfiguration.FilePathToSaveResult;
        }

        public bool? GetFlagIsAchieved()
        {
            return _gogAchievementConfiguration.IsAchieved;
        }
    }
}
