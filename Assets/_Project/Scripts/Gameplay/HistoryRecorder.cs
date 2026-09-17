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

            if (winner == Player.None)
                history.VsAi.RecordDraw(settings.Difficulty);
            else if (winner == settings.HumanPlayer)
                history.VsAi.RecordWin(settings.Difficulty);
            else
                history.VsAi.RecordLoss(settings.Difficulty);
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
