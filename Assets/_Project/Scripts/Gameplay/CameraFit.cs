using UnityEngine;

namespace Triki.Gameplay
{
    public readonly struct CameraFitResult
    {
        public CameraFitResult(float orthographicSize, Vector2 regionCenterOffset)
        {
            OrthographicSize = orthographicSize;
            RegionCenterOffset = regionCenterOffset;
        }

        public float OrthographicSize { get; }

        /// <summary>
        /// Dónde queda el centro de la zona útil respecto al centro de la cámara, en unidades de mundo.
        /// Para centrar el tablero ahí: posición de cámara = centro del tablero - este offset.
        /// </summary>
        public Vector2 RegionCenterOffset { get; }
    }

    /// <summary>
    /// Cálculo puro (sin estado) para encajar un rectángulo del mundo en una cámara ortográfica.
    /// La zona útil es la zona segura de la pantalla menos las franjas reservadas para la UI.
    /// </summary>
    public static class CameraFit
    {
        // Evita divisiones por cero si las reservas se comen toda la pantalla.
        private const float MinRegionFraction = 0.05f;

        public static CameraFitResult Compute(
            Vector2 halfExtents,
            float screenWidth,
            float screenHeight,
            Rect safeArea,
            float topReserved,
            float bottomReserved)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
                return new CameraFitResult(Mathf.Max(halfExtents.x, halfExtents.y, 0.01f), Vector2.zero);

            var aspect = screenWidth / screenHeight;

            // Zona útil en coordenadas de viewport (0..1, origen abajo a la izquierda, como Screen.safeArea).
            var left = safeArea.xMin / screenWidth;
            var right = safeArea.xMax / screenWidth;
            var bottom = safeArea.yMin / screenHeight + bottomReserved;
            var top = safeArea.yMax / screenHeight - topReserved;

            var width = Mathf.Max(right - left, MinRegionFraction);
            var height = Mathf.Max(top - bottom, MinRegionFraction);

            // Con tamaño ortográfico s, el viewport mide 2s de alto y 2s·aspect de ancho.
            var size = Mathf.Max(halfExtents.y / height, halfExtents.x / (aspect * width));
            size = Mathf.Max(size, 0.01f);

            var centerX = left + width * 0.5f - 0.5f;
            var centerY = bottom + height * 0.5f - 0.5f;
            var offset = new Vector2(centerX * 2f * size * aspect, centerY * 2f * size);

            return new CameraFitResult(size, offset);
        }
    }
}
