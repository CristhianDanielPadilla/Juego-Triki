using Triki.Core;

namespace Triki.UI
{
    /// <summary>Cómo se presenta cada jugador en la UI. Los colores viven en Triki.uss.</summary>
    public static class PlayerLabels
    {
        public const string PlayerOneClass = "player-one";
        public const string PlayerTwoClass = "player-two";

        public static string GetName(Player player)
        {
            switch (player)
            {
                case Player.One: return "Rojo";
                case Player.Two: return "Azul";
                default: return string.Empty;
            }
        }
    }
}
