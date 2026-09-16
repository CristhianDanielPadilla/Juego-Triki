using Triki.Core;
using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>Configuración de la próxima partida elegida en el menú.</summary>
    public readonly struct MatchSettings
    {
        public MatchSettings(bool vsAi, AiDifficulty difficulty, Player humanPlayer)
        {
            VsAi = vsAi;
            Difficulty = difficulty;
            HumanPlayer = humanPlayer == Player.Two ? Player.Two : Player.One;
        }

        public static MatchSettings Default => new MatchSettings(true, AiDifficulty.Normal, Player.One);

        public bool VsAi { get; }

        public AiDifficulty Difficulty { get; }

        /// <summary>Color del humano contra la IA. El Rojo (One) siempre empieza.</summary>
        public Player HumanPlayer { get; }

        public Player AiPlayer => VsAi ? HumanPlayer.Opponent() : Player.None;

        public MatchSettings WithVsAi(bool vsAi) => new MatchSettings(vsAi, Difficulty, HumanPlayer);

        public MatchSettings WithDifficulty(AiDifficulty difficulty) => new MatchSettings(VsAi, difficulty, HumanPlayer);

        public MatchSettings WithHumanPlayer(Player player) => new MatchSettings(VsAi, Difficulty, player);
    }

    /// <summary>
    /// Guarda la configuración en PlayerPrefs: pasa del menú a la escena de juego y se recuerda
    /// entre sesiones. No se usa un campo estático porque el proyecto no recarga el dominio al
    /// entrar en Play y el valor sobreviviría entre sesiones del editor.
    /// </summary>
    public static class MatchSettingsStore
    {
        private const string VsAiKey = "Triki.Match.VsAi";
        private const string DifficultyKey = "Triki.Match.Difficulty";
        private const string HumanPlayerKey = "Triki.Match.HumanPlayer";

        public static MatchSettings Load()
        {
            var defaults = MatchSettings.Default;
            var vsAi = PlayerPrefs.GetInt(VsAiKey, defaults.VsAi ? 1 : 0) != 0;

            var difficulty = PlayerPrefs.GetInt(DifficultyKey, (int)defaults.Difficulty);
            if (difficulty < (int)AiDifficulty.Easy || difficulty > (int)AiDifficulty.Hard)
                difficulty = (int)defaults.Difficulty;

            var human = PlayerPrefs.GetInt(HumanPlayerKey, (int)defaults.HumanPlayer) == (int)Player.Two
                ? Player.Two
                : Player.One;

            return new MatchSettings(vsAi, (AiDifficulty)difficulty, human);
        }

        public static void Save(MatchSettings settings)
        {
            PlayerPrefs.SetInt(VsAiKey, settings.VsAi ? 1 : 0);
            PlayerPrefs.SetInt(DifficultyKey, (int)settings.Difficulty);
            PlayerPrefs.SetInt(HumanPlayerKey, (int)settings.HumanPlayer);
            PlayerPrefs.Save();
        }
    }
}
