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
            stats.RecordDraw();

            _repository.Save(stats);
            var loaded = _repository.Load();

            Assert.IsTrue(File.Exists(_repository.FilePath));
            Assert.IsFalse(File.Exists(_repository.FilePath + ".tmp"));
            Assert.AreEqual(4, loaded.GamesPlayed);
            Assert.AreEqual(1, loaded.Draws);
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

        [Test]
        public void Load_FileFromBeforeDraws_LoadsWithZeroDraws()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_repository.FilePath,
                "{\"version\":1,\"gamesPlayed\":2,\"playerOneWins\":2,\"playerOneLosses\":0,\"playerTwoWins\":0,\"playerTwoLosses\":2}");

            var stats = _repository.Load();

            Assert.AreEqual(2, stats.GamesPlayed);
            Assert.AreEqual(0, stats.Draws);
            Assert.AreEqual(2, stats.GetWins(Player.One));
        }

        [Test]
        public void GetLegacyFilePath_SwapsCompanyFolder()
        {
            var root = Path.Combine(Path.GetTempPath(), "LocalLow");
            var current = Path.Combine(root, "CDCompany", "Juego-Triki");

            var legacy = StatsRepository.GetLegacyFilePath(current, "CDCompany", "DefaultCompany");

            Assert.AreEqual(
                Path.Combine(Path.GetFullPath(root), "DefaultCompany", "Juego-Triki", StatsRepository.DefaultFileName),
                legacy);
        }

        [TestCase("DefaultCompany", "DefaultCompany", TestName = "Misma compañía")]
        [TestCase("OtraCompania", "DefaultCompany", TestName = "La ruta no termina en la compañía")]
        public void GetLegacyFilePath_NotApplicable_ReturnsNull(string companyName, string legacyName)
        {
            var current = Path.Combine(Path.GetTempPath(), "LocalLow", "CDCompany", "Juego-Triki");

            Assert.IsNull(StatsRepository.GetLegacyFilePath(current, companyName, legacyName));
            Assert.IsNull(StatsRepository.GetLegacyFilePath(null, "CDCompany", "DefaultCompany"));
        }

        [Test]
        public void Load_WithoutFile_CopiesLegacyFile_AndKeepsOriginal()
        {
            var legacyPath = WriteLegacyStats(winsForOne: 4);
            var repository = new StatsRepository(Path.Combine(_directory, "nuevo", StatsRepository.DefaultFileName), legacyPath);

            var stats = repository.Load();

            Assert.AreEqual(4, stats.GamesPlayed);
            Assert.AreEqual(4, stats.GetWins(Player.One));
            Assert.IsTrue(File.Exists(repository.FilePath), "Debe quedar la copia en la ruta nueva.");
            Assert.IsTrue(File.Exists(legacyPath), "El original se conserva como respaldo.");
        }

        [Test]
        public void Load_WithExistingFile_IgnoresLegacyFile()
        {
            var legacyPath = WriteLegacyStats(winsForOne: 4);
            var repository = new StatsRepository(Path.Combine(_directory, "nuevo", StatsRepository.DefaultFileName), legacyPath);
            var current = new MatchStats();
            current.RecordWin(Player.Two);
            repository.Save(current);

            var stats = repository.Load();

            Assert.AreEqual(1, stats.GamesPlayed);
            Assert.AreEqual(1, stats.GetWins(Player.Two));
        }

        [Test]
        public void Load_WithMissingLegacyFile_StartsEmpty()
        {
            var repository = new StatsRepository(
                Path.Combine(_directory, "nuevo", StatsRepository.DefaultFileName),
                Path.Combine(_directory, "no-existe", StatsRepository.DefaultFileName));

            Assert.AreEqual(0, repository.Load().GamesPlayed);
            Assert.IsFalse(File.Exists(repository.FilePath));
        }

        private string WriteLegacyStats(int winsForOne)
        {
            var legacy = new StatsRepository(Path.Combine(_directory, "viejo", StatsRepository.DefaultFileName));
            var stats = new MatchStats();
            for (var i = 0; i < winsForOne; i++)
                stats.RecordWin(Player.One);
            legacy.Save(stats);
            return legacy.FilePath;
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
