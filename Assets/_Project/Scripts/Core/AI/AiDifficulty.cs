namespace Triki.Core
{
    public enum AiDifficulty : byte
    {
        /// <summary>La mitad de las veces juega al azar; el resto solo mira su propia jugada.</summary>
        Easy,

        /// <summary>Mira 3 jugadas adelante: gana si puede y bloquea amenazas directas.</summary>
        Normal,

        /// <summary>Mira 8 jugadas adelante.</summary>
        Hard,
    }
}
