using UnityEngine;
using UnityEngine.UIElements;

namespace Triki.UI
{
    /// <summary>Márgenes de la zona segura (muescas, esquinas redondeadas) en unidades del panel.</summary>
    public readonly struct SafeAreaInsets
    {
        public SafeAreaInsets(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public float Left { get; }
        public float Right { get; }
        public float Top { get; }
        public float Bottom { get; }

        /// <summary>
        /// Convierte la zona segura (píxeles, origen abajo a la izquierda) a márgenes del panel.
        /// <paramref name="panelWidth"/> es el ancho del panel en sus propias unidades, que difiere de
        /// los píxeles cuando el PanelSettings escala con la pantalla.
        /// </summary>
        public static SafeAreaInsets Compute(float screenWidth, float screenHeight, Rect safeArea, float panelWidth)
        {
            if (screenWidth <= 0f || screenHeight <= 0f || panelWidth <= 0f)
                return default;

            var scale = panelWidth / screenWidth;
            return new SafeAreaInsets(
                Mathf.Max(0f, safeArea.xMin) * scale,
                Mathf.Max(0f, screenWidth - safeArea.xMax) * scale,
                Mathf.Max(0f, screenHeight - safeArea.yMax) * scale,
                Mathf.Max(0f, safeArea.yMin) * scale);
        }
    }

    /// <summary>
    /// Aplica la zona segura como padding a la raíz de un UIDocument para que ningún botón quede
    /// bajo una muesca. Solo recalcula cuando cambia la pantalla o el tamaño del panel.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SafeAreaPadding : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private int _lastWidth = -1;
        private int _lastHeight = -1;
        private float _lastPanelWidth = -1f;
        private Rect _lastSafeArea;

        private void OnEnable() => _lastWidth = -1;

        private void LateUpdate()
        {
            if (_document == null)
                return;

            var root = _document.rootVisualElement;
            if (root?.panel == null)
                return;

            // El ancho del panel se conoce tras el primer layout; hasta entonces es NaN.
            var panelWidth = root.panel.visualTree.layout.width;
            if (float.IsNaN(panelWidth) || panelWidth <= 0f)
                return;

            var width = Screen.width;
            var height = Screen.height;
            var safeArea = Screen.safeArea;
            if (width == _lastWidth && height == _lastHeight && safeArea == _lastSafeArea
                && Mathf.Approximately(panelWidth, _lastPanelWidth))
                return;

            _lastWidth = width;
            _lastHeight = height;
            _lastSafeArea = safeArea;
            _lastPanelWidth = panelWidth;

            var insets = SafeAreaInsets.Compute(width, height, safeArea, panelWidth);
            root.style.paddingLeft = insets.Left;
            root.style.paddingRight = insets.Right;
            root.style.paddingTop = insets.Top;
            root.style.paddingBottom = insets.Bottom;
        }

        private void Reset() => _document = GetComponent<UIDocument>();
    }
}
