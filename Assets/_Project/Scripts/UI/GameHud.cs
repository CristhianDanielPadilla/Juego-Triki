using Triki.Core;
using Triki.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Triki.UI
{
    /// <summary>
    /// Barra de la partida: de quién es el turno, qué debe hacer y el resultado final,
    /// más los botones Reiniciar y Menú. Solo reacciona a eventos del juego.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameHud : MonoBehaviour, IPointerBlocker
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private GameController _gameController;

        private TrikiGame _game;
        private Label _status;
        private Button _restartButton;
        private Button _menuButton;

        private void OnEnable()
        {
            if (_document == null || _gameController == null)
            {
                Debug.LogError($"{nameof(GameHud)}: asigna UIDocument y GameController en el inspector.", this);
                return;
            }

            var root = _document.rootVisualElement;
            _status = root.Q<Label>("status-label");
            _restartButton = root.Q<Button>("restart-button");
            _menuButton = root.Q<Button>("menu-button");

            _restartButton.clicked += _gameController.RestartGame;
            _menuButton.clicked += SceneNavigator.OpenMenu;
            _gameController.SetPointerBlocker(this);

            // Al volver a habilitarse (no en el primer OnEnable) el juego ya existe.
            if (_game != null)
                Refresh();
        }

        // En Start todos los Awake ya corrieron, así que GameController ya creó la partida.
        private void Start()
        {
            if (_gameController == null)
                return;

            _game = _gameController.Game;
            if (_game == null)
                return;

            _game.TurnChanged += HandleTurnChanged;
            _game.GameWon += HandleGameWon;
            _game.GameDrawn += HandleGameDrawn;
            _game.GameReset += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_restartButton == null)
                return;

            _restartButton.clicked -= _gameController.RestartGame;
            _menuButton.clicked -= SceneNavigator.OpenMenu;
            _gameController.SetPointerBlocker(null);
        }

        /// <summary>
        /// Un clic es del HUD si cae sobre uno de sus botones. El resto del panel está marcado
        /// como <c>picking-mode="Ignore"</c> en el UXML, así que no intercepta nada y el tablero
        /// recibe los clics con normalidad.
        /// </summary>
        public bool BlocksPointer(Vector2 screenPosition)
        {
            var panel = _document != null ? _document.rootVisualElement?.panel : null;
            if (panel == null)
                return false;

            // ScreenToPanel espera el origen arriba a la izquierda; el puntero llega al revés.
            var point = RuntimePanelUtils.ScreenToPanel(
                panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));

            for (var element = panel.Pick(point); element != null; element = element.parent)
            {
                if (element is Button)
                    return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (_game == null)
                return;

            _game.TurnChanged -= HandleTurnChanged;
            _game.GameWon -= HandleGameWon;
            _game.GameDrawn -= HandleGameDrawn;
            _game.GameReset -= Refresh;
        }

        private void HandleTurnChanged(Player player) => Refresh();

        private void HandleGameWon(Player winner, WinReason reason) => Refresh();

        private void HandleGameDrawn(DrawReason reason) => Refresh();

        private void Refresh()
        {
            if (_status == null || _game == null)
                return;

            switch (_game.Phase)
            {
                case GamePhase.Placement when _gameController.IsAiTurn:
                case GamePhase.Movement when _gameController.IsAiTurn:
                    SetStatus(_game.CurrentPlayer, $"Turno de {GetDisplayName(_game.CurrentPlayer)}: pensando…");
                    _restartButton.text = "Reiniciar";
                    break;

                case GamePhase.Placement when _game.ForbiddenCell != TrikiGame.NoCell:
                    SetStatus(_game.CurrentPlayer, $"Turno de {GetDisplayName(_game.CurrentPlayer)}: coloca la primera ficha fuera del centro");
                    _restartButton.text = "Reiniciar";
                    break;

                case GamePhase.Placement:
                    var inHand = _game.GetPiecesInHand(_game.CurrentPlayer);
                    SetStatus(_game.CurrentPlayer, $"Turno de {GetDisplayName(_game.CurrentPlayer)}: coloca una ficha ({inHand} en mano)");
                    _restartButton.text = "Reiniciar";
                    break;

                case GamePhase.Movement:
                    var move = _game.MovementMovesPlayed + 1;
                    SetStatus(_game.CurrentPlayer, $"Turno de {GetDisplayName(_game.CurrentPlayer)}: mueve una ficha (movimiento {move} de {_game.Rules.MaxMovementMoves})");
                    _restartButton.text = "Reiniciar";
                    break;

                case GamePhase.GameOver when _game.IsDraw:
                    SetStatus(Player.None, _game.DrawReason == DrawReason.Repetition
                        ? $"Empate: la posición se repitió {_game.Rules.RepetitionLimit} veces"
                        : "Empate: se agotaron los movimientos");
                    _restartButton.text = "Revancha";
                    break;

                case GamePhase.GameOver:
                    var suffix = _game.WinReason == WinReason.OpponentBlocked ? " por bloqueo" : string.Empty;
                    SetStatus(_game.Winner, $"¡Gana {GetDisplayName(_game.Winner)}{suffix}!");
                    _restartButton.text = "Revancha";
                    break;
            }
        }

        /// <summary>"Rojo", o "Rojo (tú)" / "Azul (IA)" en partidas contra la IA.</summary>
        private string GetDisplayName(Player player)
        {
            var name = PlayerLabels.GetName(player);
            if (!_gameController.Settings.VsAi)
                return name;
            return _gameController.IsAiPlayer(player) ? name + " (IA)" : name + " (tú)";
        }

        private void SetStatus(Player player, string text)
        {
            _status.text = text;
            _status.EnableInClassList(PlayerLabels.PlayerOneClass, player == Player.One);
            _status.EnableInClassList(PlayerLabels.PlayerTwoClass, player == Player.Two);
        }

        private void Reset()
        {
            _document = GetComponent<UIDocument>();
        }
    }
}
