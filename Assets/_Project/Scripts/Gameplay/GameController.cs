using Triki.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Triki.Gameplay
{
    /// <summary>
    /// Punto de unión: es dueño de la partida, traduce el puntero (mouse o toque) a jugadas,
    /// hace jugar a la IA cuando le toca y reenvía los eventos de <see cref="TrikiGame"/> a la vista.
    /// En la fase de movimiento, un clic elige una ficha propia y el siguiente, su destino.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        private const int NoSelection = -1;
        private const float NoAiPending = -1f;

        [SerializeField] private BoardView _boardView;
        [SerializeField] private Camera _camera;

        [Tooltip("Pausa antes de que la IA juegue, para que su jugada se pueda seguir.")]
        [SerializeField, Min(0f)] private float _aiMoveDelay = 0.6f;

        private TrikiGame _game;
        private TrikiAi _ai;
        private MatchSettings _settings;
        private InputAction _pressAction;
        private int _selectedCell = NoSelection;
        private float _aiCountdown = NoAiPending;
        private StatsRepository _statsRepository;
        private MatchHistory _history;
        private IPointerBlocker _pointerBlocker;

        /// <summary>Partida en curso; la UI se suscribe a sus eventos. Disponible desde <c>Start</c>.</summary>
        public TrikiGame Game => _game;

        public MatchSettings Settings => _settings;

        public bool IsAiTurn => _ai != null && _game.Phase != GamePhase.GameOver && _game.CurrentPlayer == _settings.AiPlayer;

        public bool IsAiPlayer(Player player) => _ai != null && player == _settings.AiPlayer;

        public void RestartGame() => _game?.Reset();

        /// <summary>
        /// Registra la capa de interfaz que puede quedarse con el clic; <c>null</c> lo quita.
        /// El área sensible de las casillas de abajo sobresale del tablero y llega a solaparse con
        /// los botones del HUD: sin esto, pulsar el borde de "Reiniciar" tocaba además la casilla
        /// que quedaba justo encima.
        /// </summary>
        public void SetPointerBlocker(IPointerBlocker blocker) => _pointerBlocker = blocker;

        private void Awake()
        {
            if (_boardView == null || _camera == null)
            {
                Debug.LogError($"{nameof(GameController)}: asigna BoardView y Camera en el inspector.", this);
                enabled = false;
                return;
            }

            _statsRepository = new StatsRepository();
            _history = _statsRepository.Load();

            _settings = MatchSettingsStore.Load();
            if (_settings.VsAi)
                _ai = new TrikiAi(_settings.Difficulty);

            _game = new TrikiGame();
            _boardView.Build(_game.Board.Graph);
            _pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
        }

        private void OnEnable()
        {
            if (_game == null)
                return;

            _game.PiecePlaced += HandlePiecePlaced;
            _game.PieceMoved += HandlePieceMoved;
            _game.TurnChanged += HandleTurnChanged;
            _game.GameWon += HandleGameWon;
            _game.GameDrawn += HandleGameDrawn;
            _game.GameReset += HandleGameReset;
            _pressAction.performed += HandlePress;
            _pressAction.Enable();
        }

        private void Start()
        {
            // Si la IA lleva el Rojo, abre la partida.
            if (_game != null)
                ScheduleAiIfNeeded();
        }

        private void OnDisable()
        {
            if (_game == null)
                return;

            _pressAction.Disable();
            _pressAction.performed -= HandlePress;
            _game.PiecePlaced -= HandlePiecePlaced;
            _game.PieceMoved -= HandlePieceMoved;
            _game.TurnChanged -= HandleTurnChanged;
            _game.GameWon -= HandleGameWon;
            _game.GameDrawn -= HandleGameDrawn;
            _game.GameReset -= HandleGameReset;
        }

        private void OnDestroy()
        {
            _pressAction?.Dispose();
        }

        private void Update()
        {
            if (_aiCountdown < 0f)
                return;

            _aiCountdown -= Time.deltaTime;
            if (_aiCountdown > 0f)
                return;

            _aiCountdown = NoAiPending;
            PlayAiMove();
        }

        private void PlayAiMove()
        {
            if (!IsAiTurn)
                return;

            var move = _ai.ChooseMove(_game);
            if (!move.ApplyTo(_game))
                Debug.LogError($"La IA eligió una jugada inválida: {move}.", this);
        }

        private void ScheduleAiIfNeeded()
        {
            _aiCountdown = IsAiTurn ? _aiMoveDelay : NoAiPending;
        }

        private void HandlePress(InputAction.CallbackContext context)
        {
            if (_game.Phase == GamePhase.GameOver || IsAiTurn)
                return;

            var pointer = Pointer.current;
            if (pointer == null)
                return;

            var screen = pointer.position.ReadValue();
            if (_pointerBlocker != null && _pointerBlocker.BlocksPointer(screen))
                return;

            var world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            if (!_boardView.TryGetCell(world, out var cell))
            {
                ClearSelection();
                return;
            }

            if (_game.Phase == GamePhase.Placement)
                _game.TryPlace(cell);
            else
                HandleMovementPress(cell);
        }

        private void HandleMovementPress(int cell)
        {
            if (_game.Board[cell] == _game.CurrentPlayer)
            {
                // Clic en ficha propia: la elige, o la suelta si ya estaba elegida.
                if (cell == _selectedCell)
                    ClearSelection();
                else
                    Select(cell);
                return;
            }

            if (_selectedCell != NoSelection && _game.TryMove(_selectedCell, cell) == MoveResult.Moved)
                return; // HandlePieceMoved limpia la selección.

            ClearSelection();
        }

        private void Select(int cell)
        {
            _selectedCell = cell;
            _boardView.ShowSelection(cell, _game.GetMoveTargets(cell));
        }

        private void ClearSelection()
        {
            if (_selectedCell == NoSelection)
                return;

            _selectedCell = NoSelection;
            _boardView.HideSelection();
        }

        private void HandlePiecePlaced(int cell, Player player) => _boardView.ShowPiece(cell, player);

        private void HandlePieceMoved(int from, int to, Player player)
        {
            ClearSelection();
            _boardView.MovePiece(from, to);
        }

        private void HandleTurnChanged(Player player) => ScheduleAiIfNeeded();

        /// <param name="winner"><see cref="Player.None"/> si fue empate.</param>
        private void SaveResult(Player winner)
        {
            HistoryRecorder.Record(_history, _settings, winner);
            _statsRepository.Save(_history);
        }

        private void HandleGameDrawn(DrawReason reason)
        {
            SaveResult(Player.None);
            ClearSelection();
        }

        private void HandleGameWon(Player winner, WinReason reason)
        {
            SaveResult(winner);

            ClearSelection();
            if (reason == WinReason.Line)
                _boardView.ShowWinningLine(_game.WinningLine, winner);
            else
                _boardView.ShowWinningPieces(_game.Board.GetMask(winner));
        }

        private void HandleGameReset()
        {
            _selectedCell = NoSelection;
            _boardView.ClearPieces();
            ScheduleAiIfNeeded();
        }
    }
}
