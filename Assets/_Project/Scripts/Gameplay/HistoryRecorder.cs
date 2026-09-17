using System;
using Triki.Core;

namespace Triki.Gameplay
{
    /// <summary>Anota el resultado de una partida en la sección del histórico que le corresponde.</summary>
    public static class HistoryRecorder
    {
        /// <param name="winner"><see cref="Player.None"/> si fue empate.</param>
        public static void Record(MatchHistory history, MatchSettings settings, Player winner)
        {
            if (history == null)
                throw new ArgumentNullException(nameof(history));

            if (!settings.VsAi)
            {
                if (winner == Player.None)
                    history.TwoPlayer.RecordDraw();
                else
                    history.TwoPlayer.RecordWin(winner);
                return;
            }

            if (winner == Player.None)
                history.VsAi.RecordDraw(settings.Difficulty);
            else if (winner == settings.HumanPlayer)
                history.VsAi.RecordWin(settings.Difficulty);
            else
                history.VsAi.RecordLoss(settings.Difficulty);
        }
    }
}
