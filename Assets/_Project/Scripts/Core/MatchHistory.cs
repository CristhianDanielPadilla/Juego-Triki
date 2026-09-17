using System;

namespace Triki.Core
{
    /// <summary>
    /// Histórico completo. Cada partida se anota en <see cref="Overall"/> y en la sección de su modo.
    /// Los registros son independientes: borrar uno no modifica los demás.
    /// </summary>
    public sealed class MatchHistory
    {
        /// <summary>
        /// Todas las partidas, por color. Incluye las guardadas antes de separar por modo (v0.1.x).
        /// </summary>
        public MatchStats Overall { get; } = new MatchStats();

        /// <summary>Partidas contra la IA, desde el punto de vista del humano.</summary>
        public AiMatchStats VsAi { get; } = new AiMatchStats();

        /// <summary>Partidas de dos jugadores en la misma PC, por color.</summary>
        public MatchStats TwoPlayer { get; } = new MatchStats();

        public int GetGamesPlayed(HistorySection section)
        {
            switch (section)
            {
                case HistorySection.Overall: return Overall.GamesPlayed;
                case HistorySection.VsAi: return VsAi.GamesPlayed;
                case HistorySection.TwoPlayer: return TwoPlayer.GamesPlayed;
                default: throw new ArgumentOutOfRangeException(nameof(section), section, "Sección desconocida.");
            }
        }

        /// <summary>Borra solo el registro indicado.</summary>
        public void Clear(HistorySection section)
        {
            switch (section)
            {
                case HistorySection.Overall: Overall.Clear(); break;
                case HistorySection.VsAi: VsAi.Clear(); break;
                case HistorySection.TwoPlayer: TwoPlayer.Clear(); break;
                default: throw new ArgumentOutOfRangeException(nameof(section), section, "Sección desconocida.");
            }
        }

        /// <summary>Ningún registro tiene partidas.</summary>
        public bool IsEmpty => Overall.GamesPlayed == 0 && VsAi.GamesPlayed == 0 && TwoPlayer.GamesPlayed == 0;

        /// <summary>Borra los tres registros.</summary>
        public void ClearAll()
        {
            Overall.Clear();
            VsAi.Clear();
            TwoPlayer.Clear();
        }
    }
}
