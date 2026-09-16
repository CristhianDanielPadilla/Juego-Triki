using NUnit.Framework;
using Triki.Core;

namespace Triki.Tests
{
    public class BoardTests
    {
        private TrikiGame _game;

        [SetUp]
        public void SetUp() => _game = new TrikiGame();

        [Test]
        public void EmptyBoard_MasksAreEmpty_ExceptNone()
        {
            Assert.AreEqual(0, _game.Board.GetMask(Player.One));
            Assert.AreEqual(0, _game.Board.GetMask(Player.Two));
            Assert.AreEqual(0b1_1111_1111, _game.Board.GetMask(Player.None));
        }

        [Test]
        public void Masks_TrackEachPlayersCells()
        {
            _game.TryPlace(0); // One
            _game.TryPlace(4); // Two
            _game.TryPlace(8); // One

            Assert.AreEqual((1 << 0) | (1 << 8), _game.Board.GetMask(Player.One));
            Assert.AreEqual(1 << 4, _game.Board.GetMask(Player.Two));
            Assert.AreEqual(0b1_1111_1111 & ~((1 << 0) | (1 << 4) | (1 << 8)), _game.Board.GetMask(Player.None));
        }

        [Test]
        public void Reset_ClearsMasks()
        {
            _game.TryPlace(0);
            _game.TryPlace(4);

            _game.Reset();

            Assert.AreEqual(0, _game.Board.GetMask(Player.One));
            Assert.AreEqual(0, _game.Board.GetMask(Player.Two));
        }

        [Test]
        public void BoardLine_MaskAndEquality()
        {
            var line = new BoardLine(2, 4, 6);

            Assert.AreEqual((1 << 2) | (1 << 4) | (1 << 6), line.Mask);
            Assert.AreEqual(new BoardLine(2, 4, 6), line);
            Assert.AreNotEqual(new BoardLine(6, 4, 2), line);
            Assert.AreEqual("(2, 4, 6)", line.ToString());
        }
    }
}
