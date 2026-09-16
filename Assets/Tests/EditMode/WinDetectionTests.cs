using System.Collections.Generic;
using NUnit.Framework;
using Triki.Core;

namespace Triki.Tests
{
    public class WinDetectionTests
    {
        private TrikiGame _game;

        [SetUp]
        public void SetUp() => _game = new TrikiGame();

        [Test]
        public void SquareGraph_HasAllEightLines() => Assert.AreEqual(8, _game.WinLineCount);

        // Jugadas alternas empezando por One; el último movimiento de One completa la línea.
        [TestCase(new[] { 0, 3, 1, 4, 2 }, 0, 1, 2, TestName = "Fila superior")]
        [TestCase(new[] { 6, 0, 7, 1, 8 }, 6, 7, 8, TestName = "Fila inferior")]
        [TestCase(new[] { 1, 0, 4, 2, 7 }, 1, 4, 7, TestName = "Columna central")]
        [TestCase(new[] { 0, 1, 4, 2, 8 }, 0, 4, 8, TestName = "Diagonal principal")]
        [TestCase(new[] { 2, 0, 4, 1, 6 }, 2, 4, 6, TestName = "Diagonal secundaria")]
        public void PlayerOne_WinsDuringPlacement(int[] moves, int a, int b, int c)
        {
            Play(moves);

            Assert.AreEqual(GamePhase.GameOver, _game.Phase);
            Assert.AreEqual(Player.One, _game.Winner);
            Assert.AreEqual(new BoardLine(a, b, c), _game.WinningLine);
        }

        [Test]
        public void PlayerTwo_CanWinWithTheLastPiece()
        {
            // One: 0, 1, 7 (sin línea). Two: 2, 5, 8 (columna derecha).
            Play(0, 2, 1, 5, 7, 8);

            Assert.AreEqual(GamePhase.GameOver, _game.Phase, "La victoria tiene prioridad sobre pasar a movimiento.");
            Assert.AreEqual(Player.Two, _game.Winner);
            Assert.AreEqual(new BoardLine(2, 5, 8), _game.WinningLine);
        }

        [Test]
        public void OpponentPieces_DoNotCountForTheLine()
        {
            // Fila 0-1-2 llena pero mezclada.
            Play(0, 1, 2);

            Assert.AreEqual(Player.None, _game.Winner);
            Assert.AreEqual(GamePhase.Placement, _game.Phase);
        }

        [Test]
        public void SixPiecesWithoutLine_GoToMovement_WithoutWinner()
        {
            // One: 0, 2, 5. Two: 1, 3, 6.
            Play(0, 1, 2, 3, 5, 6);

            Assert.AreEqual(GamePhase.Movement, _game.Phase);
            Assert.AreEqual(Player.None, _game.Winner);
        }

        [Test]
        public void WinningMove_EmitsPlacedPhaseAndWon_ButNoTurnChange()
        {
            Play(0, 3, 1, 4);
            var log = new List<string>();
            _game.PiecePlaced += (cell, player) => log.Add($"placed {cell} {player}");
            _game.TurnChanged += player => log.Add($"turn {player}");
            _game.PhaseChanged += phase => log.Add($"phase {phase}");
            _game.GameWon += (player, line) => log.Add($"won {player} {line}");

            _game.TryPlace(2);

            CollectionAssert.AreEqual(
                new[] { "placed 2 One", "phase GameOver", "won One (0, 1, 2)" },
                log);
            Assert.AreEqual(Player.One, _game.CurrentPlayer, "Al terminar se conserva al ganador.");
        }

        [Test]
        public void AfterWin_PlacementIsRejected()
        {
            Play(0, 3, 1, 4, 2);

            Assert.AreEqual(PlaceResult.WrongPhase, _game.TryPlace(8));
            Assert.IsTrue(_game.Board.IsEmpty(8));
        }

        [Test]
        public void Reset_ClearsWinner()
        {
            Play(0, 3, 1, 4, 2);

            _game.Reset();

            Assert.AreEqual(Player.None, _game.Winner);
            Assert.AreEqual(GamePhase.Placement, _game.Phase);
            Assert.AreEqual(PlaceResult.Placed, _game.TryPlace(0));
        }

        [Test]
        public void WithoutDiagonalEdges_DiagonalAlignment_DoesNotWin()
        {
            var orthogonalOnly = BoardGraph.FromEdges(
                (0, 1), (1, 2), (3, 4), (4, 5), (6, 7), (7, 8),
                (0, 3), (3, 6), (1, 4), (4, 7), (2, 5), (5, 8));
            _game = new TrikiGame(orthogonalOnly);

            Play(0, 1, 4, 2, 8);

            Assert.AreEqual(6, _game.WinLineCount);
            Assert.AreEqual(Player.None, _game.Winner);
            Assert.AreEqual(GamePhase.Placement, _game.Phase);
        }

        private void Play(params int[] cells)
        {
            foreach (var cell in cells)
                Assert.AreEqual(PlaceResult.Placed, _game.TryPlace(cell), $"casilla {cell}");
        }
    }
}
