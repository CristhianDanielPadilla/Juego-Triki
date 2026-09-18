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

            Assert.AreEqual(0, history.GetGamesPlayed(HistorySection.Overall));
            Assert.AreEqual(0, history.GetGamesPlayed(HistorySection.VsAi));
        }

        [Test]
        public void SaveThenLoad_RoundTripsEverySection_AndCreatesDirectory()
        {
            var history = new MatchHistory();
            history.TwoPlayer.RecordWin(Player.One);
            history.TwoPlayer.RecordWin(Player.Two);
            history.TwoPlayer.RecordDraw();
            history.VsAi.RecordWin(AiDifficulty.Easy, Player.One);
            history.VsAi.RecordLoss(AiDifficulty.Hard, Player.Two);
            history.VsAi.RecordLoss(AiDifficulty.Hard, Player.Two);
            history.VsAi.RecordDraw(AiDifficulty.Normal, Player.One);
            history.Overall.Restore(5, 1, 2, 2, 2, 2);

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

            Assert.AreEqual(5, loaded.Overall.GamesPlayed);
        }

        [Test]
        public void Save_WritesFormatVersion3()
        {
            _repository.Save(new MatchHistory());

            StringAssert.Contains("\"version\": 3", File.ReadAllText(_repository.FilePath));
        }

        [Test]
        public void Save_AlsoWritesAiTotalsWithoutColour_SoOlderVersionsKeepTheirData()
        {
            // v0.2.x y v0.3.0 leen "vsAi" (totales por dificultad). Si dejáramos de escribirlo,
            // al abrir el archivo con una versión anterior el registro contra la IA saldría a cero.
            var history = new MatchHistory();
            history.VsAi.RecordWin(AiDifficulty.Hard, Player.One);
            history.VsAi.RecordWin(AiDifficulty.Hard, Player.Two);
            history.VsAi.RecordLoss(AiDifficulty.Hard, Player.Two);

            _repository.Save(history);
            var data = JsonUtility.FromJson<StatsRepository.HistoryData>(File.ReadAllText(_repository.FilePath));

            Assert.AreEqual(2, data.vsAi.hard.wins, "El total suma los dos colores.");
            Assert.AreEqual(1, data.vsAi.hard.losses);
            Assert.AreEqual(1, data.vsAiByColor.hard.red.wins);
            Assert.AreEqual(1, data.vsAiByColor.hard.blue.wins);
            Assert.AreEqual(1, data.vsAiByColor.hard.blue.losses);
        }

        [Test]
        public void Load_V2File_KeepsAiGamesAsColourless()
        {
            // v0.3.0 y anteriores no guardaban de qué color iba el humano. Esas partidas no se
            // pierden: cuentan en los totales, pero no se pueden repartir entre Rojo y Azul.
            WriteFile(_repository.FilePath,
                "{\"version\":2,\"vsAi\":{\"hard\":{\"wins\":2,\"losses\":5,\"draws\":1}}," +
                "\"overall\":{\"gamesPlayed\":8,\"playerOneWins\":2,\"playerTwoWins\":5,\"draws\":1}}");

            var history = _repository.Load();

            Assert.AreEqual(8, history.VsAi.GetGamesPlayed(AiDifficulty.Hard));
            Assert.AreEqual(2, history.VsAi.GetWins(AiDifficulty.Hard));
            Assert.AreEqual(8, history.VsAi.ColorlessGames);
            Assert.AreEqual(0, history.VsAi.GetWins(AiDifficulty.Hard, Player.One));
            Assert.AreEqual(0, history.VsAi.GetWins(AiDifficulty.Hard, Player.Two));
            Assert.AreEqual(8, history.Overall.GamesPlayed, "El registro general no se toca.");
        }

        [Test]
        public void SaveThenLoad_RoundTripsTheColourOfEachAiGame()
        {
            var history = new MatchHistory();
            history.VsAi.RecordWin(AiDifficulty.Easy, Player.One);
            history.VsAi.RecordLoss(AiDifficulty.Easy, Player.Two);
            history.VsAi.RecordDraw(AiDifficulty.Hard, Player.Two);
            history.VsAi.Restore(AiDifficulty.Normal, Player.None, 1, 0, 0);

            _repository.Save(history);
            var loaded = _repository.Load();

            Assert.AreEqual(1, loaded.VsAi.GetWins(AiDifficulty.Easy, Player.One));
            Assert.AreEqual(0, loaded.VsAi.GetWins(AiDifficulty.Easy, Player.Two));
            Assert.AreEqual(1, loaded.VsAi.GetLosses(AiDifficulty.Easy, Player.Two));
            Assert.AreEqual(1, loaded.VsAi.GetDraws(AiDifficulty.Hard, Player.Two));
            Assert.AreEqual(1, loaded.VsAi.ColorlessGames, "Las partidas sin color se conservan.");
            Assert.AreEqual(4, loaded.VsAi.GamesPlayed);
        }

        [Test]
        public void Save_OverwritesPreviousFile()
        {
            var history = new MatchHistory();
            history.VsAi.RecordWin(AiDifficulty.Normal, Player.Two);
            _repository.Save(history);

            history.VsAi.RecordWin(AiDifficulty.Normal, Player.Two);
            _repository.Save(history);

            Assert.AreEqual(2, _repository.Load().VsAi.GetWins(AiDifficulty.Normal));
        }

        [Test]
        public void Save_ReplacesTheFileInOneStep()
        {
            // El reemplazo es atómico: nunca hay un instante sin histórico en el disco, y el
            // temporal no queda tirado. Borrar y volver a mover sí dejaba ese hueco.
            var history = new MatchHistory();
            history.Overall.RecordWin(Player.One);
            _repository.Save(history);

            history.Overall.RecordWin(Player.Two);
            _repository.Save(history);

            Assert.IsTrue(File.Exists(_repository.FilePath));
            Assert.IsFalse(File.Exists(_repository.FilePath + ".tmp"), "El temporal debe desaparecer.");
            Assert.AreEqual(2, _repository.Load().Overall.GamesPlayed);
        }

        [Test]
        public void Save_WithLeftoverTempFile_StillWorks()
        {
            // Un temporal de una caída anterior no debe impedir guardar.
            _repository.Save(new MatchHistory());
            WriteFile(_repository.FilePath + ".tmp", "sobras de un guardado a medias");

            var history = new MatchHistory();
            history.Overall.RecordDraw();
            _repository.Save(history);

            Assert.AreEqual(1, _repository.Load().Overall.Draws);
            Assert.IsFalse(File.Exists(_repository.FilePath + ".tmp"));
        }

        [Test]
        public void Load_V1File_GoesToOverallOnly()
        {
            WriteFile(_repository.FilePath, V1File);

            var history = _repository.Load();

            Assert.AreEqual(6, history.Overall.GamesPlayed);
            Assert.AreEqual(1, history.Overall.Draws);
            Assert.AreEqual(3, history.Overall.GetWins(Player.One));
            Assert.AreEqual(3, history.Overall.GetLosses(Player.Two));
            Assert.AreEqual(0, history.TwoPlayer.GamesPlayed);
            Assert.AreEqual(0, history.VsAi.GamesPlayed);
        }

        [Test]
        public void Load_V1FileBeforeDraws_LoadsWithZeroDraws()
        {
            WriteFile(_repository.FilePath,
                "{\"version\":1,\"gamesPlayed\":2,\"playerOneWins\":2,\"playerOneLosses\":0,\"playerTwoWins\":0,\"playerTwoLosses\":2}");

            var history = _repository.Load();

            Assert.AreEqual(2, history.Overall.GamesPlayed);
            Assert.AreEqual(0, history.Overall.Draws);
        }

        [Test]
        public void V1Data_SurvivesNewGames()
        {
            WriteFile(_repository.FilePath, V1File);
            var history = _repository.Load();
            history.VsAi.RecordWin(AiDifficulty.Easy, Player.One);

            _repository.Save(history);
            var reloaded = _repository.Load();

            Assert.AreEqual(6, reloaded.Overall.GamesPlayed);
            Assert.AreEqual(1, reloaded.VsAi.GamesPlayed);
        }

        [Test]
        public void DeletingOneSection_IsPersisted_AndKeepsTheOthers()
        {
            var history = new MatchHistory();
            HistoryRecorder.Record(history, new MatchSettings(true, AiDifficulty.Easy, Player.One), Player.One);
            HistoryRecorder.Record(history, new MatchSettings(false, AiDifficulty.Easy, Player.One), Player.Two);
            _repository.Save(history);

            // Igual que el menú: releer, borrar una sección y guardar.
            var loaded = _repository.Load();
            loaded.Clear(HistorySection.VsAi);
            _repository.Save(loaded);
            var reloaded = _repository.Load();

            Assert.AreEqual(0, reloaded.VsAi.GamesPlayed);
            Assert.AreEqual(1, reloaded.TwoPlayer.GamesPlayed);
            Assert.AreEqual(2, reloaded.Overall.GamesPlayed, "El registro general no cambia.");
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

            Assert.AreEqual(6, history.Overall.GamesPlayed, "Un archivo de v0.1.x va al registro general.");
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

            Assert.AreEqual(1, history.TwoPlayer.GamesPlayed);
            Assert.AreEqual(0, history.Overall.GamesPlayed, "No se leyó el archivo de la compañía anterior.");
        }

        [Test]
        public void Load_WithMissingOldCompanyFile_StartsEmpty()
        {
            var repository = new StatsRepository(
                Path.Combine(_directory, "nuevo", StatsRepository.DefaultFileName),
                Path.Combine(_directory, "no-existe", StatsRepository.DefaultFileName));

            Assert.AreEqual(0, repository.Load().Overall.GamesPlayed);
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

            foreach (HistorySection section in System.Enum.GetValues(typeof(HistorySection)))
                Assert.AreEqual(0, history.GetGamesPlayed(section));
        }

        private static void WriteFile(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
    }
}
