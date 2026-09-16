namespace Triki.Core
{
    public enum GamePhase : byte
    {
        /// <summary>Los jugadores colocan sus fichas por turnos hasta agotar las de su mano.</summary>
        Placement,

        /// <summary>Todas las fichas están en el tablero; cada turno se mueve una por una arista.</summary>
        Movement,

        /// <summary>Hay ganador. Solo se sale con <see cref="TrikiGame.Reset"/>.</summary>
        GameOver,
    }
}
