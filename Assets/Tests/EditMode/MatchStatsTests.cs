using System;
using NUnit.Framework;
using Triki.Core;

namespace Triki.Tests
{
    public class MatchStatsTests
    {
        [Test]
        public void New_IsEmpty()
        {
            var stats = new MatchStats();

            Assert.AreEqual(0, stats.GamesPlayed);
            Assert.AreEqual(0, stats.GetWins(Player.One));
            Assert.AreEqual(0, stats.GetLosses(Player.Two));
        }

        [Test]
        public void RecordWin_CountsGame_WinForWinner_LossForOpponent()
        {
            var stats = new MatchStats();

            stats.RecordWin(Player.One);
            stats.RecordWin(Player.One);
            stats.RecordWin(Player.Two);

            Assert.AreEqual(3, stats.GamesPlayed);
            Assert.AreEqual(2, stats.GetWins(Player.One));
            Assert.AreEqual(1, stats.GetLosses(Player.One));
            Assert.AreEqual(1, stats.GetWins(Player.Two));
            Assert.AreEqual(2, stats.GetLosses(Player.Two));
        }

        [Test]
        public void RecordWin_WithNone_Throws_AndChangesNothing()
        {
            var stats = new MatchStats();

            Assert.Throws<ArgumentOutOfRangeException>(() => stats.RecordWin(Player.None));
            Assert.AreEqual(0, stats.GamesPlayed);
        }

        [Test]
        public void Restore_And_Clear()
        {
            var stats = new MatchStats();

            stats.Restore(5, 3, 2, 2, 3);

            Assert.AreEqual(5, stats.GamesPlayed);
            Assert.AreEqual(3, stats.GetWins(Player.One));
            Assert.AreEqual(2, stats.GetLosses(Player.One));
            Assert.AreEqual(2, stats.GetWins(Player.Two));
            Assert.AreEqual(3, stats.GetLosses(Player.Two));

            stats.Clear();

            Assert.AreEqual(0, stats.GamesPlayed);
            Assert.AreEqual(0, stats.GetWins(Player.One));
        }

        [Test]
        public void Restore_Negative_Throws()
        {
            var stats = new MatchStats();

            Assert.Throws<ArgumentOutOfRangeException>(() => stats.Restore(1, -1, 0, 0, 0));
        }
    }
}
