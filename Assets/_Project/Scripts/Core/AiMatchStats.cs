using System;

namespace Triki.Core
{
    /// <summary>
    /// Histórico de partidas contra la IA desde el punto de vista del jugador humano:
    /// victorias, derrotas y empates por dificultad.
    /// </summary>
    public sealed class AiMatchStats
    {
        private const int DifficultyCount = (int)AiDifficulty.Hard + 1;

        private readonly int[] _wins = new int[DifficultyCount];
        private readonly int[] _losses = new int[DifficultyCount];
        private readonly int[] _draws = new int[DifficultyCount];

        public int GamesPlayed
        {
            get
            {
                var total = 0;
                for (var i = 0; i < DifficultyCount; i++)
                    total += _wins[i] + _losses[i] + _draws[i];
                return total;
            }
        }

        public int GetGamesPlayed(AiDifficulty difficulty)
        {
            var i = ToIndex(difficulty);
            return _wins[i] + _losses[i] + _draws[i];
        }

        public int GetWins(AiDifficulty difficulty) => _wins[ToIndex(difficulty)];

        public int GetLosses(AiDifficulty difficulty) => _losses[ToIndex(difficulty)];

        public int GetDraws(AiDifficulty difficulty) => _draws[ToIndex(difficulty)];

        public void RecordWin(AiDifficulty difficulty) => _wins[ToIndex(difficulty)]++;

        public void RecordLoss(AiDifficulty difficulty) => _losses[ToIndex(difficulty)]++;

        public void RecordDraw(AiDifficulty difficulty) => _draws[ToIndex(difficulty)]++;

        /// <summary>Carga valores guardados. Rechaza negativos para no arrastrar datos corruptos.</summary>
        public void Restore(AiDifficulty difficulty, int wins, int losses, int draws)
        {
            if (wins < 0 || losses < 0 || draws < 0)
                throw new ArgumentOutOfRangeException(nameof(wins), "Las estadísticas no pueden ser negativas.");

            var i = ToIndex(difficulty);
            _wins[i] = wins;
            _losses[i] = losses;
            _draws[i] = draws;
        }

        public void Clear()
        {
            Array.Clear(_wins, 0, DifficultyCount);
            Array.Clear(_losses, 0, DifficultyCount);
            Array.Clear(_draws, 0, DifficultyCount);
        }

        private static int ToIndex(AiDifficulty difficulty)
        {
            var i = (int)difficulty;
            if (i < 0 || i >= DifficultyCount)
                throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Dificultad desconocida.");
            return i;
        }
    }
}
