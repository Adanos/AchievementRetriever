using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using AchievementRetriever.JsonParsers;
using AchievementRetriever.Models.FromApi.Steam;
using Microsoft.Extensions.Options;

namespace AchievementRetrieverTests
{
    [TestFixture]
    public class SteamAchievementsRetrievingTests
    {
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<IAchievementParserDispatcher> _achievementParserDispatcherMock;
        private IOptions<SteamAchievementConfiguration> _configuration;

        [SetUp]
        public void SetUp()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _achievementParserDispatcherMock = new Mock<IAchievementParserDispatcher>();
            _configuration = Options.Create(new SteamAchievementConfiguration
            {
                AddressApi = "https://fake-url.com",
                AuthenticationKey = "fake-auth-key",
                ApplicationId = "12345",
                SteamId = "1234567890",
                Language = "en"
            });
            _achievementParserDispatcherMock
                .Setup(x => x.GetParser())
                .Returns(new SteamAchievementParser());
        }

        [Test]
        public async Task GetAllAchievementsAsync_ReturnsSuccessResponse()
        {
            // Arrange
            var jsonResponse = @"
            {
                ""playerstats"": {
                    ""gameName"": ""Test Game"",
                    ""achievements"": [
                        {
                            ""name"": ""achievement_1"",
                            ""achieved"": 1,
                            ""description"": ""desc_1""
                        },
                        {
                            ""name"": ""achievement_2"",
                            ""achieved"": 0,
                            ""description"": ""desc_2""
                        }
                    ]
                }
            }";

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
                    Content = new StringContent(jsonResponse)
                });

            var client = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var service = new AchievementRetriever.SteamAchievementsRetrieving(_httpClientFactoryMock.Object.CreateClient(), _achievementParserDispatcherMock.Object, _configuration);

            // Act
            var result = await service.GetAllAchievementsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetAllAchievementsAsync_ReturnsErrorResponse_OnFailure()
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

            var service = new AchievementRetriever.SteamAchievementsRetrieving(_httpClientFactoryMock.Object.CreateClient(), _achievementParserDispatcherMock.Object, _configuration);

            // Act
            var result = await service.GetAllAchievementsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void GetAllAchievementsAsync_ThrowsException_OnInvalidConfiguration()
        {
            // Arrange
            _configuration.Value.AddressApi = null;
            var service = new AchievementRetriever.SteamAchievementsRetrieving(_httpClientFactoryMock.Object.CreateClient(), _achievementParserDispatcherMock.Object, _configuration);

            // Act & Assert
            Assert.That(
                service.GetAllAchievementsAsync,
                Throws.TypeOf<InvalidOperationException>()
            );
        }
    }
}