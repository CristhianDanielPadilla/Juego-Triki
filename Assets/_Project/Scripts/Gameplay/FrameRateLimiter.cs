using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Limita los fotogramas por segundo al arrancar el juego.
    /// El tablero está quieto casi todo el tiempo: sin límite, la GPU dibuja nueve círculos a
    /// varios cientos de fps y calienta el equipo para nada.
    /// </summary>
    public static class FrameRateLimiter
    {
        /// <summary>Suficiente para las animaciones del tablero, que duran menos de un cuarto de segundo.</summary>
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            // El límite solo se respeta con la sincronía vertical apagada: si se deja encendida,
            // Unity ignora targetFrameRate y dibuja a la frecuencia del monitor (144 Hz, 240 Hz…).
            // A cambio puede aparecer tearing; en un tablero casi estático no se nota.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
