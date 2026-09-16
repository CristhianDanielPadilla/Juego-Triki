namespace Triki.Core
{
    public enum GamePhase : byte
    {
        /// <summary>Los jugadores colocan sus fichas por turnos hasta agotar las de su mano.</summary>
        Placement,

        /// <summary>Todas las fichas están en el tablero; se mueven por las aristas (pendiente).</summary>
        Movement,
    }
}
