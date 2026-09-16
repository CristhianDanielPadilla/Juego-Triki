using System;

namespace Triki.Core
{
    /// <summary>Tres casillas en línea recta (fila, columna o diagonal), de un extremo al otro.</summary>
    public readonly struct BoardLine : IEquatable<BoardLine>
    {
        public const int StraightLineCount = 8;

        private static readonly BoardLine[] _straightLines =
        {
            // Filas
            new BoardLine(0, 1, 2), new BoardLine(3, 4, 5), new BoardLine(6, 7, 8),
            // Columnas
            new BoardLine(0, 3, 6), new BoardLine(1, 4, 7), new BoardLine(2, 5, 8),
            // Diagonales
            new BoardLine(0, 4, 8), new BoardLine(2, 4, 6),
        };

        public BoardLine(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
            Mask = (1 << a) | (1 << b) | (1 << c);
        }

        /// <summary>Extremo inicial.</summary>
        public int A { get; }

        /// <summary>Casilla central de la línea.</summary>
        public int B { get; }

        /// <summary>Extremo final.</summary>
        public int C { get; }

        /// <summary>Bits de las tres casillas, comparable con <see cref="Board.GetMask"/>.</summary>
        public int Mask { get; }

        /// <summary>Las 8 rectas geométricas de un tablero 3x3.</summary>
        public static BoardLine GetStraightLine(int index) => _straightLines[index];

        public bool Equals(BoardLine other) => A == other.A && B == other.B && C == other.C;

        public override bool Equals(object obj) => obj is BoardLine other && Equals(other);

        public override int GetHashCode() => (A * 81) + (B * 9) + C;

        public override string ToString() => $"({A}, {B}, {C})";

        public static bool operator ==(BoardLine left, BoardLine right) => left.Equals(right);

        public static bool operator !=(BoardLine left, BoardLine right) => !left.Equals(right);
    }
}
