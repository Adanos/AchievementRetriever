using NUnit.Framework;
using AchievementRetriever;
using AchievementRetriever.Models;
using System.Collections.Generic;

namespace AchievementRetrieverTests
{
    class AchievementMatrixCreatorTests
    {
        private IList<Achievement> Achievements { get; set; }
        [SetUp]
        public void Setup()
        {
            GameAchievement achievementResponse = new GameAchievement() { IsUnlocked = true, Name = "", Description = "" };
            Achievements = new List<Achievement>()
            {
                new Achievement(achievementResponse) { Name = "Test 1", Description = "As the Knights conquer the world." },
                new Achievement(achievementResponse) { Name = "Test 2", Description = "As The Knights have 1000 gold." },
                new Achievement(achievementResponse) { Name = "Test 3", Description = "Win a war as Cyprus." },
                new Achievement(achievementResponse) { Name = "Test 4", Description = "As the Papacy become emperor." },
                new Achievement(achievementResponse) { Name = "Test 5", Description = "Own Jerusalem as the Papal State." }
            };
        }

        [Test]
        public void MergeTheSameKeys_KeysAreSimilarCaseSensitive_ReturnOneKey()
        {
            AchievementMatrixCreator achievementMatrixCreator = new AchievementMatrixCreator(Achievements);
            achievementMatrixCreator.CreateMatrix();
            achievementMatrixCreator.Matrix.TryGetValue("Knights", out List<string> values);

            Assert.That(achievementMatrixCreator.Matrix.ContainsKey("Knights"), Is.True);
            Assert.That(achievementMatrixCreator.Matrix.ContainsKey("the Knights"), Is.False);
            Assert.That(achievementMatrixCreator.Matrix.ContainsKey("The Knights"), Is.False);
            Assert.That(values, Is.Not.Null);
            Assert.That(values.Count, Is.EqualTo(2));
        }

        [Test]
        public void MergeTheSameKeys_DifferentNamesForTheSameCountry_ReturnOneKey()
        {
            AchievementMatrixCreator achievementMatrixCreator = new AchievementMatrixCreator(Achievements);
            achievementMatrixCreator.CreateMatrix();
            achievementMatrixCreator.Matrix.TryGetValue("Papal State", out List<string> values);

            Assert.That(achievementMatrixCreator.Matrix.ContainsKey("Papal State"), Is.True);
            Assert.That(achievementMatrixCreator.Matrix.ContainsKey("Papacy"), Is.False);
            Assert.That(values, Is.Not.Null);
            Assert.That(values.Count, Is.EqualTo(2));
        }
    }
}
