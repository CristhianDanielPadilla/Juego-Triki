namespace Triki.Core
{
    public enum PlaceResult : byte
    {
        Placed,
        InvalidCell,
        CellOccupied,
        WrongPhase,

        /// <summary>La primera ficha de la partida no puede ir al centro (ver <see cref="TrikiRules"/>).</summary>
        ForbiddenOpening,
    }
}
