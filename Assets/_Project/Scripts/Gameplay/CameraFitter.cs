using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Ajusta la cámara ortográfica para que el tablero quepa en cualquier proporción de pantalla,
    /// dentro de la zona segura y sin tapar la UI de arriba y abajo. Solo recalcula cuando cambia
    /// la resolución o la zona segura.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFitter : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;

        [Tooltip("Margen alrededor del tablero, en unidades de mundo.")]
        [SerializeField, Min(0f)] private float _padding = 0.3f;

        [Tooltip("Fracción de la altura de pantalla reservada arriba para la barra de estado.")]
        [SerializeField, Range(0f, 0.4f)] private float _topReserved = 0.12f;

        [Tooltip("Fracción de la altura de pantalla reservada abajo para los botones.")]
        [SerializeField, Range(0f, 0.4f)] private float _bottomReserved = 0.12f;

        private Camera _camera;
        private int _lastWidth = -1;
        private int _lastHeight = -1;
        private Rect _lastSafeArea;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_boardView == null)
            {
                Debug.LogError($"{nameof(CameraFitter)}: asigna BoardView en el inspector.", this);
                enabled = false;
            }
        }

        private void OnEnable() => _lastWidth = -1;

        // LateUpdate: la resolución puede cambiar en cualquier Update del frame (rotación, ventana).
        private void LateUpdate()
        {
            var width = Screen.width;
            var height = Screen.height;
            var safeArea = Screen.safeArea;
            if (width == _lastWidth && height == _lastHeight && safeArea == _lastSafeArea)
                return;

            _lastWidth = width;
            _lastHeight = height;
            _lastSafeArea = safeArea;
            Fit(width, height, safeArea);
        }

        private void Fit(int width, int height, Rect safeArea)
        {
            var half = _boardView.GetWorldHalfExtents() + new Vector2(_padding, _padding);
            var fit = CameraFit.Compute(half, width, height, safeArea, _topReserved, _bottomReserved);

            _camera.orthographicSize = fit.OrthographicSize;
            var board = _boardView.transform.position;
            transform.position = new Vector3(
                board.x - fit.RegionCenterOffset.x,
                board.y - fit.RegionCenterOffset.y,
                transform.position.z);
        }

        // Al tocar valores en el inspector durante Play, recalcular en el siguiente frame.
        private void OnValidate() => _lastWidth = -1;
    }
}
