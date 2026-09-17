using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Una capa dibujada por encima del tablero que se queda con el clic (los botones del HUD).
    /// La implementa la capa de UI y se la entrega a <see cref="GameController"/>: así Gameplay
    /// no necesita saber nada de UI Toolkit y las dependencias siguen yendo en una sola dirección.
    /// </summary>
    public interface IPointerBlocker
    {
        /// <param name="screenPosition">Puntero en píxeles de pantalla, origen abajo a la izquierda.</param>
        /// <returns><c>true</c> si el clic es para la interfaz y el tablero debe ignorarlo.</returns>
        bool BlocksPointer(Vector2 screenPosition);
    }
}
