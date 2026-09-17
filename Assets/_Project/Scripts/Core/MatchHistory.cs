namespace Triki.Core
{
    /// <summary>
    /// Histórico completo, separado por modo de juego.
    /// </summary>
    public sealed class MatchHistory
    {
        /// <summary>Partidas de dos jugadores en la misma PC, por color.</summary>
        public MatchStats TwoPlayer { get; } = new MatchStats();

        /// <summary>Partidas contra la IA, desde el punto de vista del humano.</summary>
        public AiMatchStats VsAi { get; } = new AiMatchStats();

        /// <summary>
        /// Partidas guardadas antes de separar por modo (v0.1.x): no se sabe si fueron contra la IA.
        /// Solo se leen; las partidas nuevas nunca se suman aquí.
        /// </summary>
        public MatchStats Legacy { get; } = new MatchStats();

        public bool HasLegacy => Legacy.GamesPlayed > 0;

        public int GamesPlayed => TwoPlayer.GamesPlayed + VsAi.GamesPlayed + Legacy.GamesPlayed;

        public void Clear()
        {
            TwoPlayer.Clear();
            VsAi.Clear();
            Legacy.Clear();
        }
    }
}
