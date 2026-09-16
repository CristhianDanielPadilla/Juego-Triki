using System;

namespace Triki.Core
{
    /// <summary>
    /// Histórico acumulado de partidas terminadas: total jugado, empates y victorias/derrotas
    /// de cada jugador. Solo cuenta partidas con resultado; las abandonadas no se registran.
    /// </summary>
    public sealed class MatchStats
    {
        // Indexado por (int)Player; la posición 0 (None) no se usa.
        private readonly int[] _wins = new int[3];
        private readonly int[] _losses = new int[3];

        public int GamesPlayed { get; private set; }

        public int Draws { get; private set; }

        public int GetWins(Player player) => _wins[ToIndex(player)];

        public int GetLosses(Player player) => _losses[ToIndex(player)];

        public void RecordWin(Player winner)
        {
            var index = ToIndex(winner);
            GamesPlayed++;
            _wins[index]++;
            _losses[(int)winner.Opponent()]++;
        }

        public void RecordDraw()
        {
            GamesPlayed++;
            Draws++;
        }

        /// <summary>Carga valores guardados. Rechaza negativos para no arrastrar datos corruptos.</summary>
        public void Restore(int gamesPlayed, int draws, int playerOneWins, int playerOneLosses, int playerTwoWins, int playerTwoLosses)
        {
            if (gamesPlayed < 0 || draws < 0 || playerOneWins < 0 || playerOneLosses < 0 || playerTwoWins < 0 || playerTwoLosses < 0)
                throw new ArgumentOutOfRangeException(nameof(gamesPlayed), "Las estadísticas no pueden ser negativas.");

            GamesPlayed = gamesPlayed;
            Draws = draws;
            _wins[(int)Player.One] = playerOneWins;
            _losses[(int)Player.One] = playerOneLosses;
            _wins[(int)Player.Two] = playerTwoWins;
            _losses[(int)Player.Two] = playerTwoLosses;
        }

        public void Clear() => Restore(0, 0, 0, 0, 0, 0);

        private static int ToIndex(Player player)
        {
            if (player != Player.One && player != Player.Two)
                throw new ArgumentOutOfRangeException(nameof(player), player, "Se esperaba Player.One o Player.Two.");
            return (int)player;
        }
    }
}
