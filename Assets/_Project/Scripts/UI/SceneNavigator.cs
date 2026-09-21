using UnityEngine;
using UnityEngine.SceneManagement;

namespace Triki.UI
{
    /// <summary>Único lugar que conoce los nombres de escena. Deben coincidir con Build Settings.</summary>
    public static class SceneNavigator
    {
        public const string MenuScene = "Menu";
        public const string GameScene = "Game";

        public static void OpenMenu() => SceneManager.LoadScene(MenuScene);

        public static void StartGame() => SceneManager.LoadScene(GameScene);

        /// <summary>
        /// En el navegador no hay ventana que cerrar: <c>Application.Quit</c> no hace nada, así que
        /// el menú esconde el botón "Salir" en vez de ofrecer algo que no funciona.
        /// </summary>
        public static bool CanQuitApplication =>
#if UNITY_WEBGL && !UNITY_EDITOR
            false;
#else
            true;
#endif

        public static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
