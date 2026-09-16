using Triki.Core;
using Triki.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Triki.UI
{
    /// <summary>Menú de inicio: configurar e iniciar partida, ver el histórico y salir.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string HiddenClass = "hidden";
        private const string SelectedClass = "segment--selected";

        [SerializeField] private UIDocument _document;

        private VisualElement _mainPanel;
        private VisualElement _setupPanel;
        private VisualElement _historyPanel;
        private VisualElement _aiOptions;

        private Button _playButton;
        private Button _historyButton;
        private Button _quitButton;
        private Button _startButton;
        private Button _setupBackButton;
        private Button _historyBackButton;

        private Button _modeAi;
        private Button _modeLocal;
        private Button _difficultyEasy;
        private Button _difficultyNormal;
        private Button _difficultyHard;
        private Button _colorOne;
        private Button _colorTwo;

        private Label _gamesPlayed;
        private Label _draws;
        private Label _playerOneName;
        private Label _playerOneWins;
        private Label _playerOneLosses;
        private Label _playerTwoName;
        private Label _playerTwoWins;
        private Label _playerTwoLosses;

        private MatchSettings _settings;

        private void OnEnable()
        {
            if (_document == null)
            {
                Debug.LogError($"{nameof(MainMenuController)}: asigna el UIDocument en el inspector.", this);
                return;
            }

            var root = _document.rootVisualElement;
            _mainPanel = root.Q("main-panel");
            _setupPanel = root.Q("setup-panel");
            _historyPanel = root.Q("history-panel");
            _aiOptions = root.Q("ai-options");

            _playButton = root.Q<Button>("play-button");
            _historyButton = root.Q<Button>("history-button");
            _quitButton = root.Q<Button>("quit-button");
            _startButton = root.Q<Button>("start-button");
            _setupBackButton = root.Q<Button>("setup-back-button");
            _historyBackButton = root.Q<Button>("back-button");

            _modeAi = root.Q<Button>("mode-ai");
            _modeLocal = root.Q<Button>("mode-local");
            _difficultyEasy = root.Q<Button>("difficulty-easy");
            _difficultyNormal = root.Q<Button>("difficulty-normal");
            _difficultyHard = root.Q<Button>("difficulty-hard");
            _colorOne = root.Q<Button>("color-one");
            _colorTwo = root.Q<Button>("color-two");

            _gamesPlayed = root.Q<Label>("games-played");
            _draws = root.Q<Label>("draws");
            _playerOneName = root.Q<Label>("player-one-name");
            _playerOneWins = root.Q<Label>("player-one-wins");
            _playerOneLosses = root.Q<Label>("player-one-losses");
            _playerTwoName = root.Q<Label>("player-two-name");
            _playerTwoWins = root.Q<Label>("player-two-wins");
            _playerTwoLosses = root.Q<Label>("player-two-losses");

            _playerOneName.text = PlayerLabels.GetName(Player.One);
            _playerTwoName.text = PlayerLabels.GetName(Player.Two);

            _playButton.clicked += ShowSetup;
            _historyButton.clicked += ShowHistory;
            _quitButton.clicked += SceneNavigator.QuitApplication;
            _startButton.clicked += StartMatch;
            _setupBackButton.clicked += ShowMain;
            _historyBackButton.clicked += ShowMain;

            _modeAi.RegisterCallback<ClickEvent, bool>(HandleModeClicked, true);
            _modeLocal.RegisterCallback<ClickEvent, bool>(HandleModeClicked, false);
            _difficultyEasy.RegisterCallback<ClickEvent, AiDifficulty>(HandleDifficultyClicked, AiDifficulty.Easy);
            _difficultyNormal.RegisterCallback<ClickEvent, AiDifficulty>(HandleDifficultyClicked, AiDifficulty.Normal);
            _difficultyHard.RegisterCallback<ClickEvent, AiDifficulty>(HandleDifficultyClicked, AiDifficulty.Hard);
            _colorOne.RegisterCallback<ClickEvent, Player>(HandleColorClicked, Player.One);
            _colorTwo.RegisterCallback<ClickEvent, Player>(HandleColorClicked, Player.Two);

            ShowMain();
        }

        private void OnDisable()
        {
            if (_playButton == null)
                return;

            _playButton.clicked -= ShowSetup;
            _historyButton.clicked -= ShowHistory;
            _quitButton.clicked -= SceneNavigator.QuitApplication;
            _startButton.clicked -= StartMatch;
            _setupBackButton.clicked -= ShowMain;
            _historyBackButton.clicked -= ShowMain;

            _modeAi.UnregisterCallback<ClickEvent, bool>(HandleModeClicked);
            _modeLocal.UnregisterCallback<ClickEvent, bool>(HandleModeClicked);
            _difficultyEasy.UnregisterCallback<ClickEvent, AiDifficulty>(HandleDifficultyClicked);
            _difficultyNormal.UnregisterCallback<ClickEvent, AiDifficulty>(HandleDifficultyClicked);
            _difficultyHard.UnregisterCallback<ClickEvent, AiDifficulty>(HandleDifficultyClicked);
            _colorOne.UnregisterCallback<ClickEvent, Player>(HandleColorClicked);
            _colorTwo.UnregisterCallback<ClickEvent, Player>(HandleColorClicked);
        }

        private void ShowMain()
        {
            ShowOnly(_mainPanel);
            _playButton.Focus();
        }

        private void ShowSetup()
        {
            _settings = MatchSettingsStore.Load();
            RefreshSetup();
            ShowOnly(_setupPanel);
            _startButton.Focus();
        }

        private void ShowHistory()
        {
            // Se lee al abrir: así siempre refleja las partidas jugadas desde el último vistazo.
            var stats = new StatsRepository().Load();
            _gamesPlayed.text = stats.GamesPlayed.ToString();
            _draws.text = stats.Draws.ToString();
            _playerOneWins.text = stats.GetWins(Player.One).ToString();
            _playerOneLosses.text = stats.GetLosses(Player.One).ToString();
            _playerTwoWins.text = stats.GetWins(Player.Two).ToString();
            _playerTwoLosses.text = stats.GetLosses(Player.Two).ToString();

            ShowOnly(_historyPanel);
            _historyBackButton.Focus();
        }

        private void StartMatch()
        {
            MatchSettingsStore.Save(_settings);
            SceneNavigator.StartGame();
        }

        private void HandleModeClicked(ClickEvent evt, bool vsAi)
        {
            _settings = _settings.WithVsAi(vsAi);
            RefreshSetup();
        }

        private void HandleDifficultyClicked(ClickEvent evt, AiDifficulty difficulty)
        {
            _settings = _settings.WithDifficulty(difficulty);
            RefreshSetup();
        }

        private void HandleColorClicked(ClickEvent evt, Player player)
        {
            _settings = _settings.WithHumanPlayer(player);
            RefreshSetup();
        }

        private void RefreshSetup()
        {
            _modeAi.EnableInClassList(SelectedClass, _settings.VsAi);
            _modeLocal.EnableInClassList(SelectedClass, !_settings.VsAi);
            _aiOptions.EnableInClassList(HiddenClass, !_settings.VsAi);

            _difficultyEasy.EnableInClassList(SelectedClass, _settings.Difficulty == AiDifficulty.Easy);
            _difficultyNormal.EnableInClassList(SelectedClass, _settings.Difficulty == AiDifficulty.Normal);
            _difficultyHard.EnableInClassList(SelectedClass, _settings.Difficulty == AiDifficulty.Hard);

            _colorOne.EnableInClassList(SelectedClass, _settings.HumanPlayer == Player.One);
            _colorTwo.EnableInClassList(SelectedClass, _settings.HumanPlayer == Player.Two);
        }

        private void ShowOnly(VisualElement panel)
        {
            _mainPanel.EnableInClassList(HiddenClass, panel != _mainPanel);
            _setupPanel.EnableInClassList(HiddenClass, panel != _setupPanel);
            _historyPanel.EnableInClassList(HiddenClass, panel != _historyPanel);
        }

        private void Reset() => _document = GetComponent<UIDocument>();
    }
}
