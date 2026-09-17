using System;
using NUnit.Framework;
using Triki.Core;
using Triki.Gameplay;
using Triki.UI;
using UnityEditor;
using UnityEngine.UIElements;

namespace Triki.Tests
{
    public class AiMatchStatsTests
    {
        [Test]
        public void Records_ArePerDifficulty_AndSumIntoTotal()
        {
            var stats = new AiMatchStats();

            stats.RecordWin(AiDifficulty.Easy);
            stats.RecordWin(AiDifficulty.Easy);
            stats.RecordLoss(AiDifficulty.Hard);
            stats.RecordDraw(AiDifficulty.Normal);

            Assert.AreEqual(2, stats.GetWins(AiDifficulty.Easy));
            Assert.AreEqual(0, stats.GetWins(AiDifficulty.Hard));
            Assert.AreEqual(1, stats.GetLosses(AiDifficulty.Hard));
            Assert.AreEqual(1, stats.GetDraws(AiDifficulty.Normal));
            Assert.AreEqual(2, stats.GetGamesPlayed(AiDifficulty.Easy));
            Assert.AreEqual(4, stats.GamesPlayed);
        }

        [Test]
        public void Restore_And_Clear()
        {
            var stats = new AiMatchStats();

            stats.Restore(AiDifficulty.Hard, 1, 5, 2);
            Assert.AreEqual(8, stats.GetGamesPlayed(AiDifficulty.Hard));

            stats.Clear();
            Assert.AreEqual(0, stats.GamesPlayed);
        }

        [Test]
        public void InvalidInput_Throws()
        {
            var stats = new AiMatchStats();

            Assert.Throws<ArgumentOutOfRangeException>(() => stats.Restore(AiDifficulty.Easy, -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => stats.RecordWin((AiDifficulty)7));
        }
    }

    public class HistoryRecorderTests
    {
        private MatchHistory _history;

        [SetUp]
        public void SetUp() => _history = new MatchHistory();

        [Test]
        public void TwoPlayers_RecordsByColor()
        {
            var local = new MatchSettings(false, AiDifficulty.Hard, Player.One);

            HistoryRecorder.Record(_history, local, Player.Two);
            HistoryRecorder.Record(_history, local, Player.None);

            Assert.AreEqual(2, _history.TwoPlayer.GamesPlayed);
            Assert.AreEqual(1, _history.TwoPlayer.GetWins(Player.Two));
            Assert.AreEqual(1, _history.TwoPlayer.Draws);
            Assert.AreEqual(0, _history.VsAi.GamesPlayed);
        }

        [TestCase(Player.One, Player.One, true, TestName = "Humano Rojo gana")]
        [TestCase(Player.Two, Player.Two, true, TestName = "Humano Azul gana")]
        [TestCase(Player.One, Player.Two, false, TestName = "Humano Rojo pierde")]
        [TestCase(Player.Two, Player.One, false, TestName = "Humano Azul pierde")]
        public void VsAi_RecordsFromHumanPerspective(Player human, Player winner, bool humanWins)
        {
            var vsAi = new MatchSettings(true, AiDifficulty.Normal, human);

            HistoryRecorder.Record(_history, vsAi, winner);

            Assert.AreEqual(humanWins ? 1 : 0, _history.VsAi.GetWins(AiDifficulty.Normal));
            Assert.AreEqual(humanWins ? 0 : 1, _history.VsAi.GetLosses(AiDifficulty.Normal));
            Assert.AreEqual(0, _history.TwoPlayer.GamesPlayed);
        }

        [Test]
        public void VsAi_Draw_UsesDifficulty_AndNeverTouchesLegacy()
        {
            HistoryRecorder.Record(_history, new MatchSettings(true, AiDifficulty.Hard, Player.One), Player.None);

            Assert.AreEqual(1, _history.VsAi.GetDraws(AiDifficulty.Hard));
            Assert.IsFalse(_history.HasLegacy);
        }
    }

    public class HistoryViewTests
    {
        private VisualElement _panel;
        private HistoryView _view;

        [SetUp]
        public void SetUp()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/MainMenu.uxml");
            _panel = tree.CloneTree().Q("history-panel");
            _view = new HistoryView(_panel);
        }

        [Test]
        public void Show_FillsEachSection()
        {
            var history = new MatchHistory();
            history.VsAi.Restore(AiDifficulty.Hard, 3, 7, 1);
            history.TwoPlayer.RecordWin(Player.Two);

            _view.Show(history);

            Assert.AreEqual("11", _panel.Q("history-ai").Q<Label>("ai-games").text);
            var hard = _panel.Q("ai-hard");
            Assert.AreEqual("3", hard.Q<Label>("wins").text);
            Assert.AreEqual("7", hard.Q<Label>("losses").text);
            Assert.AreEqual("1", hard.Q<Label>("draws").text);

            var local = _panel.Q("history-local");
            Assert.AreEqual("1", local.Q<Label>("games").text);
            Assert.AreEqual("1", local.Q<Label>("player-two-wins").text);
            Assert.AreEqual("Rojo", local.Q<Label>("player-one-name").text);
        }

        [Test]
        public void Show_StartsOnAiTab_AndHidesLegacyWhenEmpty()
        {
            _view.Show(new MatchHistory());

            Assert.IsFalse(IsHidden("history-ai"));
            Assert.IsTrue(IsHidden("history-local"));
            Assert.IsTrue(IsHidden("history-legacy"));
            Assert.IsTrue(IsHidden("tab-legacy"), "Sin partidas anteriores no hay pestaña.");
            Assert.IsTrue(_panel.Q("tab-ai").ClassListContains("segment--selected"));
        }

        [Test]
        public void Show_WithLegacy_ShowsLegacyTabAndData()
        {
            var history = new MatchHistory();
            history.Legacy.Restore(6, 1, 3, 2, 2, 3);

            _view.Show(history);

            Assert.IsFalse(IsHidden("tab-legacy"));
            Assert.AreEqual("6", _panel.Q("legacy-stats").Q<Label>("games").text);
            Assert.IsTrue(IsHidden("history-legacy"), "La pestaña existe pero no está seleccionada.");
        }

        private bool IsHidden(string name) => _panel.Q(name).ClassListContains("hidden");
    }
}
