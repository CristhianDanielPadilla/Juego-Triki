using Triki.Core;
using Triki.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Triki.UI
{
    /// <summary>Menú de inicio: iniciar partida, ver el histórico y salir.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string HiddenClass = "hidden";

        [SerializeField] private UIDocument _document;

        private VisualElement _mainPanel;
        private VisualElement _historyPanel;
        private Button _playButton;
        private Button _historyButton;
        private Button _quitButton;
        private Button _backButton;
        private Label _gamesPlayed;
        private Label _playerOneName;
        private Label _playerOneWins;
        private Label _playerOneLosses;
        private Label _playerTwoName;
        private Label _playerTwoWins;
        private Label _playerTwoLosses;

        private void OnEnable()
        {
            if (_document == null)
            {
                Debug.LogError($"{nameof(MainMenuController)}: asigna el UIDocument en el inspector.", this);
                return;
            }

            var root = _document.rootVisualElement;
            _mainPanel = root.Q("main-panel");
            _historyPanel = root.Q("history-panel");
            _playButton = root.Q<Button>("play-button");
            _historyButton = root.Q<Button>("history-button");
            _quitButton = root.Q<Button>("quit-button");
            _backButton = root.Q<Button>("back-button");
            _gamesPlayed = root.Q<Label>("games-played");
            _playerOneName = root.Q<Label>("player-one-name");
            _playerOneWins = root.Q<Label>("player-one-wins");
            _playerOneLosses = root.Q<Label>("player-one-losses");
            _playerTwoName = root.Q<Label>("player-two-name");
            _playerTwoWins = root.Q<Label>("player-two-wins");
            _playerTwoLosses = root.Q<Label>("player-two-losses");

            _playerOneName.text = PlayerLabels.GetName(Player.One);
            _playerTwoName.text = PlayerLabels.GetName(Player.Two);

            _playButton.clicked += SceneNavigator.StartGame;
            _historyButton.clicked += ShowHistory;
            _quitButton.clicked += SceneNavigator.QuitApplication;
            _backButton.clicked += ShowMain;

            ShowMain();
        }

        private void OnDisable()
        {
            if (_playButton == null)
                return;

            _playButton.clicked -= SceneNavigator.StartGame;
            _historyButton.clicked -= ShowHistory;
            _quitButton.clicked -= SceneNavigator.QuitApplication;
            _backButton.clicked -= ShowMain;
        }

        private void ShowMain()
        {
            _historyPanel.AddToClassList(HiddenClass);
            _mainPanel.RemoveFromClassList(HiddenClass);
            _playButton.Focus();
        }

        private void ShowHistory()
        {
            // Se lee al abrir: así siempre refleja las partidas jugadas desde el último vistazo.
            var stats = new StatsRepository().Load();
            _gamesPlayed.text = stats.GamesPlayed.ToString();
            _playerOneWins.text = stats.GetWins(Player.One).ToString();
            _playerOneLosses.text = stats.GetLosses(Player.One).ToString();
            _playerTwoWins.text = stats.GetWins(Player.Two).ToString();
            _playerTwoLosses.text = stats.GetLosses(Player.Two).ToString();

            _mainPanel.AddToClassList(HiddenClass);
            _historyPanel.RemoveFromClassList(HiddenClass);
            _backButton.Focus();
        }

        private void Reset() => _document = GetComponent<UIDocument>();
    }
}
