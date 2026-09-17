using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>Preferencia de sonido guardada en PlayerPrefs y aplicada al AudioListener global.</summary>
    public static class AudioPreferences
    {
        private const string MutedKey = "Triki.Audio.Muted";

        public static bool IsMuted => PlayerPrefs.GetInt(MutedKey, 0) != 0;

        public static void SetMuted(bool muted)
        {
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        /// <summary>AudioListener.volume es global y sobrevive a los cambios de escena.</summary>
        public static void Apply() => AudioListener.volume = IsMuted ? 0f : 1f;
    }
}
