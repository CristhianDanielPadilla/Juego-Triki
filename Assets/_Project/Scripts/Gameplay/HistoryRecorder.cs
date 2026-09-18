using System;
using Triki.Core;

namespace Triki.Gameplay
{
    /// <summary>
    /// Anota el resultado de una partida en el registro general y en el de su modo.
    /// </summary>
    public static class HistoryRecorder
    {
        /// <param name="winner"><see cref="Player.None"/> si fue empate.</param>
        public static void Record(MatchHistory history, MatchSettings settings, Player winner)
        {
            if (history == null)
                throw new ArgumentNullException(nameof(history));

            RecordByColor(history.Overall, winner);

            if (!settings.VsAi)
            {
                RecordByColor(history.TwoPlayer, winner);
                return;
            }

            // El color es el del humano: así el histórico puede decir cómo te va con cada uno.
            var color = settings.HumanPlayer;
            if (winner == Player.None)
                history.VsAi.RecordDraw(settings.Difficulty, color);
            else if (winner == color)
                history.VsAi.RecordWin(settings.Difficulty, color);
            else
                history.VsAi.RecordLoss(settings.Difficulty, color);
        }

        private static void RecordByColor(MatchStats stats, Player winner)
        {
            if (winner == Player.None)
                stats.RecordDraw();
            else
                stats.RecordWin(winner);
        }
    }
}
