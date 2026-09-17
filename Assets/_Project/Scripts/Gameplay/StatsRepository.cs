using System;
using System.IO;
using Triki.Core;
using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Guarda el histórico (<see cref="MatchHistory"/>) como JSON en <see cref="Application.persistentDataPath"/>.
    /// Si el archivo falta o está dañado se empieza de cero en vez de romper el juego.
    /// Si falta pero existe el de una compañía anterior (<see cref="LegacyCompanyName"/>), lo copia primero.
    /// </summary>
    public sealed class StatsRepository
    {
        public const string DefaultFileName = "triki-stats.json";

        /// <summary>Compañía con la que se publicó v0.1.0; su carpeta de datos era otra.</summary>
        public const string LegacyCompanyName = "DefaultCompany";

        /// <summary>Formato del archivo: 1 = v0.1.x (sin modo de juego), 2 = separado por modo.</summary>
        private const int CurrentVersion = 2;

        public StatsRepository()
            : this(
                Path.Combine(Application.persistentDataPath, DefaultFileName),
                GetLegacyFilePath(Application.persistentDataPath, Application.companyName, LegacyCompanyName))
        {
        }

        /// <param name="filePath">Dónde se lee y escribe el histórico.</param>
        /// <param name="legacyFilePath">Histórico anterior a copiar si <paramref name="filePath"/> no existe; puede ser null.</param>
        public StatsRepository(string filePath, string legacyFilePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Ruta vacía.", nameof(filePath));
            FilePath = filePath;
            LegacyFilePath = legacyFilePath;
        }

        public string FilePath { get; }

        public string LegacyFilePath { get; }

        /// <summary>
        /// Ruta del histórico bajo otra compañía. Solo aplica si <paramref name="persistentDataPath"/>
        /// tiene la forma <c>…/&lt;compañía&gt;/&lt;producto&gt;</c> (Windows y editor); si no, devuelve null.
        /// </summary>
        public static string GetLegacyFilePath(string persistentDataPath, string companyName, string legacyCompanyName)
        {
            if (string.IsNullOrEmpty(persistentDataPath) || string.IsNullOrEmpty(companyName)
                || string.IsNullOrEmpty(legacyCompanyName) || companyName == legacyCompanyName)
                return null;

            var productDirectory = new DirectoryInfo(persistentDataPath);
            var companyDirectory = productDirectory.Parent;
            if (companyDirectory?.Parent == null || companyDirectory.Name != companyName)
                return null;

            return Path.Combine(companyDirectory.Parent.FullName, legacyCompanyName, productDirectory.Name, DefaultFileName);
        }

        public MatchHistory Load()
        {
            var history = new MatchHistory();
            if (!File.Exists(FilePath))
                MigrateLegacyFile();
            if (!File.Exists(FilePath))
                return history;

            try
            {
                var data = JsonUtility.FromJson<HistoryData>(File.ReadAllText(FilePath));
                if (data == null)
                    throw new FormatException("Archivo vacío.");

                if (data.version < CurrentVersion)
                {
                    // v1 (v0.1.x): campos planos sin modo de juego -> solo al registro general.
                    history.Overall.Restore(data.gamesPlayed, data.draws, data.playerOneWins, data.playerOneLosses, data.playerTwoWins, data.playerTwoLosses);
                }
                else
                {
                    Restore(history.TwoPlayer, data.twoPlayer);
                    Restore(history.Overall, data.overall);
                    Restore(history.VsAi, AiDifficulty.Easy, data.vsAi?.easy);
                    Restore(history.VsAi, AiDifficulty.Normal, data.vsAi?.normal);
                    Restore(history.VsAi, AiDifficulty.Hard, data.vsAi?.hard);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is FormatException)
            {
                Debug.LogWarning($"No se pudo leer el histórico en '{FilePath}'; se empieza de cero. {e.Message}");
                history.ClearAll();
            }

            return history;
        }

        /// <summary>Copia (no mueve) el histórico anterior: el original queda como respaldo.</summary>
        private void MigrateLegacyFile()
        {
            if (string.IsNullOrEmpty(LegacyFilePath) || !File.Exists(LegacyFilePath))
                return;

            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.Copy(LegacyFilePath, FilePath, false);
                Debug.Log($"Histórico copiado desde '{LegacyFilePath}'.");
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"No se pudo copiar el histórico anterior desde '{LegacyFilePath}'. {e.Message}");
            }
        }

        public void Save(MatchHistory history)
        {
            if (history == null)
                throw new ArgumentNullException(nameof(history));

            var data = new HistoryData
            {
                version = CurrentVersion,
                twoPlayer = ToData(history.TwoPlayer),
                overall = ToData(history.Overall),
                vsAi = new AiStatsData
                {
                    easy = ToData(history.VsAi, AiDifficulty.Easy),
                    normal = ToData(history.VsAi, AiDifficulty.Normal),
                    hard = ToData(history.VsAi, AiDifficulty.Hard),
                },
            };

            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                // Se escribe a un temporal y se reemplaza de golpe: si el juego se cierra a mitad,
                // el archivo anterior sigue intacto. File.Replace es atómico; borrar y mover no lo
                // era (entre las dos operaciones no existía ningún histórico).
                var tempPath = FilePath + ".tmp";
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath))
                    File.Replace(tempPath, FilePath, null);
                else
                    File.Move(tempPath, FilePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"No se pudo guardar el histórico en '{FilePath}'. {e.Message}");
            }
        }

        private static void Restore(MatchStats stats, ColorStatsData data)
        {
            if (data != null)
                stats.Restore(data.gamesPlayed, data.draws, data.playerOneWins, data.playerOneLosses, data.playerTwoWins, data.playerTwoLosses);
        }

        private static void Restore(AiMatchStats stats, AiDifficulty difficulty, ResultData data)
        {
            if (data != null)
                stats.Restore(difficulty, data.wins, data.losses, data.draws);
        }

        private static ColorStatsData ToData(MatchStats stats)
        {
            return new ColorStatsData
            {
                gamesPlayed = stats.GamesPlayed,
                draws = stats.Draws,
                playerOneWins = stats.GetWins(Player.One),
                playerOneLosses = stats.GetLosses(Player.One),
                playerTwoWins = stats.GetWins(Player.Two),
                playerTwoLosses = stats.GetLosses(Player.Two),
            };
        }

        private static ResultData ToData(AiMatchStats stats, AiDifficulty difficulty)
        {
            return new ResultData
            {
                wins = stats.GetWins(difficulty),
                losses = stats.GetLosses(difficulty),
                draws = stats.GetDraws(difficulty),
            };
        }

        // Formato del archivo. v1 (v0.1.x) solo tenía los campos planos de abajo; v2 separa por modo.
        // Un archivo sin "version" se trata como v1.
        [Serializable]
        internal sealed class HistoryData
        {
            public int version;
            public ColorStatsData twoPlayer;
            public AiStatsData vsAi;
            public ColorStatsData overall;

            // Solo v1 (lectura).
            public int gamesPlayed;
            public int draws;
            public int playerOneWins;
            public int playerOneLosses;
            public int playerTwoWins;
            public int playerTwoLosses;
        }

        [Serializable]
        internal sealed class ColorStatsData
        {
            public int gamesPlayed;
            public int draws;
            public int playerOneWins;
            public int playerOneLosses;
            public int playerTwoWins;
            public int playerTwoLosses;
        }

        [Serializable]
        internal sealed class AiStatsData
        {
            public ResultData easy;
            public ResultData normal;
            public ResultData hard;
        }

        [Serializable]
        internal sealed class ResultData
        {
            public int wins;
            public int losses;
            public int draws;
        }
    }
}
