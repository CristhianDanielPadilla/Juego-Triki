using System;
using System.Collections.Generic;
using NUnit.Framework;
using Triki.Core;

namespace Triki.Tests
{
    public class DrawTests
    {
        private TrikiGame _game;

        [Test]
        public void DefaultRules_Are60MovesAnd3Repetitions()
        {
            Assert.AreEqual(60, TrikiRules.Default.MaxMovementMoves);
            Assert.AreEqual(3, TrikiRules.Default.RepetitionLimit);
            Assert.AreSame(TrikiRules.Default, new TrikiGame().Rules);
        }

        [TestCase(0, 3), TestCase(10, 1)]
        public void Rules_RejectInvalidValues(int maxMoves, int repetitions)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrikiRules(maxMoves, repetitions));
        }

        [Test]
        public void SamePositionThreeTimes_IsDraw()
        {
            StartMovement(TrikiRules.Default);

            PlayCycle(); // la posición inicial aparece por 2.ª vez
            Assert.AreEqual(GamePhase.Movement, _game.Phase);

            PlayCycle(); // 3.ª vez
            Assert.AreEqual(GamePhase.GameOver, _game.Phase);
            Assert.IsTrue(_game.IsDraw);
            Assert.AreEqual(DrawReason.Repetition, _game.DrawReason);
            Assert.AreEqual(Player.None, _game.Winner);
            Assert.AreEqual(8, _game.MovementMovesPlayed);
        }

        [Test]
        public void RepetitionDraw_EmitsMovedPhaseAndDrawn_ButNoTurnChange()
        {
            StartMovement(TrikiRules.Default);
            PlayCycle();
            Move(5, 8);
            Move(6, 7);
            Move(8, 5);

            var log = new List<string>();
            _game.PieceMoved += (from, to, player) => log.Add($"moved {from}->{to}");
            _game.TurnChanged += player => log.Add($"turn {player}");
            _game.PhaseChanged += phase => log.Add($"phase {phase}");
            _game.GameDrawn += reason => log.Add($"draw {reason}");

            Move(7, 6);

            CollectionAssert.AreEqual(new[] { "moved 7->6", "phase GameOver", "draw Repetition" }, log);
            Assert.AreEqual(MoveResult.WrongPhase, _game.TryMove(0, 4));
        }

        [Test]
        public void RunningOutOfMoves_IsDraw()
        {
            StartMovement(new TrikiRules(maxMovementMoves: 4, repetitionLimit: 10));

            Move(5, 8);
            Move(6, 7);
            Move(8, 5);
            Assert.AreEqual(GamePhase.Movement, _game.Phase, "Aún queda un movimiento.");
            Move(7, 6);

            Assert.IsTrue(_game.IsDraw);
            Assert.AreEqual(DrawReason.MoveLimit, _game.DrawReason);
        }

        [Test]
        public void WinOnLastAllowedMove_BeatsMoveLimit()
        {
            StartMovement(new TrikiRules(maxMovementMoves: 3));

            Move(0, 4); // One: 2, 4, 5
            Move(6, 7); // Two: 1, 3, 7
            Move(4, 8); // One: 2, 5, 8 -> línea en el movimiento 3 de 3

            Assert.IsFalse(_game.IsDraw);
            Assert.AreEqual(Player.One, _game.Winner);
        }

        [Test]
        public void SameCellsWithOtherPlayerToMove_DoNotCountAsRepetition()
        {
            // Con límite 2, la primera repetición real ya empata. One da la vuelta al
            // triángulo 5-4-8 (3 movimientos) y Two va y vuelve (2): tras 5 movimientos las
            // fichas están igual que al inicio, pero ahora mueve Two, así que no es repetición.
            StartMovement(new TrikiRules(repetitionLimit: 2));

            Move(5, 4);
            Move(6, 7);
            Move(4, 8);
            Move(7, 6);
            Move(8, 5);

            Assert.AreEqual((1 << 0) | (1 << 2) | (1 << 5), _game.Board.GetMask(Player.One));
            Assert.AreEqual((1 << 1) | (1 << 3) | (1 << 6), _game.Board.GetMask(Player.Two));
            Assert.AreEqual(Player.Two, _game.CurrentPlayer);
            Assert.AreEqual(GamePhase.Movement, _game.Phase);
        }

        [Test]
        public void Reset_ClearsMoveCounterAndHistory()
        {
            StartMovement(new TrikiRules(repetitionLimit: 2));
            PlayCycle();
            Assume.That(_game.IsDraw);

            _game.Reset();
            Assert.AreEqual(0, _game.MovementMovesPlayed);
            Assert.IsFalse(_game.IsDraw);

            Place(0, 1, 2, 3, 5, 6);
            Move(5, 8);
            Move(6, 7);
            Move(8, 5);
            Assert.AreEqual(GamePhase.Movement, _game.Phase, "El historial anterior no debe contar.");
        }

        [Test]
        public void MovementMovesPlayed_CountsBothPlayers()
        {
            StartMovement(TrikiRules.Default);

            Move(5, 8);
            Move(6, 7);

            Assert.AreEqual(2, _game.MovementMovesPlayed);
        }

        // One = 0, 2, 5 · Two = 1, 3, 6 · vacías = 4, 7, 8 · mueve One.
        private void StartMovement(TrikiRules rules)
        {
            _game = new TrikiGame(rules);
            Place(0, 1, 2, 3, 5, 6);
            Assume.That(_game.Phase, Is.EqualTo(GamePhase.Movement));
        }

        // Cuatro movimientos que devuelven el tablero a la posición de partida, sin líneas.
        private void PlayCycle()
        {
            Move(5, 8);
            Move(6, 7);
            Move(8, 5);
            Move(7, 6);
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
