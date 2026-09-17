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

    public class MatchHistoryTests
    {
        private MatchHistory _history;

        [SetUp]
        public void SetUp()
        {
            _history = new MatchHistory();
            _history.Overall.RecordWin(Player.One);
            _history.VsAi.RecordWin(AiDifficulty.Easy);
            _history.VsAi.RecordLoss(AiDifficulty.Hard);
            _history.TwoPlayer.RecordDraw();
        }

        [Test]
        public void GetGamesPlayed_ReportsEachSection()
        {
            Assert.AreEqual(1, _history.GetGamesPlayed(HistorySection.Overall));
            Assert.AreEqual(2, _history.GetGamesPlayed(HistorySection.VsAi));
            Assert.AreEqual(1, _history.GetGamesPlayed(HistorySection.TwoPlayer));
        }

        [TestCase(HistorySection.Overall)]
        [TestCase(HistorySection.VsAi)]
        [TestCase(HistorySection.TwoPlayer)]
        public void Clear_OnlyEmptiesThatSection(HistorySection section)
        {
            var before = new[]
            {
                _history.GetGamesPlayed(HistorySection.Overall),
                _history.GetGamesPlayed(HistorySection.VsAi),
                _history.GetGamesPlayed(HistorySection.TwoPlayer),
            };

            _history.Clear(section);

            foreach (HistorySection other in Enum.GetValues(typeof(HistorySection)))
            {
                var expected = other == section ? 0 : before[(int)other];
                Assert.AreEqual(expected, _history.GetGamesPlayed(other), other.ToString());
            }
        }

        [Test]
        public void ClearAll_EmptiesEverything()
        {
            Assert.IsFalse(_history.IsEmpty);

            _history.ClearAll();

            Assert.IsTrue(_history.IsEmpty);
            foreach (HistorySection section in Enum.GetValues(typeof(HistorySection)))
                Assert.AreEqual(0, _history.GetGamesPlayed(section));
        }

        [TestCase(HistorySection.Overall)]
        [TestCase(HistorySection.VsAi)]
        [TestCase(HistorySection.TwoPlayer)]
        public void IsEmpty_IsFalse_WhileAnySectionHasGames(HistorySection keep)
        {
            foreach (HistorySection section in Enum.GetValues(typeof(HistorySection)))
            {
                if (section != keep)
                    _history.Clear(section);
            }

            Assert.IsFalse(_history.IsEmpty);
        }
    }

    public class HistoryRecorderTests
    {
        private MatchHistory _history;

        [SetUp]
        public void SetUp() => _history = new MatchHistory();

        [Test]
        public void TwoPlayers_RecordsByColor_InTwoPlayerAndOverall()
        {
            var local = new MatchSettings(false, AiDifficulty.Hard, Player.One);

            HistoryRecorder.Record(_history, local, Player.Two);
            HistoryRecorder.Record(_history, local, Player.None);

            Assert.AreEqual(2, _history.TwoPlayer.GamesPlayed);
            Assert.AreEqual(1, _history.TwoPlayer.GetWins(Player.Two));
            Assert.AreEqual(1, _history.TwoPlayer.Draws);
            Assert.AreEqual(2, _history.Overall.GamesPlayed);
            Assert.AreEqual(1, _history.Overall.GetWins(Player.Two));
            Assert.AreEqual(0, _history.VsAi.GamesPlayed);
        }

        [TestCase(Player.One, Player.One, true, TestName = "Humano Rojo gana")]
        [TestCase(Player.Two, Player.Two, true, TestName = "Humano Azul gana")]
        [TestCase(Player.One, Player.Two, false, TestName = "Humano Rojo pierde")]
        [TestCase(Player.Two, Player.One, false, TestName = "Humano Azul pierde")]
        public void VsAi_RecordsFromHumanPerspective_AndOverallByColor(Player human, Player winner, bool humanWins)
        {
            var vsAi = new MatchSettings(true, AiDifficulty.Normal, human);

            HistoryRecorder.Record(_history, vsAi, winner);

            Assert.AreEqual(humanWins ? 1 : 0, _history.VsAi.GetWins(AiDifficulty.Normal));
            Assert.AreEqual(humanWins ? 0 : 1, _history.VsAi.GetLosses(AiDifficulty.Normal));
            Assert.AreEqual(1, _history.Overall.GetWins(winner), "El general registra el color ganador.");
            Assert.AreEqual(0, _history.TwoPlayer.GamesPlayed);
        }

        [Test]
        public void VsAi_Draw_UsesDifficulty()
        {
            HistoryRecorder.Record(_history, new MatchSettings(true, AiDifficulty.Hard, Player.One), Player.None);

            Assert.AreEqual(1, _history.VsAi.GetDraws(AiDifficulty.Hard));
            Assert.AreEqual(1, _history.Overall.Draws);
        }
    }

    public class HistoryViewTests
    {
        private VisualElement _root;
        private VisualElement _panel;
        private HistoryView _view;

        [SetUp]
        public void SetUp()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/MainMenu.uxml");
            _root = tree.CloneTree();
            _panel = _root.Q("history-panel");
            _view = new HistoryView(_panel);
        }

        [Test]
        public void Show_FillsEachSection()
        {
            var history = SampleHistory();

            _view.Show(history);

            var general = _panel.Q("history-general");
            Assert.AreEqual("3", general.Q<Label>("games").text);
            Assert.AreEqual("2", general.Q<Label>("player-one-wins").text);
            Assert.AreEqual("Rojo", general.Q<Label>("player-one-name").text);

            Assert.AreEqual("11", _panel.Q("history-ai").Q<Label>("ai-games").text);
            var hard = _panel.Q("ai-hard");
            Assert.AreEqual("3", hard.Q<Label>("wins").text);
            Assert.AreEqual("7", hard.Q<Label>("losses").text);
            Assert.AreEqual("1", hard.Q<Label>("draws").text);

            var local = _panel.Q("history-local");
            Assert.AreEqual("0", local.Q<Label>("games").text);
        }

        [Test]
        public void Show_StartsOnGeneralTab()
        {
            _view.Show(SampleHistory());

            Assert.AreEqual(HistorySection.Overall, _view.CurrentSection);
            Assert.IsFalse(IsHidden("history-general"));
            Assert.IsTrue(IsHidden("history-ai"));
            Assert.IsTrue(IsHidden("history-local"));
            Assert.IsTrue(_panel.Q("tab-general").ClassListContains("segment--selected"));
        }

        [TestCase(HistorySection.Overall, "Eliminar registro general", true)]
        [TestCase(HistorySection.VsAi, "Eliminar registro contra la IA", true)]
        [TestCase(HistorySection.TwoPlayer, "Eliminar registro de dos jugadores", false)]
        public void DeleteButton_NamesTheTab_AndIsDisabledWhenEmpty(HistorySection section, string text, bool enabled)
        {
            _view.Show(SampleHistory());

            _view.Select(section);

            var button = _panel.Q<Button>("delete-history-button");
            Assert.AreEqual(text, button.text);
            Assert.AreEqual(enabled, button.enabledSelf);
            Assert.IsFalse(IsHidden(PageOf(section)));
        }

        [Test]
        public void RequestDelete_RaisesCurrentSection_OnlyWhenThereIsData()
        {
            HistorySection? requested = null;
            _view.DeleteRequested += section => requested = section;
            _view.Show(SampleHistory());

            _view.Select(HistorySection.TwoPlayer);
            _view.RequestDelete();
            Assert.IsNull(requested, "Sin partidas no se pide borrar.");

            _view.Select(HistorySection.VsAi);
            _view.RequestDelete();
            Assert.AreEqual(HistorySection.VsAi, requested);
        }

        [Test]
        public void DeleteAllButton_IsEnabledOnEveryTab_UnlessEverythingIsEmpty()
        {
            var button = _panel.Q<Button>("delete-all-history-button");

            _view.Show(SampleHistory());
            _view.Select(HistorySection.TwoPlayer); // pestaña vacía, pero hay datos en otras
            Assert.IsTrue(button.enabledSelf);

            _view.Show(new MatchHistory());
            Assert.IsFalse(button.enabledSelf);
        }

        [Test]
        public void RequestDeleteAll_RaisesOnlyWhenThereIsData()
        {
            var requests = 0;
            _view.DeleteAllRequested += () => requests++;

            _view.Show(new MatchHistory());
            _view.RequestDeleteAll();
            Assert.AreEqual(0, requests);

            _view.Show(SampleHistory());
            _view.RequestDeleteAll();
            Assert.AreEqual(1, requests);
        }

        [Test]
        public void DeleteAllMessage_ListsEverySection()
        {
            var message = HistoryView.GetDeleteAllMessage(SampleHistory());

            StringAssert.Contains("General: 3 partidas", message);
            StringAssert.Contains("Contra la IA: 11 partidas", message);
            StringAssert.Contains("Dos jugadores: 0 partidas", message);
            StringAssert.Contains("no se puede deshacer", message);
        }

        [Test]
        public void DeleteMessage_SaysWhatIsDeletedAndWhatIsKept()
        {
            var one = HistoryView.GetDeleteMessage(HistorySection.VsAi, 1);
            var many = HistoryView.GetDeleteMessage(HistorySection.Overall, 10);

            StringAssert.Contains("la 1 partida del registro contra la IA", one);
            StringAssert.Contains("El registro general y el de dos jugadores no cambian", one);
            StringAssert.Contains("las 10 partidas del registro general", many);
            StringAssert.Contains("no se puede deshacer", many);
        }

        [Test]
        public void ConfirmDialog_ConfirmRunsActionOnce_AndCancelDoesNot()
        {
            var dialog = new ConfirmDialog(_root.Q("confirm-overlay"));
            var runs = 0;

            Assert.IsFalse(dialog.IsOpen, "Empieza cerrado.");

            dialog.Show("¿Eliminar?", "Mensaje", "Eliminar", () => runs++);
            Assert.IsTrue(dialog.IsOpen);
            Assert.AreEqual("Mensaje", _root.Q<Label>("confirm-message").text);
            dialog.Cancel();
            Assert.IsFalse(dialog.IsOpen);
            Assert.AreEqual(0, runs);

            dialog.Show("¿Eliminar?", "Mensaje", "Eliminar", () => runs++);
            dialog.Confirm();
            dialog.Confirm(); // un segundo clic con el diálogo ya cerrado no repite la acción
            Assert.IsFalse(dialog.IsOpen);
            Assert.AreEqual(1, runs);
        }

        private static MatchHistory SampleHistory()
        {
            var history = new MatchHistory();
            history.Overall.Restore(3, 0, 2, 1, 1, 2);
            history.VsAi.Restore(AiDifficulty.Hard, 3, 7, 1);
            return history;
        }

        private static string PageOf(HistorySection section)
        {
            switch (section)
            {
                case HistorySection.Overall: return "history-general";
                case HistorySection.VsAi: return "history-ai";
                default: return "history-local";
            }
        }

        private bool IsHidden(string name) => _panel.Q(name).ClassListContains("hidden");
    }
}
