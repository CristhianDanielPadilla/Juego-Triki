using System;
using Triki.Core;
using UnityEngine.UIElements;

namespace Triki.UI
{
    /// <summary>
    /// Panel del histórico: pestañas "Contra la IA", "Dos jugadores" y "Anteriores" (esta última
    /// solo si hay partidas guardadas antes de separar por modo). Solo muestra datos.
    /// </summary>
    internal sealed class HistoryView
    {
        private const string HiddenClass = "hidden";
        private const string SelectedClass = "segment--selected";

        private readonly Button _tabAi;
        private readonly Button _tabLocal;
        private readonly Button _tabLegacy;
        private readonly VisualElement _aiPage;
        private readonly VisualElement _localPage;
        private readonly VisualElement _legacyPage;
        private readonly Label _aiGames;
        private readonly VisualElement _aiEasy;
        private readonly VisualElement _aiNormal;
        private readonly VisualElement _aiHard;
        private readonly VisualElement _legacyTable;

        private Tab _current = Tab.VsAi;
        private bool _hasLegacy;

        public HistoryView(VisualElement panel)
        {
            if (panel == null)
                throw new ArgumentNullException(nameof(panel));

            _tabAi = panel.Q<Button>("tab-ai");
            _tabLocal = panel.Q<Button>("tab-local");
            _tabLegacy = panel.Q<Button>("tab-legacy");
            _aiPage = panel.Q("history-ai");
            _localPage = panel.Q("history-local");
            _legacyPage = panel.Q("history-legacy");
            _aiGames = _aiPage.Q<Label>("ai-games");
            _aiEasy = _aiPage.Q("ai-easy");
            _aiNormal = _aiPage.Q("ai-normal");
            _aiHard = _aiPage.Q("ai-hard");
            _legacyTable = _legacyPage.Q("legacy-stats");

            SetPlayerNames(_localPage);
            SetPlayerNames(_legacyTable);
        }

        private enum Tab
        {
            VsAi,
            TwoPlayer,
            Legacy,
        }

        public Button DefaultFocus => _tabAi;

        public void Bind()
        {
            _tabAi.RegisterCallback<ClickEvent, Tab>(HandleTabClicked, Tab.VsAi);
            _tabLocal.RegisterCallback<ClickEvent, Tab>(HandleTabClicked, Tab.TwoPlayer);
            _tabLegacy.RegisterCallback<ClickEvent, Tab>(HandleTabClicked, Tab.Legacy);
        }

        public void Unbind()
        {
            _tabAi.UnregisterCallback<ClickEvent, Tab>(HandleTabClicked);
            _tabLocal.UnregisterCallback<ClickEvent, Tab>(HandleTabClicked);
            _tabLegacy.UnregisterCallback<ClickEvent, Tab>(HandleTabClicked);
        }

        public void Show(MatchHistory history)
        {
            if (history == null)
                throw new ArgumentNullException(nameof(history));

            _aiGames.text = history.VsAi.GamesPlayed.ToString();
            FillAiRow(_aiEasy, history.VsAi, AiDifficulty.Easy);
            FillAiRow(_aiNormal, history.VsAi, AiDifficulty.Normal);
            FillAiRow(_aiHard, history.VsAi, AiDifficulty.Hard);
            FillColorTable(_localPage, history.TwoPlayer);
            FillColorTable(_legacyTable, history.Legacy);

            _hasLegacy = history.HasLegacy;
            _tabLegacy.EnableInClassList(HiddenClass, !_hasLegacy);
            if (_current == Tab.Legacy && !_hasLegacy)
                _current = Tab.VsAi;
            Select(_current);
        }

        private void HandleTabClicked(ClickEvent evt, Tab tab) => Select(tab);

        private void Select(Tab tab)
        {
            _current = tab;
            _tabAi.EnableInClassList(SelectedClass, tab == Tab.VsAi);
            _tabLocal.EnableInClassList(SelectedClass, tab == Tab.TwoPlayer);
            _tabLegacy.EnableInClassList(SelectedClass, tab == Tab.Legacy);

            _aiPage.EnableInClassList(HiddenClass, tab != Tab.VsAi);
            _localPage.EnableInClassList(HiddenClass, tab != Tab.TwoPlayer);
            _legacyPage.EnableInClassList(HiddenClass, tab != Tab.Legacy || !_hasLegacy);
        }

        private static void FillAiRow(VisualElement row, AiMatchStats stats, AiDifficulty difficulty)
        {
            row.Q<Label>("wins").text = stats.GetWins(difficulty).ToString();
            row.Q<Label>("losses").text = stats.GetLosses(difficulty).ToString();
            row.Q<Label>("draws").text = stats.GetDraws(difficulty).ToString();
        }

        private static void FillColorTable(VisualElement table, MatchStats stats)
        {
            table.Q<Label>("games").text = stats.GamesPlayed.ToString();
            table.Q<Label>("draws").text = stats.Draws.ToString();
            table.Q<Label>("player-one-wins").text = stats.GetWins(Player.One).ToString();
            table.Q<Label>("player-one-losses").text = stats.GetLosses(Player.One).ToString();
            table.Q<Label>("player-two-wins").text = stats.GetWins(Player.Two).ToString();
            table.Q<Label>("player-two-losses").text = stats.GetLosses(Player.Two).ToString();
        }

        private static void SetPlayerNames(VisualElement table)
        {
            table.Q<Label>("player-one-name").text = PlayerLabels.GetName(Player.One);
            table.Q<Label>("player-two-name").text = PlayerLabels.GetName(Player.Two);
        }
    }
}
