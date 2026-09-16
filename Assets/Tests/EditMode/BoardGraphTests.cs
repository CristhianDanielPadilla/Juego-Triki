using System;
using NUnit.Framework;
using Triki.Core;

namespace Triki.Tests
{
    public class BoardGraphTests
    {
        private BoardGraph _graph;

        [SetUp]
        public void SetUp() => _graph = BoardGraph.CreateSquare();

        [Test]
        public void Square_Has16Edges() => Assert.AreEqual(16, _graph.EdgeCount);

        [Test]
        public void Center_ConnectsToEveryOtherCell()
        {
            Assert.AreEqual(8, _graph.GetDegree(4));
            for (var cell = 0; cell < BoardGraph.CellCount; cell++)
            {
                if (cell != 4)
                    Assert.IsTrue(_graph.AreAdjacent(4, cell), $"4-{cell}");
            }
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(5), TestCase(6), TestCase(7), TestCase(8)]
        public void OuterCells_HaveThreeNeighbors(int cell) => Assert.AreEqual(3, _graph.GetDegree(cell));

        [TestCase(0, 1), TestCase(0, 3), TestCase(0, 4), TestCase(2, 4), TestCase(7, 8)]
        public void ConnectedPairs_AreAdjacent(int a, int b) => Assert.IsTrue(_graph.AreAdjacent(a, b));

        // Sin diagonales cortas (1-3, 1-5...) ni saltos sobre una casilla (0-2, 0-8).
        [TestCase(1, 3), TestCase(1, 5), TestCase(3, 7), TestCase(5, 7), TestCase(0, 2), TestCase(0, 8), TestCase(0, 6)]
        public void UnconnectedPairs_AreNotAdjacent(int a, int b) => Assert.IsFalse(_graph.AreAdjacent(a, b));

        [Test]
        public void Adjacency_IsSymmetric_AndHasNoSelfLoops()
        {
            for (var a = 0; a < BoardGraph.CellCount; a++)
            {
                Assert.IsFalse(_graph.AreAdjacent(a, a), $"lazo en {a}");
                for (var b = 0; b < BoardGraph.CellCount; b++)
                    Assert.AreEqual(_graph.AreAdjacent(a, b), _graph.AreAdjacent(b, a), $"{a}-{b}");
            }
        }

        [Test]
        public void Edges_MatchAdjacency()
        {
            for (var i = 0; i < _graph.EdgeCount; i++)
            {
                _graph.GetEdge(i, out var a, out var b);
                Assert.IsTrue(_graph.AreAdjacent(a, b), $"arista {i}: {a}-{b}");
            }
        }

        [TestCase(-1, 0), TestCase(0, 9)]
        public void AreAdjacent_OutOfRange_ReturnsFalse(int a, int b) => Assert.IsFalse(_graph.AreAdjacent(a, b));

        [Test]
        public void FromEdges_RejectsInvalidInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardGraph.FromEdges((0, 9)));
            Assert.Throws<ArgumentException>(() => BoardGraph.FromEdges((3, 3)));
            Assert.Throws<ArgumentException>(() => BoardGraph.FromEdges((0, 1), (1, 0)));
        }

        [Test]
        public void RowColumn_RoundTrip()
        {
            for (var cell = 0; cell < BoardGraph.CellCount; cell++)
                Assert.AreEqual(cell, BoardGraph.ToCell(BoardGraph.GetRow(cell), BoardGraph.GetColumn(cell)));
        }
    }
}
