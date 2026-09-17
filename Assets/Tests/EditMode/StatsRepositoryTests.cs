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
        // Archivo tal como lo guardaba v0.1.x (formato 1, sin modo de juego).
        private const string V1File =
            "{\"version\":1,\"gamesPlayed\":6,\"draws\":1,\"playerOneWins\":3,\"playerOneLosses\":2,\"playerTwoWins\":2,\"playerTwoLosses\":3}";

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
        public void Load_WithoutFile_ReturnsEmptyHistory()
        {
            var history = _repository.Load();

            Assert.AreEqual(0, history.GamesPlayed);
            Assert.IsFalse(history.HasLegacy);
        }

        [Test]
        public void SaveThenLoad_RoundTripsEverySection_AndCreatesDirectory()
        {
            var history = new MatchHistory();
            history.TwoPlayer.RecordWin(Player.One);
            history.TwoPlayer.RecordWin(Player.Two);
            history.TwoPlayer.RecordDraw();
            history.VsAi.RecordWin(AiDifficulty.Easy);
            history.VsAi.RecordLoss(AiDifficulty.Hard);
            history.VsAi.RecordLoss(AiDifficulty.Hard);
            history.VsAi.RecordDraw(AiDifficulty.Normal);
            history.Legacy.Restore(5, 1, 2, 2, 2, 2);

            _repository.Save(history);
            var loaded = _repository.Load();

            Assert.IsTrue(File.Exists(_repository.FilePath));
            Assert.IsFalse(File.Exists(_repository.FilePath + ".tmp"));

            Assert.AreEqual(3, loaded.TwoPlayer.GamesPlayed);
            Assert.AreEqual(1, loaded.TwoPlayer.Draws);
            Assert.AreEqual(1, loaded.TwoPlayer.GetWins(Player.One));
            Assert.AreEqual(1, loaded.TwoPlayer.GetLosses(Player.One));

            Assert.AreEqual(4, loaded.VsAi.GamesPlayed);
            Assert.AreEqual(1, loaded.VsAi.GetWins(AiDifficulty.Easy));
            Assert.AreEqual(1, loaded.VsAi.GetDraws(AiDifficulty.Normal));
            Assert.AreEqual(2, loaded.VsAi.GetLosses(AiDifficulty.Hard));

            Assert.AreEqual(5, loaded.Legacy.GamesPlayed);
            Assert.AreEqual(12, loaded.GamesPlayed);
        }

        [Test]
        public void Save_WritesFormatVersion2()
        {
            _repository.Save(new MatchHistory());

            StringAssert.Contains("\"version\": 2", File.ReadAllText(_repository.FilePath));
        }

        [Test]
        public void Save_OverwritesPreviousFile()
        {
            var history = new MatchHistory();
            history.VsAi.RecordWin(AiDifficulty.Normal);
            _repository.Save(history);

            history.VsAi.RecordWin(AiDifficulty.Normal);
            _repository.Save(history);

            Assert.AreEqual(2, _repository.Load().VsAi.GetWins(AiDifficulty.Normal));
        }

        [Test]
        public void Load_V1File_GoesToLegacySection()
        {
            WriteFile(_repository.FilePath, V1File);

            var history = _repository.Load();

            Assert.IsTrue(history.HasLegacy);
            Assert.AreEqual(6, history.Legacy.GamesPlayed);
            Assert.AreEqual(1, history.Legacy.Draws);
            Assert.AreEqual(3, history.Legacy.GetWins(Player.One));
            Assert.AreEqual(3, history.Legacy.GetLosses(Player.Two));
            Assert.AreEqual(0, history.TwoPlayer.GamesPlayed);
            Assert.AreEqual(0, history.VsAi.GamesPlayed);
        }

        [Test]
        public void Load_V1FileBeforeDraws_LoadsWithZeroDraws()
        {
            WriteFile(_repository.FilePath,
                "{\"version\":1,\"gamesPlayed\":2,\"playerOneWins\":2,\"playerOneLosses\":0,\"playerTwoWins\":0,\"playerTwoLosses\":2}");

            var history = _repository.Load();

            Assert.AreEqual(2, history.Legacy.GamesPlayed);
            Assert.AreEqual(0, history.Legacy.Draws);
        }

        [Test]
        public void LegacySection_SurvivesNewGames()
        {
            WriteFile(_repository.FilePath, V1File);
            var history = _repository.Load();
            history.VsAi.RecordWin(AiDifficulty.Easy);

            _repository.Save(history);
            var reloaded = _repository.Load();

            Assert.AreEqual(6, reloaded.Legacy.GamesPlayed);
            Assert.AreEqual(1, reloaded.VsAi.GamesPlayed);
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
        public void Load_WithoutFile_CopiesFileFromOldCompany_AndKeepsOriginal()
        {
            var oldPath = Path.Combine(_directory, "viejo", StatsRepository.DefaultFileName);
            WriteFile(oldPath, V1File);
            var repository = new StatsRepository(Path.Combine(_directory, "nuevo", StatsRepository.DefaultFileName), oldPath);

            var history = repository.Load();

            Assert.AreEqual(6, history.Legacy.GamesPlayed, "Un archivo de v0.1.x va a la sección Anteriores.");
            Assert.IsTrue(File.Exists(repository.FilePath), "Debe quedar la copia en la ruta nueva.");
            Assert.IsTrue(File.Exists(oldPath), "El original se conserva como respaldo.");
        }

        [Test]
        public void Load_WithExistingFile_IgnoresOldCompanyFile()
        {
            var oldPath = Path.Combine(_directory, "viejo", StatsRepository.DefaultFileName);
            WriteFile(oldPath, V1File);
            var repository = new StatsRepository(Path.Combine(_directory, "nuevo", StatsRepository.DefaultFileName), oldPath);
            var current = new MatchHistory();
            current.TwoPlayer.RecordWin(Player.Two);
            repository.Save(current);

            var history = repository.Load();

            Assert.AreEqual(1, history.GamesPlayed);
            Assert.IsFalse(history.HasLegacy);
        }

        [Test]
        public void Load_WithMissingOldCompanyFile_StartsEmpty()
        {
            var repository = new StatsRepository(
                Path.Combine(_directory, "nuevo", StatsRepository.DefaultFileName),
                Path.Combine(_directory, "no-existe", StatsRepository.DefaultFileName));

            Assert.AreEqual(0, repository.Load().GamesPlayed);
            Assert.IsFalse(File.Exists(repository.FilePath));
        }

        [TestCase("esto no es json", TestName = "JSON inválido")]
        [TestCase("{\"gamesPlayed\": -3}", TestName = "Valores negativos (v1)")]
        [TestCase("{\"version\": 2, \"vsAi\": {\"hard\": {\"wins\": -1}}}", TestName = "Valores negativos (v2)")]
        public void Load_CorruptFile_WarnsAndReturnsEmptyHistory(string content)
        {
            WriteFile(_repository.FilePath, content);
            LogAssert.Expect(LogType.Warning, new Regex("No se pudo leer el histórico"));

            var history = _repository.Load();

            Assert.AreEqual(0, history.GamesPlayed);
        }

        private static void WriteFile(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
    }
}
