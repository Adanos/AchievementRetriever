using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using AchievementRetriever.JsonParsers;
using AchievementRetriever.Models.FromApi;
using AchievementRetriever.Models.FromApi.Steam;
using Microsoft.Extensions.Options;

namespace AchievementRetriever
{
    public class SteamAchievementsRetrieving : IAchievementsRetrieving
    {
        private readonly HttpClient _httpClient;
        private readonly IAchievementParser _achievementParser;
        private readonly SteamAchievementConfiguration _steamAchievementConfiguration;

        public SteamAchievementsRetrieving(HttpClient httpClient, IAchievementParserDispatcher achievementParserDispatcher, IOptions<SteamAchievementConfiguration> steamConfigOptions)
        {
            _httpClient = httpClient;
            _steamAchievementConfiguration = steamConfigOptions.Value;
            _achievementParser = achievementParserDispatcher.GetParser();
        }

        public async Task<AchievementsResponse> GetAllAchievementsAsync()
        {
            var response = new AchievementsResponse();

            if (string.IsNullOrWhiteSpace(_steamAchievementConfiguration.AddressApi) || string.IsNullOrWhiteSpace(_steamAchievementConfiguration.ApplicationId) ||
                string.IsNullOrWhiteSpace(_steamAchievementConfiguration.AuthenticationKey) || string.IsNullOrWhiteSpace(_steamAchievementConfiguration.SteamId) ||
                string.IsNullOrWhiteSpace(_steamAchievementConfiguration.Language))
            {
                throw new InvalidOperationException("Invalid Steam achievement configuration.");
            }

            try
            {
                var uriBuilder = new UriBuilder(_steamAchievementConfiguration.AddressApi);
                var query = HttpUtility.ParseQueryString(string.Empty);
                query[Constants.ApplicationIdWithQuestionMarkParam] = _steamAchievementConfiguration.ApplicationId;
                query[Constants.AuthenticationKeyParam] = _steamAchievementConfiguration.AuthenticationKey;
                query[Constants.SteamIdParam] = _steamAchievementConfiguration.SteamId;
                query[Constants.LanguageParam] = _steamAchievementConfiguration.Language;
                uriBuilder.Query = query.ToString();

                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.HeaderJsonType));

                var achievementsResponse = await _httpClient.GetAsync(uriBuilder.Uri);

                response.StatusCode = achievementsResponse.StatusCode;

                if (achievementsResponse.IsSuccessStatusCode)
                {
                    var content = await achievementsResponse.Content.ReadAsStringAsync();
                    response.Achievements = _achievementParser.Parse(content);
                    response.GameName = response.Achievements.FirstOrDefault()?.GameName;
                    response.Success = true;
                }
            }
            catch (HttpRequestException ex)
            {
                response.Success = false;
                response.ErrorMessage = $"HTTP request failed: {ex.Message}";
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
            return _steamAchievementConfiguration.FilePathToSaveResult;
        }

        public bool? GetFlagIsAchieved()
        {
            return _steamAchievementConfiguration.IsAchieved;
        }
    }
}
