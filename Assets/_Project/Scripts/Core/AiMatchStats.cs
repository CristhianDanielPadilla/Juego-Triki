using System;

namespace Triki.Core
{
    /// <summary>
    /// Histórico de partidas contra la IA desde el punto de vista del jugador humano:
    /// victorias, derrotas y empates por dificultad <b>y por el color que llevaba el humano</b>.
    /// <para>
    /// El color <see cref="Player.None"/> guarda las partidas anteriores a la v0.4.0, que se
    /// anotaban sin color. Cuentan en los totales por dificultad, pero no se pueden repartir
    /// entre Rojo y Azul: por eso las dos columnas pueden no sumar el total (<see cref="ColorlessGames"/>).
    /// </para>
    /// </summary>
    public sealed class AiMatchStats
    {
        private const int DifficultyCount = (int)AiDifficulty.Hard + 1;

        // Segunda dimensión indexada por (int)Player; la posición 0 (None) son las partidas viejas.
        private const int ColorCount = 3;

        private readonly int[,] _wins = new int[DifficultyCount, ColorCount];
        private readonly int[,] _losses = new int[DifficultyCount, ColorCount];
        private readonly int[,] _draws = new int[DifficultyCount, ColorCount];

        public int GamesPlayed
        {
            get
            {
                var total = 0;
                for (var difficulty = 0; difficulty < DifficultyCount; difficulty++)
                    total += Total(_wins, difficulty) + Total(_losses, difficulty) + Total(_draws, difficulty);
                return total;
            }
        }

        /// <summary>Partidas guardadas sin saber de qué color iba el humano (anteriores a la v0.4.0).</summary>
        public int ColorlessGames
        {
            get
            {
                var total = 0;
                for (var difficulty = 0; difficulty < DifficultyCount; difficulty++)
                    total += _wins[difficulty, 0] + _losses[difficulty, 0] + _draws[difficulty, 0];
                return total;
            }
        }

        public int GetGamesPlayed(AiDifficulty difficulty)
        {
            var i = ToIndex(difficulty);
            return Total(_wins, i) + Total(_losses, i) + Total(_draws, i);
        }

        public int GetGamesPlayed(AiDifficulty difficulty, Player color) =>
            GetWins(difficulty, color) + GetLosses(difficulty, color) + GetDraws(difficulty, color);

        /// <summary>Victorias del humano en esa dificultad, con cualquier color.</summary>
        public int GetWins(AiDifficulty difficulty) => Total(_wins, ToIndex(difficulty));

        public int GetLosses(AiDifficulty difficulty) => Total(_losses, ToIndex(difficulty));

        public int GetDraws(AiDifficulty difficulty) => Total(_draws, ToIndex(difficulty));

        /// <param name="color">Color que llevaba el humano.</param>
        public int GetWins(AiDifficulty difficulty, Player color) => _wins[ToIndex(difficulty), ToIndex(color)];

        public int GetLosses(AiDifficulty difficulty, Player color) => _losses[ToIndex(difficulty), ToIndex(color)];

        public int GetDraws(AiDifficulty difficulty, Player color) => _draws[ToIndex(difficulty), ToIndex(color)];

        public void RecordWin(AiDifficulty difficulty, Player color) => _wins[ToIndex(difficulty), ToIndex(color)]++;

        public void RecordLoss(AiDifficulty difficulty, Player color) => _losses[ToIndex(difficulty), ToIndex(color)]++;

        public void RecordDraw(AiDifficulty difficulty, Player color) => _draws[ToIndex(difficulty), ToIndex(color)]++;

        /// <summary>Carga valores guardados. Rechaza negativos para no arrastrar datos corruptos.</summary>
        public void Restore(AiDifficulty difficulty, Player color, int wins, int losses, int draws)
        {
            if (wins < 0 || losses < 0 || draws < 0)
                throw new ArgumentOutOfRangeException(nameof(wins), "Las estadísticas no pueden ser negativas.");

            var i = ToIndex(difficulty);
            var c = ToIndex(color);
            _wins[i, c] = wins;
            _losses[i, c] = losses;
            _draws[i, c] = draws;
        }

        public void Clear()
        {
            Array.Clear(_wins, 0, _wins.Length);
            Array.Clear(_losses, 0, _losses.Length);
            Array.Clear(_draws, 0, _draws.Length);
        }

        private static int Total(int[,] counters, int difficulty)
        {
            var total = 0;
            for (var color = 0; color < ColorCount; color++)
                total += counters[difficulty, color];
            return total;
        }

        private static int ToIndex(AiDifficulty difficulty)
        {
            var i = (int)difficulty;
            if (i < 0 || i >= DifficultyCount)
                throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Dificultad desconocida.");
            return i;
        }

        private static int ToIndex(Player color)
        {
            var i = (int)color;
            if (i < 0 || i >= ColorCount)
                throw new ArgumentOutOfRangeException(nameof(color), color, "Color desconocido.");
            return i;
        }
    }
}
