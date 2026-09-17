using System;

namespace Triki.Core
{
    /// <summary>Parámetros ajustables de una partida. Inmutable.</summary>
    public sealed class TrikiRules
    {
        public const int DefaultMaxMovementMoves = 60;
        public const int DefaultRepetitionLimit = 3;
        public const bool DefaultBanCenterOpening = true;

        public TrikiRules(
            int maxMovementMoves = DefaultMaxMovementMoves,
            int repetitionLimit = DefaultRepetitionLimit,
            bool banCenterOpening = DefaultBanCenterOpening)
        {
            if (maxMovementMoves < 1)
                throw new ArgumentOutOfRangeException(nameof(maxMovementMoves), maxMovementMoves, "Debe permitir al menos un movimiento.");
            if (repetitionLimit < 2)
                throw new ArgumentOutOfRangeException(nameof(repetitionLimit), repetitionLimit, "Una posición debe poder repetirse al menos una vez.");

            MaxMovementMoves = maxMovementMoves;
            RepetitionLimit = repetitionLimit;
            BanCenterOpening = banCenterOpening;
        }

        public static TrikiRules Default { get; } = new TrikiRules();

        /// <summary>Movimientos totales (de ambos jugadores) en la fase de movimiento antes de declarar empate.</summary>
        public int MaxMovementMoves { get; }

        /// <summary>Veces que puede aparecer la misma posición, con el mismo jugador por mover, antes del empate.</summary>
        public int RepetitionLimit { get; }

        /// <summary>
        /// Prohíbe poner la primera ficha de la partida en <see cref="BoardGraph.CenterCell"/>.
        /// Sin esta regla el juego está resuelto: quien empieza toma el centro y gana siempre.
        /// Con ella, el juego perfecto acaba en tablas. Es la regla de casa habitual de esta variante.
        /// Apagarla devuelve el comportamiento de v0.2.x.
        /// </summary>
        public bool BanCenterOpening { get; }
    }
}
