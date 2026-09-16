using System;

namespace Triki.Core
{
    /// <summary>
    /// Reglas y turnos. Cada jugador tiene <see cref="PiecesPerPlayer"/> fichas y las coloca
    /// por turnos; al estar las 6 en el tablero la partida pasa a <see cref="GamePhase.Movement"/>.
    /// La capa visual escucha los eventos; nunca consulta el estado cada frame.
    /// </summary>
    public sealed class TrikiGame
    {
        public const int PiecesPerPlayer = 3;

        // Indexado por (int)Player; la posición 0 (None) no se usa.
        private readonly int[] _piecesPlaced = new int[3];

        public TrikiGame() : this(BoardGraph.CreateSquare())
        {
        }

        public TrikiGame(BoardGraph graph)
        {
            Board = new Board(graph);
            Reset();
        }

        /// <summary>Casilla y dueño de la ficha recién colocada.</summary>
        public event Action<int, Player> PiecePlaced;

        public event Action<Player> TurnChanged;

        public event Action<GamePhase> PhaseChanged;

        public event Action GameReset;

        public Board Board { get; }

        public Player CurrentPlayer { get; private set; }

        public GamePhase Phase { get; private set; }

        public int GetPiecesPlaced(Player player) => _piecesPlaced[ToIndex(player)];

        public int GetPiecesInHand(Player player) => PiecesPerPlayer - _piecesPlaced[ToIndex(player)];

        public PlaceResult TryPlace(int cell)
        {
            if (Phase != GamePhase.Placement)
                return PlaceResult.WrongPhase;
            if (!BoardGraph.IsValidCell(cell))
                return PlaceResult.InvalidCell;
            if (!Board.IsEmpty(cell))
                return PlaceResult.CellOccupied;

            var player = CurrentPlayer;
            Board.Set(cell, player);
            _piecesPlaced[(int)player]++;
            PiecePlaced?.Invoke(cell, player);

            if (_piecesPlaced[(int)Player.One] == PiecesPerPlayer &&
                _piecesPlaced[(int)Player.Two] == PiecesPerPlayer)
            {
                Phase = GamePhase.Movement;
                PhaseChanged?.Invoke(Phase);
            }

            CurrentPlayer = player.Opponent();
            TurnChanged?.Invoke(CurrentPlayer);
            return PlaceResult.Placed;
        }

        public void Reset(Player startingPlayer = Player.One)
        {
            ToIndex(startingPlayer);

            Board.Clear();
            Array.Clear(_piecesPlaced, 0, _piecesPlaced.Length);
            CurrentPlayer = startingPlayer;
            Phase = GamePhase.Placement;
            GameReset?.Invoke();
        }

        private static int ToIndex(Player player)
        {
            if (player != Player.One && player != Player.Two)
                throw new ArgumentOutOfRangeException(nameof(player), player, "Se esperaba Player.One o Player.Two.");
            return (int)player;
        }
    }
}
