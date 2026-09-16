using System;
using System.IO;
using Triki.Core;
using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Guarda el histórico como JSON en <see cref="Application.persistentDataPath"/>.
    /// Si el archivo falta o está dañado se empieza de cero en vez de romper el juego.
    /// </summary>
    public sealed class StatsRepository
    {
        public const string DefaultFileName = "triki-stats.json";

        public StatsRepository()
            : this(Path.Combine(Application.persistentDataPath, DefaultFileName))
        {
        }

        public StatsRepository(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Ruta vacía.", nameof(filePath));
            FilePath = filePath;
        }

        public string FilePath { get; }

        public MatchStats Load()
        {
            var stats = new MatchStats();
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
