using System;

namespace Triki.Core
{
    /// <summary>Parámetros ajustables de una partida. Inmutable.</summary>
    public sealed class TrikiRules
    {
        public const int DefaultMaxMovementMoves = 60;
        public const int DefaultRepetitionLimit = 3;

        public TrikiRules(int maxMovementMoves = DefaultMaxMovementMoves, int repetitionLimit = DefaultRepetitionLimit)
        {
            if (maxMovementMoves < 1)
                throw new ArgumentOutOfRangeException(nameof(maxMovementMoves), maxMovementMoves, "Debe permitir al menos un movimiento.");
            if (repetitionLimit < 2)
                throw new ArgumentOutOfRangeException(nameof(repetitionLimit), repetitionLimit, "Una posición debe poder repetirse al menos una vez.");

            MaxMovementMoves = maxMovementMoves;
            RepetitionLimit = repetitionLimit;
        }

        public static TrikiRules Default { get; } = new TrikiRules();

        /// <summary>Movimientos totales (de ambos jugadores) en la fase de movimiento antes de declarar empate.</summary>
        public int MaxMovementMoves { get; }

        /// <summary>Veces que puede aparecer la misma posición, con el mismo jugador por mover, antes del empate.</summary>
        public int RepetitionLimit { get; }
    }
}
