using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Triki.Editor
{
    /// <summary>
    /// Builds lanzados desde la línea de comandos. Unity trae flags para escritorio
    /// (<c>-buildWindows64Player</c>) pero ninguno para WebGL, así que esa build necesita
    /// <c>-executeMethod</c>. Tener la configuración aquí y no a mano en el editor es lo que
    /// hace que la build se pueda repetir igual desde cualquier sitio.
    /// </summary>
    public static class BuildCommands
    {
        private const string OutputArgument = "-trikiOutput";
        private const string DefaultWebGLOutput = "Build/WebGL";

        /// <summary>
        /// <c>Unity.exe -batchmode -quit -projectPath &lt;ruta&gt; -buildTarget WebGL
        /// -executeMethod Triki.Editor.BuildCommands.BuildWebGL -trikiOutput &lt;carpeta&gt;</c>
        /// </summary>
        public static void BuildWebGL()
        {
            var succeeded = RunWebGL(GetArgument(OutputArgument) ?? DefaultWebGLOutput);

            // En batchmode hay que salir a mano con el codigo correcto, o la CI creeria que fue bien.
            if (Application.isBatchMode)
                EditorApplication.Exit(succeeded ? 0 : 1);
        }

        [MenuItem("Triki/Compilar WebGL")]
        private static void BuildWebGLFromMenu() => RunWebGL(DefaultWebGLOutput);

        private static bool RunWebGL(string outputPath)
        {
            ConfigureWebGL();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            });

            var summary = report.summary;
            Debug.Log($"Build WebGL: {summary.result} · {summary.totalSize / (1024 * 1024)} MB · " +
                      $"{summary.totalTime} · salida '{outputPath}'");
            return summary.result == BuildResult.Succeeded;
        }

        /// <summary>
        /// Ajustes que el juego necesita para funcionar servido como archivos estáticos
        /// (GitHub Pages, itch.io) sin poder tocar las cabeceras del servidor.
        /// </summary>
        private static void ConfigureWebGL()
        {
            // Sin "decompression fallback", el navegador solo entiende los archivos comprimidos si
            // el servidor manda Content-Encoding. En un hosting estatico no se puede, y el juego
            // se queda en la pantalla de carga: con el fallback los descomprime el propio loader.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Guarda el juego en IndexedDB para que la segunda visita no lo descargue entero.
            PlayerSettings.WebGL.dataCaching = true;
        }

        private static string[] EnabledScenes() =>
            EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();

        /// <summary>Lee un argumento con valor de la línea de comandos, o <c>null</c> si no está.</summary>
        private static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                    return args[i + 1];
            }
            return null;
        }
    }
}
