using System;
using System.IO;
using Triki.Core;
using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Guarda el histórico como JSON en <see cref="Application.persistentDataPath"/>.
    /// Si el archivo falta o está dañado se empieza de cero en vez de romper el juego.
    /// Si falta pero existe el de una compañía anterior (<see cref="LegacyCompanyName"/>), lo copia primero.
    /// </summary>
    public sealed class StatsRepository
    {
        public const string DefaultFileName = "triki-stats.json";

        /// <summary>Compañía con la que se publicó v0.1.0; su carpeta de datos era otra.</summary>
        public const string LegacyCompanyName = "DefaultCompany";

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

        public MatchStats Load()
        {
            var stats = new MatchStats();
            if (!File.Exists(FilePath))
                MigrateLegacyFile();
            if (!File.Exists(FilePath))
                return stats;

            try
            {
                var data = JsonUtility.FromJson<StatsData>(File.ReadAllText(FilePath));
                if (data == null)
                    throw new FormatException("Archivo vacío.");
                stats.Restore(data.gamesPlayed, data.draws, data.playerOneWins, data.playerOneLosses, data.playerTwoWins, data.playerTwoLosses);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is FormatException)
            {
                Debug.LogWarning($"No se pudo leer el histórico en '{FilePath}'; se empieza de cero. {e.Message}");
                stats.Clear();
            }

            return stats;
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

        public void Save(MatchStats stats)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            var data = new StatsData
            {
                gamesPlayed = stats.GamesPlayed,
                draws = stats.Draws,
                playerOneWins = stats.GetWins(Player.One),
                playerOneLosses = stats.GetLosses(Player.One),
                playerTwoWins = stats.GetWins(Player.Two),
                playerTwoLosses = stats.GetLosses(Player.Two),
            };

            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                // Se escribe a un temporal y se reemplaza: si el juego se cierra a mitad,
                // el archivo anterior sigue intacto.
                var tempPath = FilePath + ".tmp";
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                File.Move(tempPath, FilePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"No se pudo guardar el histórico en '{FilePath}'. {e.Message}");
            }
        }

        [Serializable]
        internal sealed class StatsData
        {
            // Para migrar el formato si algún día cambia.
            public int version = 1;
            public int gamesPlayed;

            // Añadido después de la versión 1: los archivos anteriores no lo tienen y cargan 0.
            public int draws;
            public int playerOneWins;
            public int playerOneLosses;
            public int playerTwoWins;
            public int playerTwoLosses;
        }
    }
}
