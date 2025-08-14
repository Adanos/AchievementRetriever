using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using AchievementRetriever;
using AchievementRetriever.JsonParsers;
using AchievementRetriever.Models;
using AchievementRetriever.Models.FromApi;
using AchievementRetriever.Models.FromApi.Gog;
using Microsoft.Extensions.Options;

namespace AchievementRetrieverTests
{
    [TestFixture]
    public class GogAchievementServiceTests
    {
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private IOptions<GogAchievementConfiguration> _configuration;
        private Mock<IAchievementParserDispatcher> _achievementParserDispatcherMock;

        [SetUp]
        public void SetUp()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _achievementParserDispatcherMock = new Mock<IAchievementParserDispatcher>();
            
            _configuration = Options.Create(new GogAchievementConfiguration
            {
                AddressApi = "https://www.gog.com/u/{0}/game/{1}",
                User = "testUser",
                GameId = "12345"
            });
            
            _achievementParserDispatcherMock
                .Setup(x => x.GetParser())
                .Returns(new GogAchievementParser());
        }

        [Test]
        public async Task GetAllAchievements_ReturnsSuccessResponse()
        {
            // Arrange
            var expectedResponse = new AchievementsResponse
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Achievements = new List<GameAchievement>(){ new GameAchievement() {GameName = "Name", Description = "desc", Name = "name", IsUnlocked = false} }
            };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        "window.profilesData.achievements=[{\"achievement\":{\"name\":\"name\",\"description\":\"desc\",\"isUnlocked\":false}}];")
                });

            var client = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var service = new GogAchievementsRetrieving(_httpClientFactoryMock.Object.CreateClient(), _achievementParserDispatcherMock.Object, _configuration);

            // Act
            var result = await service.GetAllAchievementsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetAllAchievements_ReturnsErrorResponse_OnFailure()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("")
                });

            var client = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var service = new GogAchievementsRetrieving(_httpClientFactoryMock.Object.CreateClient(), _achievementParserDispatcherMock.Object, _configuration);

            // Act
            var result = await service.GetAllAchievementsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void GetAllAchievements_ReturnsError_OnInvalidUri()
        {
            // Arrange
            _configuration.Value.AddressApi = "invalid-url";
            var service = new GogAchievementsRetrieving(_httpClientFactoryMock.Object.CreateClient(), _achievementParserDispatcherMock.Object, _configuration);

            // Act & Assert
            Assert.That(
                service.GetAllAchievementsAsync,
                Throws.TypeOf<UriFormatException>()
            );
        }

        [Test]
        public void GetAllAchievements_ReturnsErrorResponse_OnTimeout()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new TaskCanceledException("Request timed out"));

            var client = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var service = new GogAchievementsRetrieving(_httpClientFactoryMock.Object.CreateClient(), _achievementParserDispatcherMock.Object, _configuration);

            // Act & Assert
            Assert.That(
                service.GetAllAchievementsAsync,
                Throws.TypeOf<TaskCanceledException>()
            );
        }
        
        [Test]
        public void ParseJsonFromHtmlTests_ParseFileWithTwoDescription_ReturnObject()
        {
            var path = Path.Combine("HtmlTestCase", "GogAchievementsTestCase.txt");
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            string jsonFromHtml = reader.ReadToEnd();
            AchievementSourceConfiguration achievementSourceConfiguration = new()
            {
                Name = AchievementSource.GoG
            };
            AchievementParserDispatcher achievementParserDispatcher = new AchievementParserDispatcher(achievementSourceConfiguration);

            var result = achievementParserDispatcher.GetParser().Parse(jsonFromHtml).ToList();
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First().Name, Is.EqualTo("Doge Coins"));
            Assert.That(result.First().Description, Is.EqualTo("Starting as Venice, become the best."));
            Assert.That(result.First().IsUnlocked, Is.True);
            Assert.That(result.Last().Name, Is.EqualTo("New achievement"));
            Assert.That(result.Last().Description, Is.EqualTo("Starting as any Mayan country, conquer the world"));
            Assert.That(result.Last().IsUnlocked, Is.False);
        }
    }
}
