using System.Collections.Generic;
using NUnit.Framework;
using Triki.Core;

namespace Triki.Tests
{
    public class MovementTests
    {
        private static readonly BoardGraph _orthogonalOnly = BoardGraph.FromEdges(
            (0, 1), (1, 2), (3, 4), (4, 5), (6, 7), (7, 8),
            (0, 3), (3, 6), (1, 4), (4, 7), (2, 5), (5, 8));

        private TrikiGame _game;

        // Tras esta colocación (sin líneas): One = 0, 2, 5 · Two = 1, 3, 6 · vacías = 4, 7, 8.
        // Le toca mover a One.
        [SetUp]
        public void SetUp()
        {
            _game = new TrikiGame();
            Place(0, 1, 2, 3, 5, 6);
            Assume.That(_game.Phase, Is.EqualTo(GamePhase.Movement));
        }

        [Test]
        public void TryMove_DuringPlacement_IsRejected()
        {
            var fresh = new TrikiGame();
            fresh.TryPlace(0);

            Assert.AreEqual(MoveResult.WrongPhase, fresh.TryMove(0, 1));
        }

        [Test]
        public void TryMove_AlongEdge_ToEmptyCell_MovesAndPassesTurn()
        {
            Assert.AreEqual(MoveResult.Moved, _game.TryMove(0, 4));

            Assert.IsTrue(_game.Board.IsEmpty(0));
            Assert.AreEqual(Player.One, _game.Board[4]);
            Assert.AreEqual((1 << 2) | (1 << 4) | (1 << 5), _game.Board.GetMask(Player.One));
            Assert.AreEqual(Player.Two, _game.CurrentPlayer);
            Assert.AreEqual(GamePhase.Movement, _game.Phase);
        }

        [TestCase(1, 4, MoveResult.NotYourPiece, TestName = "Ficha rival")]
        [TestCase(4, 7, MoveResult.NotYourPiece, TestName = "Casilla vacía como origen")]
        [TestCase(2, 1, MoveResult.CellOccupied, TestName = "Destino ocupado")]
        [TestCase(0, 8, MoveResult.NotAdjacent, TestName = "Sin arista")]
        [TestCase(5, 7, MoveResult.NotAdjacent, TestName = "Diagonal corta inexistente")]
        [TestCase(-1, 4, MoveResult.InvalidCell, TestName = "Origen fuera")]
        [TestCase(5, 9, MoveResult.InvalidCell, TestName = "Destino fuera")]
        public void TryMove_Invalid_IsRejected_WithoutChanges(int from, int to, MoveResult expected)
        {
            var one = _game.Board.GetMask(Player.One);
            var two = _game.Board.GetMask(Player.Two);

            Assert.AreEqual(expected, _game.TryMove(from, to));

            Assert.AreEqual(one, _game.Board.GetMask(Player.One));
            Assert.AreEqual(two, _game.Board.GetMask(Player.Two));
            Assert.AreEqual(Player.One, _game.CurrentPlayer);
        }

        [Test]
        public void Move_EmitsMovedThenTurn()
        {
            var log = new List<string>();
            _game.PieceMoved += (from, to, player) => log.Add($"moved {from}->{to} {player}");
            _game.TurnChanged += player => log.Add($"turn {player}");

            _game.TryMove(5, 8);

            CollectionAssert.AreEqual(new[] { "moved 5->8 One", "turn Two" }, log);
        }

        [Test]
        public void MoveThatCompletesLine_Wins()
        {
            Move(0, 4); // One: 2, 4, 5
            Move(6, 7); // Two: 1, 3, 7
            Move(4, 8); // One: 2, 5, 8 -> columna derecha

            Assert.AreEqual(GamePhase.GameOver, _game.Phase);
            Assert.AreEqual(Player.One, _game.Winner);
            Assert.AreEqual(WinReason.Line, _game.WinReason);
            Assert.AreEqual(new BoardLine(2, 5, 8), _game.WinningLine);
            Assert.AreEqual(MoveResult.WrongPhase, _game.TryMove(1, 4));
        }

        [Test]
        public void GetMoveTargets_ReturnsEmptyNeighbors()
        {
            // 0 conecta con 1 (Two), 3 (Two) y 4 (vacía).
            Assert.AreEqual(1 << 4, _game.GetMoveTargets(0));
            // 5 conecta con 2 (One), 4 y 8 (vacías).
            Assert.AreEqual((1 << 4) | (1 << 8), _game.GetMoveTargets(5));
            Assert.AreEqual(0, _game.GetMoveTargets(7), "Casilla vacía");
            Assert.AreEqual(0, _game.GetMoveTargets(-1), "Fuera del tablero");
        }

        [Test]
        public void HasAnyMove_IsTrueForBothPlayers()
        {
            Assert.IsTrue(_game.HasAnyMove(Player.One));
            Assert.IsTrue(_game.HasAnyMove(Player.Two));
        }

        [Test]
        public void PlayerBlockedWhenMovementStarts_Loses()
        {
            // Sin diagonales. One: 0, 1, 3 · Two: 2, 4, 6 (2-4-6 no es línea sin diagonales).
            // A One le toca mover y todas sus salidas están ocupadas.
            _game = new TrikiGame(_orthogonalOnly);
            var log = new List<string>();
            _game.PhaseChanged += phase => log.Add($"phase {phase}");
            _game.GameWon += (player, reason) => log.Add($"won {player} {reason}");

            Place(0, 2, 1, 4, 3, 6);

            Assert.AreEqual(GamePhase.GameOver, _game.Phase);
            Assert.AreEqual(Player.Two, _game.Winner);
            Assert.AreEqual(WinReason.OpponentBlocked, _game.WinReason);
            CollectionAssert.AreEqual(new[] { "phase GameOver", "won Two OpponentBlocked" }, log);
        }

        [Test]
        public void MoveThatBlocksOpponent_Wins()
        {
            _game = new TrikiGame(_orthogonalOnly);
            Place(0, 2, 1, 4, 6, 7); // One: 0, 1, 6 · Two: 2, 4, 7
            Assume.That(_game.Phase, Is.EqualTo(GamePhase.Movement));

            Move(6, 3); // One: 0, 1, 3
            Move(7, 6); // Two: 2, 4, 6 -> One queda sin salidas

            Assert.IsFalse(_game.HasAnyMove(Player.One));
            Assert.AreEqual(Player.Two, _game.Winner);
            Assert.AreEqual(WinReason.OpponentBlocked, _game.WinReason);
        }

        [Test]
        public void SquareGraph_EnclosingNeedsADiagonal_SoLineWinsFirst()
        {
            // Única forma de encerrar a One (0, 1, 3) en el cuadrado: Two en 2, 4, 6,
            // que es diagonal. La línea se detecta antes que el bloqueo.
            _game = new TrikiGame();
            Place(0, 2, 1, 4, 3, 6);

            Assert.AreEqual(Player.Two, _game.Winner);
            Assert.AreEqual(WinReason.Line, _game.WinReason);
        }

        private void Place(params int[] cells)
        {
            foreach (var cell in cells)
                Assert.AreEqual(PlaceResult.Placed, _game.TryPlace(cell), $"colocar en {cell}");
        }

        private void Move(int from, int to)
        {
            Assert.AreEqual(MoveResult.Moved, _game.TryMove(from, to), $"mover {from}->{to}");
        }
    }
}
