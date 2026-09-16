using System;

namespace Triki.Core
{
    /// <summary>
    /// Estado de ocupación de las casillas. Solo <see cref="TrikiGame"/> lo modifica,
    /// así las reglas no se pueden saltar desde fuera.
    /// </summary>
    public sealed class Board
    {
        private readonly Player[] _cells = new Player[BoardGraph.CellCount];

        public Board(BoardGraph graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public BoardGraph Graph { get; }

        public Player this[int cell] => _cells[cell];

        public bool IsEmpty(int cell) => _cells[cell] == Player.None;

        internal void Set(int cell, Player player) => _cells[cell] = player;

        internal void Clear() => Array.Clear(_cells, 0, _cells.Length);
    }
}
