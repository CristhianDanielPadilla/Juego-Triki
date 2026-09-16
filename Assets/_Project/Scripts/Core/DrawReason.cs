namespace Triki.Core
{
    public enum DrawReason : byte
    {
        /// <summary>La misma posición se repitió <see cref="TrikiRules.RepetitionLimit"/> veces.</summary>
        Repetition,

        /// <summary>Se agotaron los <see cref="TrikiRules.MaxMovementMoves"/> movimientos sin ganador.</summary>
        MoveLimit,
    }
}
