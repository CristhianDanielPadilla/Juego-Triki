using Triki.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Triki.Gameplay
{
    /// <summary>
    /// Punto de unión: es dueño de la partida, traduce el puntero (mouse o toque) a jugadas
    /// y reenvía los eventos de <see cref="TrikiGame"/> a la vista.
    /// En la fase de movimiento, un clic elige una ficha propia y el siguiente, su destino.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        private const int NoSelection = -1;

        [SerializeField] private BoardView _boardView;
        [SerializeField] private Camera _camera;

        private TrikiGame _game;
        private InputAction _pressAction;
        private int _selectedCell = NoSelection;
        private StatsRepository _statsRepository;
        private MatchStats _stats;

        /// <summary>Partida en curso; la UI se suscribe a sus eventos. Disponible desde <c>Start</c>.</summary>
        public TrikiGame Game => _game;

        public void RestartGame() => _game?.Reset();

        private void Awake()
        {
            if (_boardView == null || _camera == null)
            {
                Debug.LogError($"{nameof(GameController)}: asigna BoardView y Camera en el inspector.", this);
                enabled = false;
                return;
            }

            _statsRepository = new StatsRepository();
            _stats = _statsRepository.Load();

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
            _game.GameWon += HandleGameWon;
            _game.GameReset += HandleGameReset;
            _pressAction.performed += HandlePress;
            _pressAction.Enable();
        }

        private void OnDisable()
        {
            if (_game == null)
                return;

            _pressAction.Disable();
            _pressAction.performed -= HandlePress;
            _game.PiecePlaced -= HandlePiecePlaced;
            _game.PieceMoved -= HandlePieceMoved;
            _game.GameWon -= HandleGameWon;
            _game.GameReset -= HandleGameReset;
        }

        private void OnDestroy()
        {
            _pressAction?.Dispose();
        }

        private void HandlePress(InputAction.CallbackContext context)
        {
            if (_game.Phase == GamePhase.GameOver)
                return;

            var pointer = Pointer.current;
            if (pointer == null)
                return;

            var screen = pointer.position.ReadValue();
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

        private void HandleGameWon(Player winner, WinReason reason)
        {
            _stats.RecordWin(winner);
            _statsRepository.Save(_stats);

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
        }
    }
}
