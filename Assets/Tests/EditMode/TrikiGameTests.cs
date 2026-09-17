using System;
using System.Collections.Generic;
using NUnit.Framework;
using Triki.Core;

namespace Triki.Tests
{
    public class TrikiGameTests
    {
        private TrikiGame _game;

        [SetUp]
        public void SetUp() => _game = new TrikiGame();

        [Test]
        public void NewGame_StartsInPlacement_WithPlayerOne_AndFullHands()
        {
            Assert.AreEqual(GamePhase.Placement, _game.Phase);
            Assert.AreEqual(Player.One, _game.CurrentPlayer);
            Assert.AreEqual(3, _game.GetPiecesInHand(Player.One));
            Assert.AreEqual(3, _game.GetPiecesInHand(Player.Two));
            for (var cell = 0; cell < BoardGraph.CellCount; cell++)
                Assert.IsTrue(_game.Board.IsEmpty(cell));
        }

        [Test]
        public void TryPlace_OccupiesCell_AndPassesTurn()
        {
            Assert.AreEqual(PlaceResult.Placed, _game.TryPlace(0));

            Assert.AreEqual(Player.One, _game.Board[0]);
            Assert.AreEqual(Player.Two, _game.CurrentPlayer);
            Assert.AreEqual(2, _game.GetPiecesInHand(Player.One));
        }

        [Test]
        public void TryPlace_OnOccupiedCell_IsRejected_AndKeepsTurn()
        {
            _game.TryPlace(0);

            Assert.AreEqual(PlaceResult.CellOccupied, _game.TryPlace(0));
            Assert.AreEqual(Player.One, _game.Board[0]);
            Assert.AreEqual(Player.Two, _game.CurrentPlayer);
            Assert.AreEqual(3, _game.GetPiecesInHand(Player.Two));
        }

        [TestCase(-1), TestCase(9)]
        public void TryPlace_OutsideBoard_IsRejected(int cell)
        {
            Assert.AreEqual(PlaceResult.InvalidCell, _game.TryPlace(cell));
            Assert.AreEqual(Player.One, _game.CurrentPlayer);
        }

        [Test]
        public void SixPlacements_UseAllPieces_AndSwitchToMovement()
        {
            PlaceSix();

            Assert.AreEqual(GamePhase.Movement, _game.Phase);
            Assert.AreEqual(0, _game.GetPiecesInHand(Player.One));
            Assert.AreEqual(0, _game.GetPiecesInHand(Player.Two));
            Assert.AreEqual(Player.One, _game.CurrentPlayer, "Tras la 6.ª ficha (de Two) mueve One.");
        }

        [Test]
        public void TryPlace_AfterPlacementPhase_IsRejected()
        {
            PlaceSix();

            Assert.AreEqual(PlaceResult.WrongPhase, _game.TryPlace(7));
            Assert.IsTrue(_game.Board.IsEmpty(7));
        }

        [Test]
        public void Events_FireInOrder_WithCorrectData()
        {
            var log = new List<string>();
            _game.PiecePlaced += (cell, player) => log.Add($"placed {cell} {player}");
            _game.TurnChanged += player => log.Add($"turn {player}");
            _game.PhaseChanged += phase => log.Add($"phase {phase}");

            _game.TryPlace(0);
            _game.TryPlace(0); // rechazada: no debe emitir nada

            CollectionAssert.AreEqual(new[] { "placed 0 One", "turn Two" }, log);

            log.Clear();
            _game.TryPlace(1);
            _game.TryPlace(2);
            _game.TryPlace(3);
            _game.TryPlace(5);
            _game.TryPlace(6);

            CollectionAssert.AreEqual(new[] { "placed 6 Two", "phase Movement", "turn One" }, log.GetRange(log.Count - 3, 3));
        }

        [Test]
        public void Reset_ClearsBoard_AndHonorsStartingPlayer()
        {
            var resets = 0;
            _game.GameReset += () => resets++;
            PlaceSix();

            _game.Reset(Player.Two);

            Assert.AreEqual(1, resets);
            Assert.AreEqual(GamePhase.Placement, _game.Phase);
            Assert.AreEqual(Player.Two, _game.CurrentPlayer);
            Assert.AreEqual(3, _game.GetPiecesInHand(Player.One));
            for (var cell = 0; cell < BoardGraph.CellCount; cell++)
                Assert.IsTrue(_game.Board.IsEmpty(cell));
        }

        [Test]
        public void Reset_WithNone_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _game.Reset(Player.None));
        }

        private void PlaceSix()
        {
            foreach (var cell in new[] { 0, 1, 2, 3, 5, 6 })
                Assert.AreEqual(PlaceResult.Placed, _game.TryPlace(cell), $"casilla {cell}");
        }
    }
}
