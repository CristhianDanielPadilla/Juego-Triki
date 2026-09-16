using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Triki.Core;
using Triki.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace Triki.Tests
{
    public class StatsRepositoryTests
    {
        private string _directory;
        private StatsRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "TrikiTests", Path.GetRandomFileName());
            _repository = new StatsRepository(Path.Combine(_directory, StatsRepository.DefaultFileName));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void Load_WithoutFile_ReturnsEmptyStats()
        {
            var stats = _repository.Load();

            Assert.AreEqual(0, stats.GamesPlayed);
        }

        [Test]
        public void SaveThenLoad_RoundTrips_AndCreatesDirectory()
        {
            var stats = new MatchStats();
            stats.RecordWin(Player.One);
            stats.RecordWin(Player.Two);
            stats.RecordWin(Player.Two);

            _repository.Save(stats);
            var loaded = _repository.Load();

            Assert.IsTrue(File.Exists(_repository.FilePath));
            Assert.IsFalse(File.Exists(_repository.FilePath + ".tmp"));
            Assert.AreEqual(3, loaded.GamesPlayed);
            Assert.AreEqual(1, loaded.GetWins(Player.One));
            Assert.AreEqual(2, loaded.GetLosses(Player.One));
            Assert.AreEqual(2, loaded.GetWins(Player.Two));
            Assert.AreEqual(1, loaded.GetLosses(Player.Two));
        }

        [Test]
        public void Save_OverwritesPreviousFile()
        {
            var stats = new MatchStats();
            stats.RecordWin(Player.One);
            _repository.Save(stats);

            stats.RecordWin(Player.One);
            _repository.Save(stats);

            Assert.AreEqual(2, _repository.Load().GamesPlayed);
        }

        [TestCase("esto no es json", TestName = "JSON inválido")]
        [TestCase("{\"gamesPlayed\": -3}", TestName = "Valores negativos")]
        public void Load_CorruptFile_WarnsAndReturnsEmptyStats(string content)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_repository.FilePath, content);
            LogAssert.Expect(LogType.Warning, new Regex("No se pudo leer el histórico"));

            var stats = _repository.Load();

            Assert.AreEqual(0, stats.GamesPlayed);
        }
    }
}
