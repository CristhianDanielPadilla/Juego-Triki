using Triki.Core;
using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Efectos de sonido de la partida. Escucha los eventos del juego y reproduce el clip asignado
    /// en el inspector o, si falta, uno sintetizado al arrancar con <see cref="ToneSynth"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioFeedback : MonoBehaviour
    {
        // Notas (Hz): Do5, Mi5, Sol5, Do6.
        private static readonly float[] WinArpeggio = { 523.25f, 659.25f, 783.99f, 1046.5f };
        private static readonly float[] DrawNotes = { 440f, 349.23f };

        [SerializeField] private GameController _gameController;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;

        [Header("Clips (opcionales: si faltan se sintetizan)")]
        [Tooltip("Si se asigna, se usa para ambos jugadores.")]
        [SerializeField] private AudioClip _placeClip;
        [SerializeField] private AudioClip _moveClip;
        [SerializeField] private AudioClip _winClip;
        [SerializeField] private AudioClip _drawClip;

        private AudioSource _source;
        private TrikiGame _game;

        private AudioClip _placeOne;
        private AudioClip _placeTwo;
        private AudioClip _move;
        private AudioClip _win;
        private AudioClip _draw;

        // Solo los clips generados aquí se destruyen; los del inspector son assets.
        private AudioClip _generatedPlaceOne;
        private AudioClip _generatedPlaceTwo;
        private AudioClip _generatedMove;
        private AudioClip _generatedWin;
        private AudioClip _generatedDraw;

        private void Awake()
        {
            AudioPreferences.Apply();

            _source = GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            if (_placeClip != null)
            {
                _placeOne = _placeClip;
                _placeTwo = _placeClip;
            }
            else
            {
                // Cada jugador suena un poco distinto: el Rojo más agudo que el Azul.
                _generatedPlaceOne = CreateClip("Place One", ToneSynth.Sweep(620f, 880f, 0.09f, 0.7f, 30f));
                _generatedPlaceTwo = CreateClip("Place Two", ToneSynth.Sweep(440f, 620f, 0.09f, 0.7f, 30f));
                _placeOne = _generatedPlaceOne;
                _placeTwo = _generatedPlaceTwo;
            }

            if (_moveClip == null)
                _generatedMove = CreateClip("Move", ToneSynth.Sweep(300f, 420f, 0.13f, 0.5f, 18f));
            if (_winClip == null)
                _generatedWin = CreateClip("Win", ToneSynth.Sequence(WinArpeggio, 0.11f, 0.6f, 6f));
            if (_drawClip == null)
                _generatedDraw = CreateClip("Draw", ToneSynth.Sequence(DrawNotes, 0.18f, 0.5f, 5f));

            _move = _moveClip != null ? _moveClip : _generatedMove;
            _win = _winClip != null ? _winClip : _generatedWin;
            _draw = _drawClip != null ? _drawClip : _generatedDraw;
        }

        // En Start todos los Awake ya corrieron, así que GameController ya creó la partida.
        private void Start()
        {
            if (_gameController == null)
            {
                Debug.LogError($"{nameof(AudioFeedback)}: asigna GameController en el inspector.", this);
                return;
            }

            _game = _gameController.Game;
            if (_game == null)
                return;

            _game.PiecePlaced += HandlePiecePlaced;
            _game.PieceMoved += HandlePieceMoved;
            _game.GameWon += HandleGameWon;
            _game.GameDrawn += HandleGameDrawn;
        }

        private void OnDestroy()
        {
            if (_game != null)
            {
                _game.PiecePlaced -= HandlePiecePlaced;
                _game.PieceMoved -= HandlePieceMoved;
                _game.GameWon -= HandleGameWon;
                _game.GameDrawn -= HandleGameDrawn;
            }

            DestroyClip(_generatedPlaceOne);
            DestroyClip(_generatedPlaceTwo);
            DestroyClip(_generatedMove);
            DestroyClip(_generatedWin);
            DestroyClip(_generatedDraw);
        }

        private void HandlePiecePlaced(int cell, Player player) => Play(player == Player.One ? _placeOne : _placeTwo);

        private void HandlePieceMoved(int from, int to, Player player) => Play(_move);

        private void HandleGameWon(Player winner, WinReason reason) => Play(_win);

        private void HandleGameDrawn(DrawReason reason) => Play(_draw);

        private void Play(AudioClip clip)
        {
            if (clip != null)
                _source.PlayOneShot(clip, _volume);
        }

        private static AudioClip CreateClip(string clipName, float[] samples)
        {
            var clip = AudioClip.Create("Triki " + clipName, samples.Length, 1, ToneSynth.SampleRate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.DontSave;
            return clip;
        }

        private static void DestroyClip(AudioClip clip) => SpriteFactory.SafeDestroy(clip);
    }
}
