using System;
using System.Diagnostics;
using NUnit.Framework;
using Triki.Core;

namespace Triki.Tests
{
    public class TrikiAiTests
    {
        [TestCase(AiDifficulty.Easy), TestCase(AiDifficulty.Normal), TestCase(AiDifficulty.Hard)]
        public void EmptyBoard_ReturnsLegalPlacement(AiDifficulty difficulty)
        {
            var game = new TrikiGame();
            var move = new TrikiAi(difficulty, new Random(1)).ChooseMove(game);

            Assert.IsTrue(move.IsPlacement);
            Assert.IsTrue(move.ApplyTo(game), move.ToString());
        }

        [TestCase(AiDifficulty.Normal), TestCase(AiDifficulty.Hard)]
        public void Placement_TakesImmediateWin(AiDifficulty difficulty)
        {
            var game = Place(new TrikiGame(), 0, 3, 1, 4); // One: 0, 1 · Two: 3, 4 · mueve One

            var move = new TrikiAi(difficulty, new Random(1)).ChooseMove(game);

            Assert.AreEqual(AiMove.Place(2), move);
        }

        [TestCase(AiDifficulty.Normal), TestCase(AiDifficulty.Hard)]
        public void Placement_BlocksOpponentLine(AiDifficulty difficulty)
        {
            var game = Place(new TrikiGame(), 0, 4, 1); // One: 0, 1 amenaza la fila · mueve Two

            var move = new TrikiAi(difficulty, new Random(1)).ChooseMove(game);

            Assert.AreEqual(AiMove.Place(2), move);
        }

        [TestCase(AiDifficulty.Normal), TestCase(AiDifficulty.Hard)]
        public void Movement_TakesImmediateWin(AiDifficulty difficulty)
        {
            // One: 2, 4, 5 · Two: 1, 3, 7 · mueve One; 4->8 completa la columna 2-5-8.
            var game = Place(new TrikiGame(), 0, 1, 2, 3, 5, 6);
            game.TryMove(0, 4);
            game.TryMove(6, 7);

            var move = new TrikiAi(difficulty, new Random(1)).ChooseMove(game);

            Assert.AreEqual(AiMove.Move(4, 8), move);
            Assert.IsTrue(move.ApplyTo(game));
            Assert.AreEqual(Player.One, game.Winner);
        }

        [Test]
        public void Movement_ReturnsOnlyLegalMoves()
        {
            var game = Place(new TrikiGame(), 0, 1, 2, 3, 5, 6);
            var ai = new TrikiAi(AiDifficulty.Easy, new Random(7));

            for (var i = 0; i < 20 && game.Phase == GamePhase.Movement; i++)
            {
                var move = ai.ChooseMove(game);
                Assert.IsFalse(move.IsPlacement);
                Assert.AreEqual(game.CurrentPlayer, game.Board[move.From]);
                Assert.IsTrue(move.ApplyTo(game), move.ToString());
            }
        }

        [Test]
        public void GameOver_Throws()
        {
            var game = Place(new TrikiGame(), 0, 3, 1, 4, 2);

            Assert.Throws<InvalidOperationException>(() => new TrikiAi(AiDifficulty.Hard).ChooseMove(game));
        }

        [Test]
        public void SameSeed_SameChoices()
        {
            var first = PlayFullGame(new TrikiAi(AiDifficulty.Easy, new Random(42)), new TrikiAi(AiDifficulty.Easy, new Random(43)));
            var second = PlayFullGame(new TrikiAi(AiDifficulty.Easy, new Random(42)), new TrikiAi(AiDifficulty.Easy, new Random(43)));

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Hard_NeverLosesAgainstEasy([Range(0, 19)] int seed)
        {
            var hardPlays = seed % 2 == 0 ? Player.One : Player.Two;
            var hard = new TrikiAi(AiDifficulty.Hard, new Random(seed));
            var easy = new TrikiAi(AiDifficulty.Easy, new Random(seed + 1000));

            var result = hardPlays == Player.One ? PlayFullGame(hard, easy) : PlayFullGame(easy, hard);

            Assert.AreNotEqual(hardPlays.Opponent().ToString(), result, $"Difícil ({hardPlays}) perdió.");
        }

        [Test]
        public void HardVsHard_EndsWithinMoveLimit()
        {
            var result = PlayFullGame(new TrikiAi(AiDifficulty.Hard, new Random(3)), new TrikiAi(AiDifficulty.Hard, new Random(4)));

            Assert.That(result, Is.EqualTo("One").Or.EqualTo("Two").Or.EqualTo("Draw"));
        }

        [Test]
        public void Hard_DecidesQuickly_OnTheSlowestPosition()
        {
            // El tablero vacío es el árbol más grande. Margen amplio para no depender de la máquina.
            var ai = new TrikiAi(AiDifficulty.Hard, new Random(1));
            var game = new TrikiGame();
            ai.ChooseMove(game); // calentamiento (JIT)

            var watch = Stopwatch.StartNew();
            ai.ChooseMove(game);
            watch.Stop();

            Assert.Less(watch.ElapsedMilliseconds, 500, $"Tardó {watch.ElapsedMilliseconds} ms.");
        }

        [Test]
        public void AiMove_Describes()
        {
            Assert.AreEqual("colocar 4", AiMove.Place(4).ToString());
            Assert.AreEqual("mover 0->4", AiMove.Move(0, 4).ToString());
            Assert.AreNotEqual(AiMove.Place(4), AiMove.Move(0, 4));
        }

        private static TrikiGame Place(TrikiGame game, params int[] cells)
        {
            foreach (var cell in cells)
                Assert.AreEqual(PlaceResult.Placed, game.TryPlace(cell), $"colocar en {cell}");
            return game;
        }

        /// <summary>Juega hasta el final y devuelve "One", "Two" o "Draw".</summary>
        private static string PlayFullGame(TrikiAi one, TrikiAi two)
        {
            var game = new TrikiGame();
            var guard = 0;
            while (game.Phase != GamePhase.GameOver)
            {
                Assert.Less(guard++, 200, "La partida no terminó.");
                var ai = game.CurrentPlayer == Player.One ? one : two;
                var move = ai.ChooseMove(game);
                Assert.IsTrue(move.ApplyTo(game), move.ToString());
            }

            return game.IsDraw ? "Draw" : game.Winner.ToString();
        }
    }
}
