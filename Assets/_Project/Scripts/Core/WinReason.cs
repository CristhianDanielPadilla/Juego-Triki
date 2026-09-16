namespace Triki.Core
{
    public enum WinReason : byte
    {
        /// <summary>El ganador alineó sus 3 fichas (<see cref="TrikiGame.WinningLine"/>).</summary>
        Line,

        /// <summary>Al rival le tocaba mover y no tenía ningún movimiento posible.</summary>
        OpponentBlocked,
    }
}
