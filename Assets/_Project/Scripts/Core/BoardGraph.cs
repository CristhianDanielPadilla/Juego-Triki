using System;

namespace Triki.Core
{
    /// <summary>
    /// Grafo inmutable de conexiones entre las casillas del tablero 3x3.
    /// Las casillas se indexan por filas:
    /// <code>
    /// 0 - 1 - 2
    /// | \ | / |
    /// 3 - 4 - 5
    /// | / | \ |
    /// 6 - 7 - 8
    /// </code>
    /// La adyacencia se guarda como máscara de bits por casilla: consultar si dos casillas
    /// están conectadas es O(1) y no asigna memoria.
    /// </summary>
    public sealed class BoardGraph
    {
        public const int Size = 3;
        public const int CellCount = Size * Size;

        private readonly ushort[] _neighborMasks = new ushort[CellCount];
        private readonly byte[] _edgeFrom;
        private readonly byte[] _edgeTo;

        private BoardGraph((int a, int b)[] edges)
        {
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));

            _edgeFrom = new byte[edges.Length];
            _edgeTo = new byte[edges.Length];

            for (var i = 0; i < edges.Length; i++)
            {
                var (a, b) = edges[i];

                if (!IsValidCell(a) || !IsValidCell(b))
                    throw new ArgumentOutOfRangeException(nameof(edges), $"Arista ({a}, {b}) fuera del tablero.");
                if (a == b)
                    throw new ArgumentException($"La casilla {a} no puede conectarse consigo misma.", nameof(edges));
                if ((_neighborMasks[a] & (1 << b)) != 0)
                    throw new ArgumentException($"Arista ({a}, {b}) duplicada.", nameof(edges));

                _neighborMasks[a] |= (ushort)(1 << b);
                _neighborMasks[b] |= (ushort)(1 << a);
                _edgeFrom[i] = (byte)a;
                _edgeTo[i] = (byte)b;
            }
        }

        /// <summary>
        /// Cuadrado con sus líneas medias y las dos diagonales que cruzan el centro:
        /// 12 aristas ortogonales + 4 diagonales. El centro conecta con las 8 casillas;
        /// esquinas y bordes conectan con 3.
        /// </summary>
        public static BoardGraph CreateSquare()
        {
            return FromEdges(
                // Horizontales
                (0, 1), (1, 2), (3, 4), (4, 5), (6, 7), (7, 8),
                // Verticales
                (0, 3), (3, 6), (1, 4), (4, 7), (2, 5), (5, 8),
                // Diagonales por el centro
                (0, 4), (4, 8), (2, 4), (4, 6));
        }

        /// <summary>Construye un grafo con aristas arbitrarias (no dirigidas).</summary>
        public static BoardGraph FromEdges(params (int a, int b)[] edges) => new BoardGraph(edges);

        public int EdgeCount => _edgeFrom.Length;

        public void GetEdge(int index, out int a, out int b)
        {
            a = _edgeFrom[index];
            b = _edgeTo[index];
        }

        public bool AreAdjacent(int a, int b)
        {
            return IsValidCell(a) && IsValidCell(b) && (_neighborMasks[a] & (1 << b)) != 0;
        }

        /// <summary>Bit <c>i</c> encendido = la casilla <c>i</c> es vecina de <paramref name="cell"/>.</summary>
        public int GetNeighborMask(int cell) => _neighborMasks[cell];

        public int GetDegree(int cell)
        {
            var mask = (int)_neighborMasks[cell];
            var count = 0;
            while (mask != 0)
            {
                mask &= mask - 1;
                count++;
            }
            return count;
        }

        public static bool IsValidCell(int cell) => (uint)cell < CellCount;

        public static int GetRow(int cell) => cell / Size;

        public static int GetColumn(int cell) => cell % Size;

        public static int ToCell(int row, int column) => row * Size + column;
    }
}
