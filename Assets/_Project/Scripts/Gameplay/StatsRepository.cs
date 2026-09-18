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

        /// <summary>
        /// Formato del archivo: 1 = v0.1.x (sin modo de juego), 2 = separado por modo,
        /// 3 = las partidas contra la IA guardan ademas el color del humano.
        /// </summary>
        private const int CurrentVersion = 3;

        /// <summary>Formato que empezó a separar por modo de juego.</summary>
        private const int ByModeVersion = 2;

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

                if (data.version < ByModeVersion)
                {
                    // v1 (v0.1.x): campos planos sin modo de juego -> solo al registro general.
                    history.Overall.Restore(data.gamesPlayed, data.draws, data.playerOneWins, data.playerOneLosses, data.playerTwoWins, data.playerTwoLosses);
                }
                else
                {
                    Restore(history.TwoPlayer, data.twoPlayer);
                    Restore(history.Overall, data.overall);

                    if (data.version < CurrentVersion)
                    {
                        // v2: la partida contra la IA no guardaba el color del humano. Se conservan
                        // los totales en el hueco "sin color", que cuenta pero no se reparte.
                        RestoreColorless(history.VsAi, AiDifficulty.Easy, data.vsAi?.easy);
                        RestoreColorless(history.VsAi, AiDifficulty.Normal, data.vsAi?.normal);
                        RestoreColorless(history.VsAi, AiDifficulty.Hard, data.vsAi?.hard);
                    }
                    else
                    {
                        Restore(history.VsAi, AiDifficulty.Easy, data.vsAiByColor?.easy);
                        Restore(history.VsAi, AiDifficulty.Normal, data.vsAiByColor?.normal);
                        Restore(history.VsAi, AiDifficulty.Hard, data.vsAiByColor?.hard);
                    }
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

                // Totales por dificultad, sin color. No los lee esta version: estan para que
                // v0.2.x y v0.3.0 sigan mostrando el registro contra la IA si abren el archivo.
                vsAi = new AiStatsData
                {
                    easy = ToTotals(history.VsAi, AiDifficulty.Easy),
                    normal = ToTotals(history.VsAi, AiDifficulty.Normal),
                    hard = ToTotals(history.VsAi, AiDifficulty.Hard),
                },
                vsAiByColor = new AiColorStatsData
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

        private static void Restore(AiMatchStats stats, AiDifficulty difficulty, ByColorData data)
        {
            if (data == null)
                return;
            RestoreColor(stats, difficulty, Player.One, data.red);
            RestoreColor(stats, difficulty, Player.Two, data.blue);
            RestoreColor(stats, difficulty, Player.None, data.colorless);
        }

        private static void RestoreColor(AiMatchStats stats, AiDifficulty difficulty, Player color, ResultData data)
        {
            if (data != null)
                stats.Restore(difficulty, color, data.wins, data.losses, data.draws);
        }

        /// <summary>Un archivo v2 no sabía de qué color iba el humano: todo al hueco sin color.</summary>
        private static void RestoreColorless(AiMatchStats stats, AiDifficulty difficulty, ResultData data)
        {
            if (data != null)
                stats.Restore(difficulty, Player.None, data.wins, data.losses, data.draws);
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

        private static ByColorData ToData(AiMatchStats stats, AiDifficulty difficulty)
        {
            return new ByColorData
            {
                red = ToData(stats, difficulty, Player.One),
                blue = ToData(stats, difficulty, Player.Two),
                colorless = ToData(stats, difficulty, Player.None),
            };
        }

        private static ResultData ToData(AiMatchStats stats, AiDifficulty difficulty, Player color)
        {
            return new ResultData
            {
                wins = stats.GetWins(difficulty, color),
                losses = stats.GetLosses(difficulty, color),
                draws = stats.GetDraws(difficulty, color),
            };
        }

        private static ResultData ToTotals(AiMatchStats stats, AiDifficulty difficulty)
        {
            return new ResultData
            {
                wins = stats.GetWins(difficulty),
                losses = stats.GetLosses(difficulty),
                draws = stats.GetDraws(difficulty),
            };
        }

        // Formato del archivo. v1 (v0.1.x) solo tenía los campos planos de abajo; v2 separa por modo;
        // v3 añade el color del humano en las partidas contra la IA.
        // Un archivo sin "version" se trata como v1.
        // Cada version conserva los campos de la anterior: los viejos se leen al migrar, y se siguen
        // escribiendo para que una version anterior del juego no se quede sin datos al abrir el archivo.
        [Serializable]
        internal sealed class HistoryData
        {
            public int version;
            public ColorStatsData twoPlayer;
            public AiColorStatsData vsAiByColor;
            public ColorStatsData overall;

            // v2 (lectura al migrar; escritura solo para versiones anteriores del juego).
            public AiStatsData vsAi;

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

        /// <summary>v2: un solo resultado por dificultad, sin color.</summary>
        [Serializable]
        internal sealed class AiStatsData
        {
            public ResultData easy;
            public ResultData normal;
            public ResultData hard;
        }

        /// <summary>v3: cada dificultad, repartida por el color que llevaba el humano.</summary>
        [Serializable]
        internal sealed class AiColorStatsData
        {
            public ByColorData easy;
            public ByColorData normal;
            public ByColorData hard;
        }

        [Serializable]
        internal sealed class ByColorData
        {
            public ResultData red;
            public ResultData blue;

            /// <summary>Partidas heredadas de v2, sin color conocido.</summary>
            public ResultData colorless;
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
