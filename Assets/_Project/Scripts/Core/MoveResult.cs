namespace Triki.Core
{
    public enum MoveResult : byte
    {
        Moved,
        InvalidCell,
        WrongPhase,
        NotYourPiece,
        CellOccupied,
        NotAdjacent,
    }
}
