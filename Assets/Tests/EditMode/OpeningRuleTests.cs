using System;
using System.Collections.Generic;
using NUnit.Framework;
using Triki.Core;

namespace Triki.Tests
{
    /// <summary>
    /// La primera ficha de la partida no puede ir al centro. Sin esta regla el juego está
    /// resuelto: quien empieza toma el centro y gana siempre, pase lo que pase después.
    /// </summary>
    public class OpeningRuleTests
    {
        private const int Center = BoardGraph.CenterCell;

        private TrikiGame _game;

        [SetUp]
        public void SetUp() => _game = new TrikiGame();

        [Test]
        public void Center_IsTheMiddleOfTheBoard() => Assert.AreEqual(4, Center);

        [Test]
        public void OpeningInTheCenter_IsRejected_AndChangesNothing()
        {
            var events = new List<string>();
            _game.PiecePlaced += (cell, player) => events.Add($"{cell} {player}");
            _game.TurnChanged += player => events.Add($"turno {player}");

            Assert.AreEqual(PlaceResult.ForbiddenOpening, _game.TryPlace(Center));

            Assert.IsTrue(_game.Board.IsEmpty(Center));
            Assert.AreEqual(Player.One, _game.CurrentPlayer, "Sigue siendo el mismo turno.");
            Assert.AreEqual(3, _game.GetPiecesInHand(Player.One));
            Assert.IsEmpty(events, "Una jugada rechazada no emite eventos.");
        }

        [TestCase(0, TestName = "Esquina")]
        [TestCase(1, TestName = "Borde")]
        public void OpeningAnywhereElse_IsAllowed(int cell)
        {
            Assert.AreEqual(PlaceResult.Placed, _game.TryPlace(cell));
            Assert.AreEqual(Player.One, _game.Board[cell]);
        }

        [Test]
        public void Center_IsFreeFromTheSecondPieceOn()
        {
            _game.TryPlace(0);

            Assert.AreEqual(TrikiGame.NoCell, _game.ForbiddenCell, "El veto dura solo una jugada.");
            Assert.AreEqual(PlaceResult.Placed, _game.TryPlace(Center));
            Assert.AreEqual(Player.Two, _game.Board[Center]);
        }

        [Test]
        public void ForbiddenCell_IsTheCenterOnlyAtTheOpening()
        {
            Assert.IsTrue(_game.IsOpeningMove);
            Assert.AreEqual(Center, _game.ForbiddenCell);

            foreach (var cell in new[] { 0, 1, 2, 3, 5, 6 })
                Assert.AreEqual(PlaceResult.Placed, _game.TryPlace(cell), $"colocar en {cell}");

            Assert.AreEqual(GamePhase.Movement, _game.Phase);
            Assert.IsFalse(_game.IsOpeningMove);
            Assert.AreEqual(TrikiGame.NoCell, _game.ForbiddenCell, "En movimiento no se veta nada.");
        }

        [Test]
        public void AfterReset_TheCenterIsVetoedAgain()
        {
            _game.TryPlace(0);
            _game.Reset();

            Assert.AreEqual(Center, _game.ForbiddenCell);
            Assert.AreEqual(PlaceResult.ForbiddenOpening, _game.TryPlace(Center));
        }

        [Test]
        public void RuleCanBeTurnedOff_ForTheOldBehaviour()
        {
            var game = new TrikiGame(new TrikiRules(banCenterOpening: false));

            Assert.AreEqual(TrikiGame.NoCell, game.ForbiddenCell);
            Assert.AreEqual(PlaceResult.Placed, game.TryPlace(Center));
        }

        [TestCase(AiDifficulty.Easy), TestCase(AiDifficulty.Normal), TestCase(AiDifficulty.Hard)]
        public void Ai_NeverOpensInTheCenter(AiDifficulty difficulty)
        {
            for (var seed = 0; seed < 30; seed++)
            {
                var move = new TrikiAi(difficulty, new Random(seed)).ChooseMove(new TrikiGame());
                Assert.AreNotEqual(Center, move.To, $"semilla {seed}");
            }
        }

        [Test]
        public void Ai_StillUsesTheCenterWhenTheRuleIsOff()
        {
            var game = new TrikiGame(new TrikiRules(banCenterOpening: false));

            // Con el centro libre es tan buena jugada que la IA lo elige siempre.
            Assert.AreEqual(AiMove.Place(Center), new TrikiAi(AiDifficulty.Hard, new Random(1)).ChooseMove(game));
        }

        /// <summary>
        /// La prueba de que la regla equilibra el juego: dos rivales que juegan perfecto ya no
        /// deciden la partida en la primera ficha, empatan. Sin la regla, Rojo ganaba siempre.
        /// </summary>
        [Test]
        public void PerfectPlay_NowEndsInADraw()
        {
            for (var seed = 0; seed < 5; seed++)
            {
                var game = new TrikiGame();
                var red = new TrikiAi(AiDifficulty.Hard, new Random(seed));
                var blue = new TrikiAi(AiDifficulty.Hard, new Random(seed + 500));

                var plies = 0;
                while (game.Phase != GamePhase.GameOver)
                {
                    var ai = game.CurrentPlayer == Player.One ? red : blue;
                    Assert.IsTrue(ai.ChooseMove(game).ApplyTo(game), "La IA eligió una jugada inválida.");
                    Assert.Less(++plies, 200, "La partida no termina.");
                }

                Assert.AreEqual(Player.None, game.Winner, $"semilla {seed}: nadie debería ganar.");
            }
        }
    }
}
