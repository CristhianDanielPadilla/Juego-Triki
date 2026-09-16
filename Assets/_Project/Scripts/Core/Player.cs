namespace Triki.Core
{
    /// <summary>Dueño de una casilla o de un turno. <see cref="None"/> = casilla vacía.</summary>
    public enum Player : byte
    {
        None = 0,
        One = 1,
        Two = 2,
    }

    public static class PlayerExtensions
    {
        public static Player Opponent(this Player player)
        {
            switch (player)
            {
                case Player.One: return Player.Two;
                case Player.Two: return Player.One;
                default: return Player.None;
            }
        }
    }
}
