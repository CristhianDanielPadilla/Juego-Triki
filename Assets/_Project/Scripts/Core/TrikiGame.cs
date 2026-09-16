using System;

namespace Triki.Core
{
    /// <summary>
    /// Reglas y turnos. Cada jugador tiene <see cref="PiecesPerPlayer"/> fichas y las coloca
    /// por turnos; al estar las 6 en el tablero la partida pasa a <see cref="GamePhase.Movement"/>.
    /// Gana quien alinee sus 3 fichas en una recta cuyas casillas estén unidas por aristas,
    /// incluso durante la colocación.
    /// La capa visual escucha los eventos; nunca consulta el estado cada frame.
    /// </summary>
    public sealed class TrikiGame
    {
        public const int PiecesPerPlayer = 3;

        // Indexado por (int)Player; la posición 0 (None) no se usa.
        private readonly int[] _piecesPlaced = new int[3];
        private readonly BoardLine[] _winLines;

        public TrikiGame() : this(BoardGraph.CreateSquare())
        {
        }

        public TrikiGame(BoardGraph graph)
        {
            Board = new Board(graph);
            _winLines = BuildWinLines(graph);
            Reset();
        }

        /// <summary>Casilla y dueño de la ficha recién colocada.</summary>
        public event Action<int, Player> PiecePlaced;

        /// <summary>Se emite al cambiar de turno. No se emite en la jugada que gana.</summary>
        public event Action<Player> TurnChanged;

        public event Action<GamePhase> PhaseChanged;

        /// <summary>Ganador y línea formada. Se emite después de <see cref="PhaseChanged"/>.</summary>
        public event Action<Player, BoardLine> GameWon;

        public event Action GameReset;

        public Board Board { get; }

        /// <summary>A quién le toca. Al terminar la partida conserva al ganador.</summary>
        public Player CurrentPlayer { get; private set; }

        public GamePhase Phase { get; private set; }

        /// <summary><see cref="Player.None"/> mientras nadie haya ganado.</summary>
        public Player Winner { get; private set; }

        /// <summary>Solo tiene sentido si <see cref="Winner"/> no es <see cref="Player.None"/>.</summary>
        public BoardLine WinningLine { get; private set; }

        /// <summary>Líneas que dan victoria con el grafo de esta partida.</summary>
        public int WinLineCount => _winLines.Length;

        public BoardLine GetWinLine(int index) => _winLines[index];

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

            if (TryFindLine(Board.GetMask(player), out var line))
            {
                Winner = player;
                WinningLine = line;
                ChangePhase(GamePhase.GameOver);
                GameWon?.Invoke(player, line);
                return PlaceResult.Placed;
            }

            if (_piecesPlaced[(int)Player.One] == PiecesPerPlayer &&
                _piecesPlaced[(int)Player.Two] == PiecesPerPlayer)
            {
                ChangePhase(GamePhase.Movement);
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
            Winner = Player.None;
            WinningLine = default;
            GameReset?.Invoke();
        }

        private bool TryFindLine(int playerMask, out BoardLine line)
        {
            for (var i = 0; i < _winLines.Length; i++)
            {
                var candidate = _winLines[i];
                if ((playerMask & candidate.Mask) == candidate.Mask)
                {
                    line = candidate;
                    return true;
                }
            }

            line = default;
            return false;
        }

        private void ChangePhase(GamePhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        /// <summary>Una recta solo cuenta si sus casillas se pueden recorrer por aristas del grafo.</summary>
        private static BoardLine[] BuildWinLines(BoardGraph graph)
        {
            var count = 0;
            for (var i = 0; i < BoardLine.StraightLineCount; i++)
            {
                if (IsConnected(graph, BoardLine.GetStraightLine(i)))
                    count++;
            }

            var lines = new BoardLine[count];
            var next = 0;
            for (var i = 0; i < BoardLine.StraightLineCount; i++)
            {
                var line = BoardLine.GetStraightLine(i);
                if (IsConnected(graph, line))
                    lines[next++] = line;
            }
            return lines;
        }

        private static bool IsConnected(BoardGraph graph, BoardLine line)
        {
            return graph.AreAdjacent(line.A, line.B) && graph.AreAdjacent(line.B, line.C);
        }

        private static int ToIndex(Player player)
        {
            if (player != Player.One && player != Player.Two)
                throw new ArgumentOutOfRangeException(nameof(player), player, "Se esperaba Player.One o Player.Two.");
            return (int)player;
        }
    }
}
