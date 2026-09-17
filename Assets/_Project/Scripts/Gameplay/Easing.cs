using System;

namespace Triki.Gameplay
{
    /// <summary>Curvas de animación: reciben t en [0, 1] y devuelven el progreso.</summary>
    public static class Easing
    {
        private const float BackOvershoot = 1.70158f;

        /// <summary>Se pasa un poco del final y vuelve: sensación de "rebote" al aparecer.</summary>
        public static float OutBack(float t)
        {
            t = Clamp01(t) - 1f;
            return 1f + t * t * ((BackOvershoot + 1f) * t + BackOvershoot);
        }

        public static float OutCubic(float t)
        {
            t = 1f - Clamp01(t);
            return 1f - t * t * t;
        }

        public static float InOutCubic(float t)
        {
            t = Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3) * 0.5f;
        }

        private static float Clamp01(float t) => t < 0f ? 0f : t > 1f ? 1f : t;
    }
}
