using System;
using Triki.Core;
using UnityEngine.UIElements;

namespace Triki.UI
{
    /// <summary>
    /// Panel del histórico: pestañas "General", "Contra la IA" y "Dos jugadores", y un botón que
    /// pide borrar el registro de la pestaña visible (<see cref="DeleteRequested"/>).
    /// Solo muestra datos; borrar y confirmar lo decide quien la usa.
    /// </summary>
    internal sealed class HistoryView
    {
        private const string HiddenClass = "hidden";
        private const string SelectedClass = "segment--selected";

        private readonly Button _tabGeneral;
        private readonly Button _tabAi;
        private readonly Button _tabLocal;
        private readonly VisualElement _generalPage;
        private readonly VisualElement _aiPage;
        private readonly VisualElement _localPage;
        private readonly Label _aiGames;
        private readonly Label _aiColorless;
        private readonly VisualElement _aiEasy;
        private readonly VisualElement _aiNormal;
        private readonly VisualElement _aiHard;
        private readonly Button _deleteButton;

        private MatchHistory _history;

        public HistoryView(VisualElement panel)
        {
            if (panel == null)
                throw new ArgumentNullException(nameof(panel));

            _tabGeneral = panel.Q<Button>("tab-general");
            _tabAi = panel.Q<Button>("tab-ai");
            _tabLocal = panel.Q<Button>("tab-local");
            _generalPage = panel.Q("history-general");
            _aiPage = panel.Q("history-ai");
            _localPage = panel.Q("history-local");
            _aiGames = _aiPage.Q<Label>("ai-games");
            _aiColorless = _aiPage.Q<Label>("ai-colorless");
            _aiEasy = _aiPage.Q("ai-easy");
            _aiNormal = _aiPage.Q("ai-normal");
            _aiHard = _aiPage.Q("ai-hard");
            _deleteButton = panel.Q<Button>("delete-history-button");

            SetPlayerNames(_generalPage);
            SetPlayerNames(_localPage);
        }

        /// <summary>El usuario pulsó "Eliminar registro" en la pestaña indicada.</summary>
        public event Action<HistorySection> DeleteRequested;

        public HistorySection CurrentSection { get; private set; } = HistorySection.Overall;

        public Button DefaultFocus => _tabGeneral;

        public static string GetSectionName(HistorySection section)
        {
            switch (section)
            {
                case HistorySection.Overall: return "general";
                case HistorySection.VsAi: return "contra la IA";
                case HistorySection.TwoPlayer: return "de dos jugadores";
                default: throw new ArgumentOutOfRangeException(nameof(section), section, "Sección desconocida.");
            }
        }

        /// <summary>Texto de la advertencia: cuántas partidas se borran y qué registros no cambian.</summary>
        public static string GetDeleteMessage(HistorySection section, int gamesPlayed)
        {
            var games = gamesPlayed == 1 ? "la 1 partida" : $"las {gamesPlayed} partidas";
            string untouched;
            switch (section)
            {
                case HistorySection.Overall:
                    untouched = "Los registros contra la IA y de dos jugadores no cambian.";
                    break;
                case HistorySection.VsAi:
                    untouched = "El registro general y el de dos jugadores no cambian.";
                    break;
                case HistorySection.TwoPlayer:
                    untouched = "El registro general y el de contra la IA no cambian.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(section), section, "Sección desconocida.");
            }

            return $"Se borrarán {games} del registro {GetSectionName(section)}. {untouched}\nEsta acción no se puede deshacer.";
        }

        public void Bind()
        {
            _tabGeneral.RegisterCallback<ClickEvent, HistorySection>(HandleTabClicked, HistorySection.Overall);
            _tabAi.RegisterCallback<ClickEvent, HistorySection>(HandleTabClicked, HistorySection.VsAi);
            _tabLocal.RegisterCallback<ClickEvent, HistorySection>(HandleTabClicked, HistorySection.TwoPlayer);
            _deleteButton.clicked += RequestDelete;
        }

        public void Unbind()
        {
            _tabGeneral.UnregisterCallback<ClickEvent, HistorySection>(HandleTabClicked);
            _tabAi.UnregisterCallback<ClickEvent, HistorySection>(HandleTabClicked);
            _tabLocal.UnregisterCallback<ClickEvent, HistorySection>(HandleTabClicked);
            _deleteButton.clicked -= RequestDelete;
        }

        /// <summary>Pinta el histórico y conserva la pestaña que estaba seleccionada.</summary>
        public void Show(MatchHistory history)
        {
            _history = history ?? throw new ArgumentNullException(nameof(history));

            FillColorTable(_generalPage, history.Overall);
            _aiGames.text = history.VsAi.GamesPlayed.ToString();
            FillAiRow(_aiEasy, history.VsAi, AiDifficulty.Easy);
            FillAiRow(_aiNormal, history.VsAi, AiDifficulty.Normal);
            FillAiRow(_aiHard, history.VsAi, AiDifficulty.Hard);
            FillColorlessNote(history.VsAi.ColorlessGames);
            FillColorTable(_localPage, history.TwoPlayer);

            Select(CurrentSection);
        }

        internal void Select(HistorySection section)
        {
            CurrentSection = section;
            _tabGeneral.EnableInClassList(SelectedClass, section == HistorySection.Overall);
            _tabAi.EnableInClassList(SelectedClass, section == HistorySection.VsAi);
            _tabLocal.EnableInClassList(SelectedClass, section == HistorySection.TwoPlayer);

            _generalPage.EnableInClassList(HiddenClass, section != HistorySection.Overall);
            _aiPage.EnableInClassList(HiddenClass, section != HistorySection.VsAi);
            _localPage.EnableInClassList(HiddenClass, section != HistorySection.TwoPlayer);

            _deleteButton.text = "Eliminar registro " + GetSectionName(section);
            _deleteButton.SetEnabled(_history != null && _history.GetGamesPlayed(section) > 0);
        }

        internal void RequestDelete()
        {
            if (_history != null && _history.GetGamesPlayed(CurrentSection) > 0)
                DeleteRequested?.Invoke(CurrentSection);
        }

        private void HandleTabClicked(ClickEvent evt, HistorySection section) => Select(section);

        /// <summary>
        /// Texto del aviso de partidas sin color, o <c>null</c> si no hay ninguna y no se muestra.
        /// Las partidas de v0.3.0 y anteriores cuentan en el total pero no se pueden repartir
        /// entre Rojo y Azul, así que sin este aviso las dos columnas parecerían no cuadrar.
        /// </summary>
        public static string GetColorlessNote(int games)
        {
            if (games <= 0)
                return null;
            return games == 1
                ? "1 partida anterior no guardó el color; cuenta en el total pero no en las columnas."
                : $"{games} partidas anteriores no guardaron el color; cuentan en el total pero no en las columnas.";
        }

        private void FillColorlessNote(int games)
        {
            var note = GetColorlessNote(games);
            _aiColorless.text = note ?? string.Empty;
            _aiColorless.EnableInClassList(HiddenClass, note == null);
        }

        private static void FillAiRow(VisualElement row, AiMatchStats stats, AiDifficulty difficulty)
        {
            FillAiResults(row, "one", stats, difficulty, Player.One);
            FillAiResults(row, "two", stats, difficulty, Player.Two);
        }

        private static void FillAiResults(VisualElement row, string prefix, AiMatchStats stats, AiDifficulty difficulty, Player color)
        {
            row.Q<Label>(prefix + "-wins").text = stats.GetWins(difficulty, color).ToString();
            row.Q<Label>(prefix + "-losses").text = stats.GetLosses(difficulty, color).ToString();
            row.Q<Label>(prefix + "-draws").text = stats.GetDraws(difficulty, color).ToString();
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
