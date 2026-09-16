using Triki.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Triki.Gameplay
{
    /// <summary>
    /// Punto de unión: es dueño de la partida, traduce el puntero (mouse o toque) a casillas
    /// y reenvía los eventos de <see cref="TrikiGame"/> a la vista.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private Camera _camera;

        private TrikiGame _game;
        private InputAction _pressAction;

        /// <summary>Partida en curso; la UI se suscribe a sus eventos.</summary>
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

            _game = new TrikiGame();
            _boardView.Build(_game.Board.Graph);
            _pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
        }

        private void OnEnable()
        {
            if (_game == null)
                return;

            _game.PiecePlaced += HandlePiecePlaced;
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
            _game.GameReset -= HandleGameReset;
        }

        private void OnDestroy()
        {
            _pressAction?.Dispose();
        }

        private void HandlePress(InputAction.CallbackContext context)
        {
            var pointer = Pointer.current;
            if (pointer == null)
                return;

            var screen = pointer.position.ReadValue();
            var world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            if (_boardView.TryGetCell(world, out var cell))
                _game.TryPlace(cell);
        }

        private void HandlePiecePlaced(int cell, Player player) => _boardView.ShowPiece(cell, player);

        private void HandleGameReset() => _boardView.ClearPieces();
    }
}
