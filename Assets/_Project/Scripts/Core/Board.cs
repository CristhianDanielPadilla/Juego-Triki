using System;

namespace Triki.Core
{
    /// <summary>
    /// Estado de ocupación de las casillas. Solo <see cref="TrikiGame"/> lo modifica,
    /// así las reglas no se pueden saltar desde fuera.
    /// </summary>
    public sealed class Board
    {
        private const int AllCellsMask = (1 << BoardGraph.CellCount) - 1;

        private readonly Player[] _cells = new Player[BoardGraph.CellCount];

        // Casillas de cada jugador como bits, indexado por (int)Player (0 sin uso).
        private readonly int[] _masks = new int[3];

        public Board(BoardGraph graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public BoardGraph Graph { get; }

        public Player this[int cell] => _cells[cell];

        public bool IsEmpty(int cell) => _cells[cell] == Player.None;

        /// <summary>
        /// Bit <c>i</c> encendido = la casilla <c>i</c> pertenece a <paramref name="player"/>.
        /// Con <see cref="Player.None"/> devuelve las casillas vacías.
        /// </summary>
        public int GetMask(Player player)
        {
            if (player == Player.None)
                return ~(_masks[(int)Player.One] | _masks[(int)Player.Two]) & AllCellsMask;
            return _masks[(int)player];
        }

        internal void Set(int cell, Player player)
        {
            var bit = 1 << cell;
            var previous = _cells[cell];
            if (previous != Player.None)
                _masks[(int)previous] &= ~bit;
            if (player != Player.None)
                _masks[(int)player] |= bit;
            _cells[cell] = player;
        }

        internal void Clear()
        {
            Array.Clear(_cells, 0, _cells.Length);
            Array.Clear(_masks, 0, _masks.Length);
        }
    }
}
